//------------------------------------------------------------------------------
// <copyright file="SensorStreamHandlerContext.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver.Sensor
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Samples.Kinect.Webserver.Sensor.Serialization;

    /// <summary>
    /// Provides context through which an instance of <see cref="ISensorStreamHandler"/> can
    /// communicate back with its owner.
    /// </summary>
    public sealed class SensorStreamHandlerContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SensorStreamHandlerContext"/> class.
        /// </summary>
        /// <param name="sendTwoPartStreamMessageAsync">
        /// Function used to asynchronously send two-part (textual header plus binary payload) stream
        /// message to client(s) of stream handler. If binary (second) part of message is null, only
        /// textual header (first) part will be sent.
        /// </param>
        /// <param name="sendEventMessageAsync">
        /// Function used to asynchronously send event message to client(s) of stream handler.
        /// </param>
        public SensorStreamHandlerContext(
            Func<StreamMessage, byte[], Task> sendTwoPartStreamMessageAsync,
            Func<EventMessage, Task> sendEventMessageAsync)
        {
            this.SendTwoPartStreamMessageAsync = sendTwoPartStreamMessageAsync;
            this.SendStreamMessageAsync = message => sendTwoPartStreamMessageAsync(message, null);
            this.SendEventMessageAsync = sendEventMessageAsync;
        }

        /// <summary>
        /// Function used to asynchronously send stream message to client(s) of stream handler.
        /// </summary>
        public Func<StreamMessage, Task> SendStreamMessageAsync { get; private set; }

        /// <summary>
        /// Function used to asynchronously send two-part (textual header plus binary payload) stream
        /// message to client(s) of stream handler. If binary (second) part of message is null, only
        /// textual header (first) part will be sent.
        /// </summary>
        public Func<StreamMessage, byte[], Task> SendTwoPartStreamMessageAsync { get; private set; }

        /// <summary>
        /// Function used to asynchronously send event message to client(s) of stream handler.
        /// </summary>
        public Func<EventMessage, Task> SendEventMessageAsync { get; private set; }
    }
}
