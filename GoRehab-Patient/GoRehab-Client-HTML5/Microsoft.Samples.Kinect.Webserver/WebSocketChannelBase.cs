//------------------------------------------------------------------------------
// <copyright file="WebSocketChannelBase.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Net.WebSockets;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Threading;

    /// <summary>
    /// Base class representing a web socket communication channel.
    /// </summary>
    /// <remarks>
    /// This class has asynchronous functionality but it is NOT thread-safe, so it is expected
    /// to be called by a single-threaded scheduler, e.g.: one running over a Dispatcher or
    /// other SynchronizationContext implementation.
    /// </remarks>
    public class WebSocketChannelBase : IDisposable
    {
        /// <summary>
        /// Maximum time allowed to pass between successive checks for web socket disconnection.
        /// </summary>
        private static readonly TimeSpan DisconnectionCheckTimeout = TimeSpan.FromSeconds(2.0);

        /// <summary>
        /// Timer used to ensure we periodically check for disconnection even if no messages are
        /// being sent between server and client.
        /// </summary>
        /// <remarks>
        /// The disconnection timer helps us notice that there is a web socket resource ready to
        /// be disposed.
        /// </remarks>
        private readonly DispatcherTimer disconnectionCheckTimer = new DispatcherTimer();

        /// <summary>
        /// Action to perform when web socket becomes closed.
        /// </summary>
        private readonly Action<WebSocketChannelBase> closedAction;

        /// <summary>
        /// Non-null if someone has requested that this object be closed and disposed. Null otherwise.
        /// </summary>
        private Task disposingTask;

        /// <summary>
        /// Non-null if channel is currently sending a message. Null otherwise.
        /// </summary>
        private Task sendingTask;

        /// <summary>
        /// True if we're performing the protocol close handshake and notifying clients
        /// that socket has been closed.
        /// </summary>
        private bool isClosing;

        /// <summary>
        /// True if this object has already been disposed. False otherwise.
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// True if Closed event has been sent already. False otherwise.
        /// </summary>
        private bool closedSent;

        /// <summary>
        /// True if disconnection monitoring task has been started, false otherwise.
        /// </summary>
        private bool isDisconnectionMonitorStarted;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketChannelBase"/> class.
        /// </summary>
        /// <param name="context">
        /// Web socket context.
        /// </param>
        /// <param name="closedAction">
        /// Action to perform when web socket becomes closed.
        /// </param>
        protected WebSocketChannelBase(WebSocketContext context, Action<WebSocketChannelBase> closedAction)
        {
            if ((context == null) || (context.WebSocket == null))
            {
                throw new ArgumentNullException("context", @"Context and associated web socket must not be null");
            }

            this.Socket = context.WebSocket;
            this.CancellationTokenSource = new CancellationTokenSource();

            this.disconnectionCheckTimer.Interval = DisconnectionCheckTimeout;
            this.disconnectionCheckTimer.Tick += this.OnDisconnectionCheckTimerTick;
            this.disconnectionCheckTimer.Start();

            this.closedAction = closedAction;
        }

        /// <summary>
        /// True if this web socket channel is open for sending/receiving messages.
        /// </summary>
        public bool IsOpen
        {
            get
            {
                return !this.isDisposed && ((this.Socket.State == WebSocketState.Open) || (this.Socket.State == WebSocketState.Connecting));
            }
        }

        /// <summary>
        /// Web socket used for communications.
        /// </summary>
        protected WebSocket Socket { get; private set; }

        /// <summary>
        /// Token source used to cancel pending socket operations.
        /// </summary>
        protected CancellationTokenSource CancellationTokenSource { get; private set; }

        /// <summary>
        /// Cancel all pending socket operations.
        /// </summary>
        public void Cancel()
        {
            this.CancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Implement IDisposable.
        /// </summary>
        /// <remarks>
        /// Releases resources right away if underlying socket is already closed.
        /// Otherwise asynchronously awaits to complete web socket close handshake
        /// and then disposes resources.
        /// Call <see cref="CloseAsync"/> instead to be able to await for socket to
        /// finish disposing resources.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Asynchronous disposal. Does call Dispose(true) and GC.SuppressFinalize.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1816:CallGCSuppressFinalizeCorrectly", Justification = "Asynchronous disposal. Does call Dispose(true) and GC.SuppressFinalize.")]
        public async void Dispose()
        {
            await this.CloseAsync();
        }

        /// <summary>
        /// Cancel all pending operations, initiate web socket close handshake and send Closed
        /// event to clients  if necessary.
        /// </summary>
        /// <returns>
        /// Await-able task.
        /// </returns>
        /// <remarks>
        /// Disposes of socket resources. There is no need to call <see cref="Dispose"/> after
        /// calling this method.
        /// </remarks>
        public async Task CloseAsync()
        {
            if (this.disposingTask != null)
            {
                await this.disposingTask;
                return;
            }

            this.disposingTask = this.CloseAndDisposeAsync();
            await this.disposingTask;
        }

        /// <summary>
        /// Determine if this web socket channel is open for sending/receiving messages
        /// or if it has been closed
        /// </summary>
        /// <returns>
        /// True if this web socket channel is still open, false otherwise.
        /// </returns>
        /// <remarks>
        /// This call is expected to perform more comprehensive connection state checks
        /// than IsOpen property, which might include sending remote messages, if the
        /// specific <see cref="WebSocketChannelBase"/> subclass warrants it, so callers
        /// should be careful not to call this method too often.
        /// </remarks>
        public virtual bool CheckConnectionStatus()
        {
            return this.IsOpen;
        }

        /// <summary>
        /// Try to establish a web socket context from the specified HTTP request context.
        /// </summary>
        /// <param name="listenerContext">
        /// HTTP listener context.
        /// </param>
        /// <returns>
        /// A web socket context if communications channel was successfully established.
        /// Null if web socket channel could not be established.
        /// </returns>
        /// <remarks>
        /// If <paramref name="listenerContext"/> does not represent a web socket request, or if
        /// web socket channel could not be established, an appropriate status code will be
        /// returned via <paramref name="listenerContext"/>'s Response property, and the return
        /// value will be null.
        /// </remarks>
        protected static async Task<WebSocketContext> HandleWebSocketRequestAsync(HttpListenerContext listenerContext)
        {
            if (!listenerContext.Request.IsWebSocketRequest)
            {
                listenerContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                listenerContext.Response.Close();
                return null;
            }

            try
            {
                return await listenerContext.AcceptWebSocketAsync(null);
            }
            catch (WebSocketException)
            {
                listenerContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                listenerContext.Response.Close();
                return null;
            }
        }

        /// <summary>
        /// Sends data over the WebSocket connection asynchronously.
        /// </summary>
        /// <param name="buffer">
        /// The buffer to be sent over the connection.
        /// </param>
        /// <param name="messageType">
        /// Indicates whether the application is sending a binary or text message.
        /// </param>
        /// <returns>
        /// true if the message was sent successfully. false otherwise.
        /// </returns>
        protected async Task<bool> SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType)
        {
            if (await this.CheckForDisconnectionAsync())
            {
                return false;
            }

            if (this.sendingTask != null)
            {
                Trace.TraceError("Channel is unable to start sending a new websocket message while it is already sending a previous message.");
                return false;
            }

            bool result = true;

            // We create a separate task from the one corresponding to the WebSocket.SendAsync
            // method call, because we need to provide a guarantee to other parts of the code
            // waiting on this task that SendAsync method call has returned by the time the
            // sendingTask has completed.
            // If we don't do this, it would be possible for other code awaiting on task to
            // start executing before SendAsync stack frame gets a chance to clear the "sending"
            // state to get ready to receive other calls that initiate data sending.
            this.sendingTask = SharedConstants.CreateNonstartedTask();

            try
            {
                await this.Socket.SendAsync(buffer, messageType, true, this.CancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                if (!IsSendReceiveException(e))
                {
                    throw;
                }

                result = false;
            }
            finally
            {
                this.sendingTask.Start();
                this.sendingTask = null;
            }

            if (!result)
            {
                // Client might have disconnected
                await this.CheckForDisconnectionAsync();
            }

            return result;
        }

        /// <summary>
        /// Receives data from the WebSocket connection asynchronously.
        /// </summary>
        /// <param name="buffer">
        /// References the application buffer that is the storage location for the received data.
        /// </param>
        /// <returns>
        /// A receive result representing a full message if we could receive one successfully
        /// within the specified buffer. null otherwise.
        /// </returns>
        /// <remarks>
        /// This method receives data from socket until either we get to the end of message sent
        /// by socket client or we fill the specified buffer. If we don't get to end of message
        /// within the allocated buffer space, we return a result value indicating that message
        /// is still incomplete.
        /// </remarks>
        protected async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer)
        {
            if (await this.CheckForDisconnectionAsync())
            {
                return null;
            }

            int receiveOffset = 0;
            int receiveCount = 0;
            bool isResponseComplete = false;
            WebSocketReceiveResult receiveResult = null;
                
            try
            {
                while (!isResponseComplete)
                {
                    if (receiveCount >= buffer.Count)
                    {
                        // If we've filled up the buffer and response message is still not
                        // complete, we won't have space for response at all, so just return
                        // incomplete message.
                        return new WebSocketReceiveResult(
                            receiveCount, receiveResult != null ? receiveResult.MessageType : WebSocketMessageType.Text, false);
                    }

                    receiveResult = await this.Socket.ReceiveAsync(new ArraySegment<byte>(buffer.Array, receiveOffset + buffer.Offset, buffer.Count - receiveOffset), this.CancellationTokenSource.Token);

                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        await this.SendCloseMessagesAsync(Properties.Resources.SocketClosedByClient, true);
                        return null;
                    }

                    receiveCount += receiveResult.Count;
                    receiveOffset += receiveResult.Count;
                    isResponseComplete = receiveResult.EndOfMessage;
                }

                receiveResult = new WebSocketReceiveResult(receiveCount, receiveResult.MessageType, true);
            }
            catch (Exception e)
            {
                if (!IsSendReceiveException(e))
                {
                    throw;
                }

                return null;
            }

            return receiveResult;
        }

        /// <summary>
        /// Dispose resources owned by this object.
        /// </summary>
        /// <param name="disposing">
        /// True if called from IDisposable.Dispose. False if called by runtime during
        /// finalization.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                if (disposing)
                {
                    this.disconnectionCheckTimer.Stop();

                    this.Socket.Dispose();
                    this.Socket = null;

                    this.CancellationTokenSource.Dispose();
                    this.CancellationTokenSource = null;
                }

                this.isDisposed = true;
            }
        }

        /// <summary>
        /// Start monitoring for client disconnection requests.
        /// </summary>
        /// <remarks>
        /// Should not be called if legitimate (non-disconnection request) messages are
        /// expected from client.
        /// </remarks>
        protected async void StartDisconnectionMonitor()
        {
            if (this.isDisposed)
            {
                return;
            }

            if (this.isDisconnectionMonitorStarted)
            {
                // Monitor is already started
                return;
            }

            this.isDisconnectionMonitorStarted = true;
            var dummyBuffer = new byte[1];

            try
            {
                WebSocketReceiveResult result;

                do
                {
                    // We don't use a real cancellation token because explicitly cancelling a
                    // send or receive operation will put the socket in "Aborted" state, and
                    // it will appear to client as if we have forcefully closed the connection,
                    // when in reality our goal is just to passively monitor for client closing
                    // requests until socket connection is closed by either party.
                    var receiveTask = this.Socket.ReceiveAsync(new ArraySegment<byte>(dummyBuffer), CancellationToken.None);
                    await receiveTask;

                    if (!receiveTask.IsCompleted)
                    {
                        return;
                    }

                    result = receiveTask.Result;
                }
                while (result.MessageType != WebSocketMessageType.Close); // If message received was not a close message, keep looping.

                // We have received a close message, so initiate closing actions.
                await this.SendCloseMessagesAsync(Properties.Resources.SocketClosedByClient, true);
            }
            catch (WebSocketException)
            {
                // If connection closing is server-driven, our call to receive data will throw
                // a WebSocketException and we won't receive any closing message from client.
            }
        }

        /// <summary>
        /// Determine if the specified exception is one of the standard web socket send/receive
        /// exceptions.
        /// </summary>
        /// <param name="ex">
        /// Caught exception.
        /// </param>
        /// <returns>
        /// True if exception is of a type recognized as a standard web socket send/receive
        /// exception.
        /// </returns>
        private static bool IsSendReceiveException(Exception ex)
        {
            return (ex is WebSocketException) || (ex is HttpListenerException) || (ex is OperationCanceledException);
        }

        /// <summary>
        /// Checks if socket has been disconnected
        /// </summary>
        /// <returns>
        /// true if web socket has been disconnected already. false otherwise.
        /// </returns>
        private async Task<bool> CheckForDisconnectionAsync()
        {
            if (this.isDisposed || this.isClosing)
            {
                return true;
            }

            // Every time we check for disconnection, stop the disconnection check timer
            this.disconnectionCheckTimer.Stop();

            bool isDisconnected = !this.IsOpen;

            if (!isDisconnected)
            {
                // re-start timer if we're still connected
                this.disconnectionCheckTimer.Start();
            }
            else
            {
                this.CancellationTokenSource.Cancel();
                await this.SendCloseMessagesAsync(Properties.Resources.SocketClientDisconnectionDetected, false);
            }

            return isDisconnected;
        }

        /// <summary>
        /// Send Closed event if it hasn't been sent already.
        /// </summary>
        private void SendClosed()
        {
            if (!this.closedSent)
            {
                if (this.closedAction != null)
                {
                    this.closedAction(this);
                }

                this.closedSent = true;
            }
        }

        /// <summary>
        /// Cancel all pending operations, initiate web socket close handshake, send Closed
        /// event to clients if necessary, and dispose of socket resources.
        /// </summary>
        /// <returns>
        /// Await-able task.
        /// </returns>
        private async Task CloseAndDisposeAsync()
        {
            if (!await this.CheckForDisconnectionAsync())
            {
                this.CancellationTokenSource.Cancel();

                await this.SendCloseMessagesAsync(Properties.Resources.SocketClosedByServer, false);
            }

            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gracefully close socket by performing the protocol close handshake and notify clients
        /// that socket has been closed.
        /// </summary>
        /// <param name="closeMessage">
        /// Human readable explanation as to why the connection is being closed.
        /// </param>
        /// <param name="awaitAcknowledgement">
        /// True if we should wait for client acknowledgement of socket close request.
        /// False if we shouldn't wait.
        /// </param>
        /// <returns>
        /// Await-able task.
        /// </returns>
        private async Task SendCloseMessagesAsync(string closeMessage, bool awaitAcknowledgement)
        {
            // If we're already closing, there's no need to start closing again
            if (this.isClosing)
            {
                return;
            }

            this.isClosing = true;

            try
            {
                // Store task reference because instance variable could be set to null while
                // we await, and await operator will reference task again once we are done
                // awaiting.
                var task = this.sendingTask;
                if (task != null)
                {
                    // Wait for the sending task to complete, if it is pending.
                    await task;
                }
            }
            catch (Exception e)
            {
                if (!IsSendReceiveException(e))
                {
                    throw;
                }
            }

            try
            {
                if (awaitAcknowledgement)
                {
                    await this.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, closeMessage, CancellationToken.None);
                }
                else
                {
                    await this.Socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, closeMessage, CancellationToken.None);
                }
            }
            catch (Exception e)
            {
                if (!IsSendReceiveException(e))
                {
                    throw;
                }

                Trace.TraceWarning(
                    "Problem encountered while closing socket. Client might have gone away abruptly without initiating socket close handshake.\n{0}",
                    e);
            }

            this.SendClosed();
        }

        /// <summary>
        /// Handler for Tick event of disconnection check timer.
        /// </summary>
        /// <param name="sender">
        /// Object that sent this event.
        /// </param>
        /// <param name="args">
        /// Event arguments.
        /// </param>
        private async void OnDisconnectionCheckTimerTick(object sender, EventArgs args)
        {
            await this.CheckForDisconnectionAsync();
        }
    }
}
