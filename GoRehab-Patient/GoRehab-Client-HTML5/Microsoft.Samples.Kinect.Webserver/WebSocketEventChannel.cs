//------------------------------------------------------------------------------
// <copyright file="WebSocketEventChannel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using System.Net.WebSockets;
    using System.Threading.Tasks;

    /// <summary>
    /// Web socket communication channel used for sending events to clients.
    /// </summary>
    public sealed class WebSocketEventChannel : WebSocketChannelBase
    {
        /// <summary>
        /// If more than this number of send tasks are overlapping (i.e.: simultaneously
        /// awaiting to finish sending), it is considered as an indication that more
        /// events are happening at a faster pace than can be handled by the underlying
        /// web socket.
        /// </summary>
        private const int MaximumOverlappingTaskCount = 10;

        /// <summary>
        /// Keeps track of task representing the last send request initiated.
        /// </summary>
        private Task lastSendTask;

        /// <summary>
        /// Number of overlapping send tasks.
        /// </summary>
        private int overlappingTaskCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketEventChannel"/> class.
        /// </summary>
        /// <param name="context">
        /// Web socket context.
        /// </param>
        /// <param name="closedAction">
        /// Action to perform when web socket becomes closed.
        /// </param>
        internal WebSocketEventChannel(WebSocketContext context, Action<WebSocketChannelBase> closedAction)
            : base(context, closedAction)
        {
            // Always monitor for disconnections.
            this.StartDisconnectionMonitor();
        }

        /// <summary>
        /// Attempt to open a new event channel from the specified HTTP listener context.
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
        /// If <paramref name="listenerContext"/> does not represent a web socket request, or if
        /// web socket channel could not be established, an appropriate status code will be
        /// returned via <paramref name="listenerContext"/>'s Response property.
        /// </remarks>
        public static async void TryOpenAsync(
            HttpListenerContext listenerContext, Action<WebSocketEventChannel> openedAction, Action<WebSocketEventChannel> closedAction)
        {
            var socketContext = await HandleWebSocketRequestAsync(listenerContext);

            if (socketContext != null)
            {
                var channel = new WebSocketEventChannel(socketContext, closedChannel => closedAction(closedChannel as WebSocketEventChannel));
                openedAction(channel);
            }
        }

        /// <summary>
        /// Asynchronously sends a batch of messages over the web socket channel.
        /// </summary>
        /// <param name="messages">
        /// Batch of messages to be sent as an atomic block through the web socket.
        /// </param>
        /// <returns>
        /// true if the messages were sent successfully. false otherwise.
        /// </returns>
        public async Task<bool> SendMessagesAsync(params WebSocketMessage[] messages)
        {
            if (messages.Length == 0)
            {
                // No work to be done
                return true;
            }

            ++this.overlappingTaskCount;

            Task<Task<bool>> getSendTaskTask = null;

            try
            {
                if (this.overlappingTaskCount > MaximumOverlappingTaskCount)
                {
                    throw new InvalidOperationException(@"Events are being generated faster than web socket channel can handle");
                }

                // Create a function whose only purpose is to return a task representing
                // the real work that needs to be done to send the messages, and a task
                // corresponding to this function.
                // We're basically creating a linked list of tasks, where each task waits
                // for the previous task (if it exists) to finish processing before starting
                // to do its own work.
                // We do things in this way rather than adding message data to a queue that
                // then processes each message in order, to avoid the additional data copy
                // (and potential allocation) that would be required to allow for deferred
                // processing. The current contract has message data be processed in the same
                // function stack frame in which an awaiting client called us.
                Func<object, Task<bool>> getSendTaskFunction = previousTask => this.SerializedSendMessages(previousTask, messages);
                if (this.lastSendTask == null)
                {
                    getSendTaskTask = new Task<Task<bool>>(getSendTaskFunction, null);
                    getSendTaskTask.Start();
                }
                else
                {
                    getSendTaskTask = this.lastSendTask.ContinueWith(getSendTaskFunction);
                }
                
                this.lastSendTask = getSendTaskTask;

                // After awaiting for this nested task we will have a task that represents
                // the real work of actually sending the messages.
                Task<bool> sendTask = await getSendTaskTask;
                return await sendTask;
            }
            finally
            {
                // If no other send tasks came in while we were waiting for this send batch
                // to complete, clear the last send task state so that future batches don't
                // think that they are overlapping a previous batch.
                if (this.lastSendTask == getSendTaskTask)
                {
                    this.lastSendTask = null;
                }

                --this.overlappingTaskCount;

                Debug.Assert((this.overlappingTaskCount > 0) || (this.lastSendTask == null), "Whenever no more tasks overlap, there should be no pending send task to await");
            }
        }

        /// <summary>
        /// Asynchronously sends a batch of messages over the web socket channel after ensuring
        /// that all previous message batches have completed the sending process.
        /// </summary>
        /// <param name="previous">
        /// Previous message-batch-sending task that needs to complete before we start
        /// sending new messages. May be null, if this batch was requested after all previous
        /// batches had already finished sending.
        /// </param>
        /// <param name="messages">
        /// Batch of messages to send.
        /// </param>
        /// <returns>
        /// true if the messages were sent successfully. false otherwise.
        /// </returns>
        private async Task<bool> SerializedSendMessages(object previous, IEnumerable<WebSocketMessage> messages)
        {
            var previousTask = previous as Task<Task<bool>>;
            if (previousTask != null)
            {
                // The previous task, if non-null, is a task that returns the task that does
                // the real work of sending the previous batch of messages, so we need to wait
                // for the result task to finish to ensure that messages are serialized in the
                // expected order.
                await previousTask.Result;
            }

            foreach (var message in messages)
            {
                if (!await this.SendAsync(message.Content, message.MessageType))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
