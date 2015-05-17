// -----------------------------------------------------------------------
// <copyright file="UserTrackingIdChangedEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver.Sensor.Serialization
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Event message used to notify clients that a user tracking id has changed.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Lower case names allowed for JSON serialization.")]
    internal class UserTrackingIdChangedEventMessage : EventMessage
    {
        /// <summary>
        /// Old user tracking identifier.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "old", Justification = "Lower case names allowed for JSON serialization.")]
        public int oldValue { get; set; }

        /// <summary>
        /// New user tracking identifier.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "new", Justification = "Lower case names allowed for JSON serialization.")]
        public int newValue { get; set; }
    }
}
