//------------------------------------------------------------------------------
// <copyright file="EventMessage.cs" company="Microsoft">
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
    public class EventMessage
    {
        /// <summary>
        /// Category to which this event pertains.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "category", Justification = "Lower case names allowed for JSON serialization.")]
        public string category { get; set; }

        /// <summary>
        /// Type of event being sent.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "event", Justification = "Lower case names allowed for JSON serialization.")]
        public string eventType { get; set; }
    }
}
