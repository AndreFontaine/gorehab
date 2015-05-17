// -----------------------------------------------------------------------
// <copyright file="ImageHeaderStreamMessage.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver.Sensor.Serialization
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Serializable representation of a stream message that represents an image message header to send to client.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Lower case names allowed for JSON serialization.")]
    public class ImageHeaderStreamMessage : StreamMessage
    {
        /// <summary>
        /// Image width
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "width", Justification = "Lower case names allowed for JSON serialization.")]
        public int width { get; set; }

        /// <summary>
        /// Image height.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "height", Justification = "Lower case names allowed for JSON serialization.")]
        public int height { get; set; }

        /// <summary>
        /// Number of bytes in image buffer.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "buffer", Justification = "Lower case names allowed for JSON serialization.")]
        public int bufferLength { get; set; }
    }
}