//------------------------------------------------------------------------------
// <copyright file="WebSocketRpcChannel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Net.WebSockets;
    using System.Runtime.Serialization;
    using System.Threading.Tasks;
    using System.Windows.Threading;

    using Microsoft.Samples.Kinect.Webserver.Sensor.Serialization;

    /// <summary>
    /// Web socket communication channel used for server to call remote procedures exposed
    /// by web client and synchronously wait for the call to return with results.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All communications performed by this channel are sent/received as UTF8-encoded web
    /// socket text messages.
    /// </para>
    /// <para>
    /// We only support one single RPC call to be sent at a time, by any client.
    /// Calls initiated before a previous call completes will simply fail.
    /// </para>
    /// </remarks>
    public sealed class WebSocketRpcChannel : WebSocketChannelBase
    {
        /// <summary>
        /// Default number of bytes in buffer used to receive RPC responses from client.
        /// </summary>
        private const int DefaultReceiveBufferSize = 2048;

        /// <summary>
        /// Function name used to verify that client connection is still fully operational.
        /// </summary>
        private const string PingFunctionName = "ping";

        /// <summary>
        /// Buffer used to receive RPC responses from client.
        /// </summary>
        private byte[] receiveBuffer;

        /// <summary>
        /// Sequence Id of last function call performed.
        /// </summary>
        /// <remarks>
        /// This is incremented with each function call, to make sequence ids unique.
        /// </remarks>
        private int sequenceId;

        /// <summary>
        /// True if we're currently waiting for an RPC call to come back with a response.
        /// </summary>
        private bool isInCall;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketRpcChannel"/> class.
        /// </summary>
        /// <param name="context">
        /// Web socket context.
        /// </param>
        /// <param name="closedAction">
        /// Action to perform when web socket becomes closed.
        /// </param>
        /// <param name="receiveBufferSize">
        /// Number of bytes in buffer used to receive RPC responses from client.
        /// </param>
        private WebSocketRpcChannel(WebSocketContext context, Action<WebSocketChannelBase> closedAction, int receiveBufferSize)
            : base(context, closedAction)
        {
            this.receiveBuffer = new byte[receiveBufferSize];
        }

        /// <summary>
        /// Attempt to open a new RPC channel from the specified HTTP listener context.
        /// </summary>
        /// <param name="listenerContext">
        /// HTTP listener context.
        /// </param>
        /// <param name="openedAction">
        /// Action to perform when web socket is opened.
        /// Will never be called if web channel can't be opened.
        /// </param>
        /// <param name="closedAction">
        /// Action to perform when web socket is closed.
        /// Will never be called if web channel can't be opened.
        /// </param>
        /// <param name="receiveBufferSize">
        /// Number of bytes in buffer used to receive RPC responses from client.
        /// </param>
        /// <remarks>
        /// If <paramref name="listenerContext"/> does not represent a web socket request, or if
        /// web socket channel could not be established, an appropriate status code will be
        /// returned via <paramref name="listenerContext"/>'s Response property.
        /// </remarks>
        public static async void TryOpenAsync(
            HttpListenerContext listenerContext,
            Action<WebSocketRpcChannel> openedAction,
            Action<WebSocketRpcChannel> closedAction,
            int receiveBufferSize)
        {
            var socketContext = await HandleWebSocketRequestAsync(listenerContext);

            if (socketContext != null)
            {
                var channel = new WebSocketRpcChannel(
                    socketContext, closedChannel => closedAction(closedChannel as WebSocketRpcChannel), receiveBufferSize);
                openedAction(channel);
            }
        }

        /// <summary>
        /// Attempt to open a new RPC channel from the specified HTTP listener context.
        /// </summary>
        /// <param name="listenerContext">
        /// HTTP listener context.
        /// </param>
        /// <param name="openedAction">
        /// Action to perform when web socket is opened.
        /// Will never be called if web channel can't be opened.
        /// </param>
        /// <param name="closedAction">
        /// Action to perform when web socket is closed.
        /// Will never be called if web channel can't be opened.
        /// </param>
        /// <remarks>
        /// <para>
        /// If <paramref name="listenerContext"/> does not represent a web socket request, or if
        /// web socket channel could not be established, an appropriate status code will be
        /// returned via <paramref name="listenerContext"/>'s Response property.
        /// </para>
        /// <para>
        /// Will use default receive buffer size.
        /// </para>
        /// </remarks>
        public static void TryOpenAsync(
            HttpListenerContext listenerContext,
            Action<WebSocketRpcChannel> openedAction,
            Action<WebSocketRpcChannel> closedAction)
        {
            TryOpenAsync(listenerContext, openedAction, closedAction, DefaultReceiveBufferSize);
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
        public override bool CheckConnectionStatus()
        {
            if (!base.CheckConnectionStatus())
            {
                return false;
            }

            if (this.isInCall)
            {
                return true;
            }

            var result = this.CallFunction<bool>(PingFunctionName);
            var isValidConnection = result.Success && result.Result;

            // If client did not respond to our ping, but socket is still open, close it
            // because we're dealing with a client that does not respect our expected
            // communication contract.
            if (this.IsOpen && !isValidConnection)
            {
                this.Dispose();
            }

            return isValidConnection;
        }

        /// <summary>
        /// Perform synchronous RPC function call.
        /// </summary>
        /// <typeparam name="T">
        /// Type of function call result.
        /// </typeparam>
        /// <param name="functionName">
        /// Name of remote function to invoke.
        /// </param>
        /// <param name="args">
        /// Function arguments.
        /// </param>
        /// <returns>
        /// Result of RPC call.
        /// </returns>
        public RpcResult<T> CallFunction<T>(string functionName, params object[] args)
        {
            if (this.isInCall)
            {
                return new RpcResult<T>(false);
            }

            this.isInCall = true;

            try
            {
                var frame = new DispatcherFrame { Continue = true };
                RpcResult<T> result = null;
                try
                {
                    // Push dispatcher frame with a single posted message to process before
                    // breaking out of frame
                    Dispatcher.CurrentDispatcher.BeginInvoke(
                        (Action)(async () =>
                        {
                            try
                            {
                                result = await this.SendReceiveAsync<T>(functionName, args);
                            }
                            catch (Exception e)
                            {
                                Trace.TraceError("Error while sending/receiving data during remote function call:\n{0}", e);
                                result = new RpcResult<T>(false);
                            }

                            frame.Continue = false;
                        }));
                    Dispatcher.PushFrame(frame);

                    return result;
                }
                catch (AggregateException e)
                {
                    Trace.TraceError("Error while sending/receiving data during remote function call:\n{0}", e);
                    return new RpcResult<T>(false);
                }
            }
            finally
            {
                this.isInCall = false;
            }
        }

        /// <summary>
        /// Asynchronously send RPC function call request and process response, ensuring that
        /// response matches request.
        /// </summary>
        /// <typeparam name="T">
        /// Type of function call result.
        /// </typeparam>
        /// <param name="functionName">
        /// Name of remote function to invoke.
        /// </param>
        /// <param name="args">
        /// Function arguments.
        /// </param>
        /// <returns>
        /// Result of RPC call, as an await-able task.
        /// </returns>
        private async Task<RpcResult<T>> SendReceiveAsync<T>(string functionName, object[] args)
        {
            var call = new FunctionCallRequest(functionName, args, ++this.sequenceId);

            using (var callStream = new MemoryStream())
            {
                call.ToJson(callStream);
                var sendResult =
                    await
                    this.SendAsync(new ArraySegment<byte>(callStream.GetBuffer(), 0, (int)callStream.Length), WebSocketMessageType.Text);
                if (!sendResult)
                {
                    return new RpcResult<T>(false);
                }
            }

            var receiveResult = await this.ReceiveCompleteMessageAsync();
            if (receiveResult == null)
            {
                return new RpcResult<T>(false);
            }

            using (var responseStream = new MemoryStream(this.receiveBuffer, 0, receiveResult.Count))
            {
                FunctionCallResponse<T> callResponse;

                try
                {
                    callResponse = responseStream.FromJson<FunctionCallResponse<T>>();
                }
                catch (SerializationException)
                {
                    return new RpcResult<T>(false);
                }

                if (callResponse.id != call.id)
                {
                    // call and response sequence ids don't match, so call did not succeed
                    return new RpcResult<T>(false);
                }

                return new RpcResult<T>(true, callResponse.result);
            }
        }

        /// <summary>
        /// Asynchronously wait for RPC function call response, until a complete message has
        /// been received.
        /// </summary>
        /// <returns>
        /// WebSocketReceiveResult if complete, non-empty, text message has been received.
        /// Null if any failure occurred while receiving message.
        /// </returns>
        private async Task<WebSocketReceiveResult> ReceiveCompleteMessageAsync()
        {
            int receiveCount = 0;
            WebSocketReceiveResult receiveResult;

            do
            {
                receiveResult =
                    await
                    this.ReceiveAsync(new ArraySegment<byte>(this.receiveBuffer, receiveCount, this.receiveBuffer.Length - receiveCount));

                if ((receiveResult == null) || (receiveResult.MessageType != WebSocketMessageType.Text))
                {
                    return null;
                }

                receiveCount += receiveResult.Count;

                if (receiveResult.EndOfMessage)
                {
                    break;
                }

                // This can only happen if we've filled the buffer and message is still not completely
                // received, so we double the buffer size.
                Debug.Assert(receiveCount == this.receiveBuffer.Length, "ReceiveAsync method should guarantee that incomplete messages are only returned when buffer is completely full");

                var newBuffer = new byte[receiveCount * 2];
                Array.Copy(this.receiveBuffer, newBuffer, receiveCount);
                this.receiveBuffer = newBuffer;
            }
            while (!receiveResult.EndOfMessage);

            if (receiveCount == 0)
            {
                return null;
            }

            return new WebSocketReceiveResult(receiveCount, WebSocketMessageType.Text, true);
        }
    }
}
