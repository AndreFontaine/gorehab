// -----------------------------------------------------------------------
// <copyright file="IUserStateManager.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver.Sensor
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Kinect;
    using Microsoft.Kinect.Toolkit.Interaction;

    /// <summary>
    /// Interface for objects that keep track of state of users associated with a specific
    /// Kinect sensor.
    /// </summary>
    public interface IUserStateManager
    {
        /// <summary>
        /// Event triggered whenever user state changes.
        /// </summary>
        event EventHandler<UserStateChangedEventArgs> UserStateChanged;

        /// <summary>
        /// Dictionary mapping user tracking Ids to names used for states corresponding to
        /// those users.
        /// </summary>
        IDictionary<int, string> UserStates { get; }

        /// <summary>
        /// Tracking id of primary user associated with UI interactions.
        /// </summary>
        int PrimaryUserTrackingId { get; }

        /// <summary>
        /// Determines which users should be tracked in the future, based on selection
        /// metrics and engagement state.
        /// </summary>
        /// <param name="frameSkeletons">
        /// Array of skeletons from which the appropriate user tracking Ids will be selected.
        /// </param>
        /// <param name="timestamp">
        /// Timestamp from skeleton frame.
        /// </param>
        /// <param name="chosenTrackingIds">
        /// Array that will contain the tracking Ids of users to track, sorted from most
        /// important to least important user to track.
        /// </param>
        void ChooseTrackedUsers(Skeleton[] frameSkeletons, long timestamp, int[] chosenTrackingIds);

        /// <summary>
        /// Called whenever the set of tracked users has changed.
        /// </summary>
        /// <param name="trackedUserInfo">
        /// User information from which we'll update the set of tracked users and the primary user.
        /// </param>
        /// <param name="timestamp">
        /// Interaction frame timestamp corresponding to given user information.
        /// </param>
        void UpdateUserInformation(IEnumerable<UserInfo> trackedUserInfo, long timestamp);

        /// <summary>
        /// Clear out all user state and start from scratch.
        /// </summary>
        void Reset();
    }
}
