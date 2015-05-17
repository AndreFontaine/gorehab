// -----------------------------------------------------------------------
// <copyright file="WebSocketMessageExtensions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver
{
    using System;
    using System.IO;
    using System.Net.WebSockets;

    /// <summary>
    /// Static class that defines extensions used to serialize objects into web socket messages.
    /// </summary>
    public static class WebSocketMessageExtensions
    {
        /// <summary>
        /// Serializes object as a UTF8-encoded JSON string and creates a web socket message to
        /// be sent over the wire.
        /// </summary>
        /// <typeparam name="T">
        /// Type of object to serialize and send as a message.
        /// </typeparam>
        /// <param name="obj">
        /// Object to serialize and send as a message.
        /// </param>
        /// <returns>
        /// Web socket message ready to be sent.
        /// </returns>
        public static WebSocketMessage ToTextMessage<T>(this T obj)
        {
            using (var stream = new MemoryStream())
            {
                obj.ToJson(stream);
                return new WebSocketMessage(new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length), WebSocketMessageType.Text);
            }
        }

        /// <summary>
        /// Takes an array of bytes representing UTF8-encoded text and creates a web socket message
        /// to be sent over the wire.
        /// </summary>
        /// <param name="textData">
        /// Array of bytes representing UTF8-encoded text.
        /// </param>
        /// <returns>
        /// Web socket message ready to be sent.
        /// </returns>
        public static WebSocketMessage ToTextMessage(this byte[] textData)
        {
            return new WebSocketMessage(new ArraySegment<byte>(textData), WebSocketMessageType.Text);
        }

        /// <summary>
        /// Takes an array of bytes representing binary data and creates a web socket message
        /// to be sent over the wire.
        /// </summary>
        /// <param name="data">
        /// Array of bytes representing binary data.
        /// </param>
        /// <returns>
        /// Web socket message ready to be sent.
        /// </returns>
        public static WebSocketMessage ToBinaryMessage(this byte[] data)
        {
            return new WebSocketMessage(new ArraySegment<byte>(data), WebSocketMessageType.Binary);
        }
    }
}
