//------------------------------------------------------------------------------
// <copyright file="StreamMessage.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver.Sensor.Serialization
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Base class for web socket messages sent over "stream" endpoint.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Lower case names allowed for JSON serialization.")]
    public class StreamMessage
    {
        /// <summary>
        /// Name of stream that owns this message.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "stream", Justification = "Lower case names allowed for JSON serialization.")]
        public string stream { get; set; }

        /// <summary>
        /// Stream message timestamp.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "timestamp", Justification = "Lower case names allowed for JSON serialization.")]
        public long timestamp { get; set; }
    }
}
