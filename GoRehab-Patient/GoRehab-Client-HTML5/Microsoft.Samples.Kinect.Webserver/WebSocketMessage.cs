//------------------------------------------------------------------------------
// <copyright file="WebSocketMessage.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver
{
    using System;
    using System.Net.WebSockets;

    /// <summary>
    /// Represents a web socket message with associated type (UTF8 versus binary).
    /// </summary>
    public class WebSocketMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketMessage"/> class.
        /// </summary>
        /// <param name="messageContent">
        /// Message content.
        /// </param>
        /// <param name="messageType">
        /// Type of message (UTF8 versus binary).
        /// </param>
        public WebSocketMessage(ArraySegment<byte> messageContent, WebSocketMessageType messageType)
        {
            this.Content = messageContent;
            this.MessageType = messageType;
        }

        /// <summary>
        /// Message content.
        /// </summary>
        public ArraySegment<byte> Content { get; private set; }

        /// <summary>
        /// Message type (UTF8 text versus binary).
        /// </summary>
        public WebSocketMessageType MessageType { get; private set; }
    }
}
