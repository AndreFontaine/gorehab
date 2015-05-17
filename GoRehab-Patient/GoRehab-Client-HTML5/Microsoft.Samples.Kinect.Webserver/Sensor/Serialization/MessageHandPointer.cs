// -----------------------------------------------------------------------
// <copyright file="MessageHandPointer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver.Sensor.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;

    /// <summary>
    /// Serializable representation of a hand pointer.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Lower case names allowed for JSON serialization.")]
    [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Used by serialization code")]
    public class MessageHandPointer
    {
        /// <summary>
        /// User tracking Id corresponding to this hand pointer.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "tracking", Justification = "Lower case names allowed for JSON serialization.")]
        public int trackingId { get; set; }

        /// <summary>
        /// Indicates whether this hand pointer refers to right or left hand.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "hand", Justification = "Lower case names allowed for JSON serialization.")]
        public string handType { get; set; }

        /// <summary>
        /// Indicates which event type (grip/release) is currently triggered, if any.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "hand", Justification = "Lower case names allowed for JSON serialization.")]
        public string handEventType { get; set; }

        /// <summary>
        /// Indicates whether this hand pointer is currently being tracked.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "is", Justification = "Lower case names allowed for JSON serialization.")]
        public bool isTracked { get; set; }

        /// <summary>
        /// Indicates whether hand is one of possibly several candidates to be labeled as
        /// the primary hand for the user.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "is", Justification = "Lower case names allowed for JSON serialization.")]
        public bool isActive { get; set; }

        /// <summary>
        /// Indicates whether hand is within physical interaction region that maps to the
        /// user interface.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "is", Justification = "Lower case names allowed for JSON serialization.")]
        public bool isInteractive { get; set; }

        /// <summary>
        /// Indicates whether  hand is currently performing "press" action.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "is", Justification = "Lower case names allowed for JSON serialization.")]
        public bool isPressed { get; set; }

        /// <summary>
        /// Indicates whether this hand is the primary hand for corresponding user.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "is", Justification = "Lower case names allowed for JSON serialization.")]
        public bool isPrimaryHandOfUser { get; set; }

        /// <summary>
        /// Indicates whether corresponding user is the primary user.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "is", Justification = "Lower case names allowed for JSON serialization.")]
        public bool isPrimaryUser { get; set; }

        /// <summary>
        /// Gets interaction-adjusted X-coordinate of hand pointer position.
        /// </summary>
        /// <remarks>
        /// 0.0 corresponds to left edge of interaction region and 1.0 corresponds to right edge
        /// of interaction region, but values could be outside of this range.
        /// </remarks>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "x", Justification = "Lower case names allowed for JSON serialization.")]
        public double x { get; set; }

        /// <summary>
        /// Gets interaction-adjusted Y-coordinate of hand pointer position.
        /// </summary>
        /// <remarks>
        /// 0.0 corresponds to top edge of interaction region and 1.0 corresponds to bottom edge
        /// of interaction region, but values could be outside of this range.
        /// </remarks>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "y", Justification = "Lower case names allowed for JSON serialization.")]
        public double y { get; set; }

        /// <summary>
        /// Gets a interaction-adjusted measure of how much pressing is being performed by hand.
        /// </summary>
        /// <remarks>
        /// 0.0 means that hand is not performing a press action at all, and 1.0 means that
        /// hand is at the trigger point for press action, but values could be greater than 1.0.
        /// </remarks>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "press", Justification = "Lower case names allowed for JSON serialization.")]
        public double pressExtent { get; set; }

        /// <summary>
        /// Gets X-coordinate of hand pointer position.
        /// </summary>
        /// <remarks>
        /// 0.0 corresponds to left edge of interaction region and 1.0 corresponds to right edge
        /// of interaction region, but values could be outside of this range.
        /// </remarks>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "raw", Justification = "Lower case names allowed for JSON serialization.")]
        public double rawX { get; set; }

        /// <summary>
        /// Gets Y-coordinate of hand pointer position.
        /// </summary>
        /// <remarks>
        /// 0.0 corresponds to top edge of interaction region and 1.0 corresponds to bottom edge
        /// of interaction region, but values could be outside of this range.
        /// </remarks>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "raw", Justification = "Lower case names allowed for JSON serialization.")]
        public double rawY { get; set; }

        /// <summary>
        /// Gets a measure of how much pressing is being performed by hand.
        /// </summary>
        /// <remarks>
        /// 0.0 means that hand is not performing a press action at all, and 1.0 means that
        /// hand is at the trigger point for press action, but values could be greater than 1.0.
        /// </remarks>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "raw", Justification = "Lower case names allowed for JSON serialization.")]
        public double rawZ { get; set; }

        public static implicit operator MessageHandPointer(Dictionary<string, object> handPointerDictionary)
        {
            return FromDictionary(handPointerDictionary);
        }

        public static MessageHandPointer FromDictionary(Dictionary<string, object> handPointerDictionary)
        {
            if (handPointerDictionary == null)
            {
                throw new ArgumentNullException("handPointerDictionary");
            }

            var messageHandPointer = new MessageHandPointer
                                     {
                                         trackingId = (int)handPointerDictionary["trackingId"],
                                         handType = (string)handPointerDictionary["handType"],
                                         handEventType = (string)handPointerDictionary["handEventType"],
                                         isTracked = (bool)handPointerDictionary["isTracked"],
                                         isActive = (bool)handPointerDictionary["isActive"],
                                         isInteractive = (bool)handPointerDictionary["isInteractive"],
                                         isPressed = (bool)handPointerDictionary["isPressed"],
                                         isPrimaryHandOfUser = (bool)handPointerDictionary["isPrimaryHandOfUser"],
                                         isPrimaryUser = (bool)handPointerDictionary["isPrimaryUser"],
                                         x =
                                             double.Parse(
                                                 handPointerDictionary["x"].ToString(), CultureInfo.InvariantCulture),
                                         y =
                                             double.Parse(
                                                 handPointerDictionary["y"].ToString(), CultureInfo.InvariantCulture),
                                         pressExtent =
                                             double.Parse(
                                                 handPointerDictionary["pressExtent"].ToString(),
                                                 CultureInfo.InvariantCulture),
                                         rawX =
                                             double.Parse(
                                                 handPointerDictionary["rawX"].ToString(), CultureInfo.InvariantCulture),
                                         rawY =
                                             double.Parse(
                                                 handPointerDictionary["rawY"].ToString(), CultureInfo.InvariantCulture),
                                         rawZ =
                                             double.Parse(
                                                 handPointerDictionary["rawZ"].ToString(), CultureInfo.InvariantCulture)
                                     };
            return messageHandPointer;
        }
    }
}