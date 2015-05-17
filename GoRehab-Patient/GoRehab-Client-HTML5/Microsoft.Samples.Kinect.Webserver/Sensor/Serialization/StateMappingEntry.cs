// -----------------------------------------------------------------------
// <copyright file="StateMappingEntry.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver.Sensor.Serialization
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Represents a single entry mapping a user tracking id to a user state.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Lower case names allowed for JSON serialization.")]
    internal class StateMappingEntry
    {
        /// <summary>
        /// User tracking id.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "id", Justification = "Lower case names allowed for JSON serialization.")]
        public int id { get; set; }

        /// <summary>
        /// User state associated with tracking id
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "user", Justification = "Lower case names allowed for JSON serialization.")]
        public string userState { get; set; }
    }
}
