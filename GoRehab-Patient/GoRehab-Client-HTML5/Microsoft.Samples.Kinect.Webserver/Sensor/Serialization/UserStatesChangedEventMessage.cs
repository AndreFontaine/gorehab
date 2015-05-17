// -----------------------------------------------------------------------
// <copyright file="UserStatesChangedEventMessage.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver.Sensor.Serialization
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Event message used to notify clients that user states have changed.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Lower case names allowed for JSON serialization.")]
    internal class UserStatesChangedEventMessage : EventMessage
    {
        /// <summary>
        /// New map of user tracking IDs to user state names.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "user", Justification = "Lower case names allowed for JSON serialization.")]
        public StateMappingEntry[] userStates { get; set; }
    }
}
