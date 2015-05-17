// -----------------------------------------------------------------------
// <copyright file="InteractionStreamMessage.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver.Sensor.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    using Microsoft.Kinect.Toolkit.Interaction;

    /// <summary>
    /// Serializable representation of an interaction stream message to send to client.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter",
        Justification = "Lower case names allowed for JSON serialization.")]
    public class InteractionStreamMessage : StreamMessage
    {
        /// <summary>
        /// Maximum number of hand pointers we can track.
        /// </summary>
        private const int MaximumHandPointers = 4; // 2 hands for 2 tracked users

        /// <summary>
        /// Internal array of MessageHandPointer objects, allocated once and then reused.
        /// </summary>
        private readonly MessageHandPointer[] internalHandPointers;

        /// <summary>
        /// Initializes a new instance of the <see cref="InteractionStreamMessage"/> class.
        /// </summary>
        public InteractionStreamMessage()
        {
            this.internalHandPointers = new MessageHandPointer[MaximumHandPointers];

            for (int i = 0; i < this.internalHandPointers.Length; ++i)
            {
                this.internalHandPointers[i] = new MessageHandPointer();
            }
        }

        /// <summary>
        /// Serializable hand pointer array.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "hand", Justification = "Lower case names allowed for JSON serialization.")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Array properties allowed for JSON serialization.")]
        public MessageHandPointer[] handPointers { get; set; }

        /// <summary>
        /// Update hand pointers from specified user info data.
        /// </summary>
        /// <param name="userInfoData">
        /// Enumeration of UserInfo structures.
        /// </param>
        /// <param name="primaryUserTrackingId">
        /// Tracking ID of primary user.
        /// </param>
        public void UpdateHandPointers(IEnumerable<UserInfo> userInfoData, int primaryUserTrackingId)
        {
            int handPointerIndex = 0;

            if (userInfoData == null)
            {
                throw new ArgumentNullException("userInfoData");
            }

            foreach (var user in userInfoData)
            {
                foreach (var handPointer in user.HandPointers)
                {
                    if (user.SkeletonTrackingId == SharedConstants.InvalidUserTrackingId)
                    {
                        continue;
                    }

                    var messageHandPointer = this.internalHandPointers[handPointerIndex];
                    messageHandPointer.trackingId = user.SkeletonTrackingId;
                    messageHandPointer.handType = handPointer.HandType.ToString();
                    messageHandPointer.isTracked = handPointer.IsTracked;
                    messageHandPointer.isActive = handPointer.IsActive;
                    messageHandPointer.isInteractive = handPointer.IsInteractive;
                    messageHandPointer.isPressed = handPointer.IsPressed;
                    messageHandPointer.isPrimaryHandOfUser = handPointer.IsPrimaryForUser;
                    messageHandPointer.isPrimaryUser = primaryUserTrackingId == user.SkeletonTrackingId;
                    messageHandPointer.handEventType = handPointer.HandEventType.ToString();
                    messageHandPointer.x = handPointer.X;
                    messageHandPointer.y = handPointer.Y;
                    messageHandPointer.pressExtent = handPointer.PressExtent;
                    messageHandPointer.rawX = handPointer.RawX;
                    messageHandPointer.rawY = handPointer.RawY;
                    messageHandPointer.rawZ = handPointer.RawZ;

                    if (++handPointerIndex >= MaximumHandPointers)
                    {
                        break;
                    }
                }

                if (handPointerIndex >= MaximumHandPointers)
                {
                    break;
                }
            }

            if ((this.handPointers == null) || (this.handPointers.Length != handPointerIndex))
            {
                this.handPointers = new MessageHandPointer[handPointerIndex];
            }

            for (handPointerIndex = 0; handPointerIndex < this.handPointers.Length; ++handPointerIndex)
            {
                this.handPointers[handPointerIndex] = this.internalHandPointers[handPointerIndex];
            }
        }
    }
}