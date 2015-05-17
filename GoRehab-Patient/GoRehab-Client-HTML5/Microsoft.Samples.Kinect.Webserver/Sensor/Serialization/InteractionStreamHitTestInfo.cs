// -----------------------------------------------------------------------
// <copyright file="InteractionStreamHitTestInfo.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver.Sensor.Serialization
{
    using System.Diagnostics.CodeAnalysis;

    using Microsoft.Kinect.Toolkit.Interaction;

    /// <summary>
    /// Represents interaction information available for a specified location in UI.
    /// </summary>
    /// <remarks>
    /// This class is equivalent to <see cref="InteractionInfo"/>, but meant for serializing
    /// from JSON responses from a web client.
    /// </remarks>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Lower case names allowed for JSON serialization.")]
    internal class InteractionStreamHitTestInfo
    {
        /// <summary>
        /// True if interaction target can respond to press actions.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "is", Justification = "Lower case names allowed for JSON serialization.")]
        public bool isPressTarget { get; set; }

        /// <summary>
        /// Identifier for control that corresponds to UI location of interest.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "press", Justification = "Lower case names allowed for JSON serialization.")]
        public string pressTargetControlId { get; set; }

        /// <summary>
        /// X-coordinate of point towards which user's hand pointer should be
        /// attracted as the user's hand performs a press action.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "press", Justification = "Lower case names allowed for JSON serialization.")]
        public double pressAttractionPointX { get; set; }

        /// <summary>
        /// Y-coordinate of point towards which user's hand pointer should be
        /// attracted as the user's hand performs a press action.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "press", Justification = "Lower case names allowed for JSON serialization.")]
        public double pressAttractionPointY { get; set; }

        /// <summary>
        /// True if interaction target can respond to grip/release actions.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "is", Justification = "Lower case names allowed for JSON serialization.")]
        public bool isGripTarget { get; set; }
    }
}
