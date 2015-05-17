// -----------------------------------------------------------------------
// <copyright file="SensorStatusEventMessage.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver.Sensor.Serialization
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Serializable representation of an sensor status event message to send to client.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter",
        Justification = "Lower case names allowed for JSON serialization.")]
    internal class SensorStatusEventMessage : EventMessage
    {
        /// <summary>
        /// status of sensor.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "connected", Justification = "Lower case names allowed for JSON serialization.")]
        public bool connected { get; set; }
    }
}
