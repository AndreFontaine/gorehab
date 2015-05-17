// -----------------------------------------------------------------------
// <copyright file="DefaultUserStateManager.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver.Sensor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Kinect;
    using Microsoft.Kinect.Toolkit;
    using Microsoft.Kinect.Toolkit.Interaction;
    using Microsoft.Samples.Kinect.Webserver.Sensor.Serialization;

    /// <summary>
    /// Default implementation of <see cref="IUserStateManager"/> interface.
    /// </summary>
    public class DefaultUserStateManager : IUserStateManager
    {
        /// <summary>
        /// Name of state representing a tracked user.
        /// </summary>
        public const string TrackedStateName = "tracked";

        /// <summary>
        /// Name of state representing an engaged user.
        /// </summary>
        public const string EngagedStateName = "engaged";

        /// <summary>
        /// Category of all events originating from this class.
        /// </summary>
        public const string EventCategory = "userState";

        /// <summary>
        /// Event type for primary user changed event.
        /// </summary>
        public const string PrimaryUserChangedEventType = "primaryUserChanged";

        /// <summary>
        /// Event type for user state changed event.
        /// </summary>
        public const string UserStatesChangedEventType = "userStatesChanged";

        /// <summary>
        /// Length (in milliseconds) of period of inactivity required
        /// before users become candidates for tracking.
        /// </summary>
        internal const long MinimumInactivityBeforeTrackingMilliseconds = 500;

        /// <summary>
        /// Object used to keep track of user activity.
        /// </summary>
        private readonly UserActivityMeter activityMeter = new UserActivityMeter();

        /// <summary>
        /// Object used to synchronize modifications to engagement state.
        /// </summary>
        private readonly object lockObject = new object();

        /// <summary>
        /// Helper object used to keep track of previous set of tracked user ids while avoiding
        /// object allocation while processing each frame.
        /// </summary>
        private HashSet<int> previousTrackedUserTrackingIds = new HashSet<int>();

        /// <summary>
        /// Map between user tracking IDs and user states exposed to clients.
        /// </summary>
        private Dictionary<int, string> publicUserStates = new Dictionary<int, string>();

        /// <summary>
        /// Dictionary used to accumulate mappings between user tracking IDs and user states
        /// before they're ready to be exposed to clients.
        /// </summary>
        private Dictionary<int, string> userStatesAccumulator = new Dictionary<int, string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultUserStateManager"/> class.
        /// </summary>
        public DefaultUserStateManager()
        {
            this.TrackedUserTrackingIds = new HashSet<int>();
        }

        /// <summary>
        /// Event triggered whenever user state changes.
        /// </summary>
        public event EventHandler<UserStateChangedEventArgs> UserStateChanged;

        /// <summary>
        /// Dictionary mapping user tracking Ids to names used for states corresponding to
        /// those users.
        /// </summary>
        public IDictionary<int, string> UserStates
        {
            get
            {
                return this.publicUserStates;
            }
        }

        /// <summary>
        /// Tracking ID corresponding to primary user.
        /// </summary>
        /// <remarks>
        /// May be an invalid tracking id to represent that no user is currently primary.
        /// </remarks>
        public int PrimaryUserTrackingId { get; set; }

        /// <summary>
        /// Tracking ID corresponding to engaged user.
        /// </summary>
        /// <remarks>
        /// May be an invalid tracking id to represent that no user is currently engaged.
        /// </remarks>
        private int EngagedUserTrackingId { get; set; }

        /// <summary>
        /// Set of tracking Ids corresponding to users currently considered to be tracked.
        /// </summary>
        private HashSet<int> TrackedUserTrackingIds { get; set; }

        /// <summary>
        /// Resets all state to the initial state, with no users remembered as engaged or tracked.
        /// </summary>
        public void Reset()
        {
            using (var callbackLock = new CallbackLock(this.lockObject))
            {
                this.activityMeter.Clear();
                this.TrackedUserTrackingIds.Clear();
                this.EngagedUserTrackingId = SharedConstants.InvalidUserTrackingId;
                this.SetPrimaryUserTrackingId(SharedConstants.InvalidUserTrackingId, callbackLock);
                this.UpdateUserStates(callbackLock);
            }
        }

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
        public void ChooseTrackedUsers(Skeleton[] frameSkeletons, long timestamp, int[] chosenTrackingIds)
        {
            if (frameSkeletons == null)
            {
                throw new ArgumentNullException("frameSkeletons");
            }

            if (chosenTrackingIds == null)
            {
                throw new ArgumentNullException("chosenTrackingIds");
            }

            var availableSkeletons = new List<Skeleton>(
                from skeleton in frameSkeletons
                where
                    (skeleton.TrackingId != SharedConstants.InvalidUserTrackingId)
                    &&
                    ((skeleton.TrackingState == SkeletonTrackingState.Tracked)
                     || (skeleton.TrackingState == SkeletonTrackingState.PositionOnly))
                select skeleton);
            var trackingCandidateSkeletons = new List<Skeleton>();

            // Update user activity metrics
            this.activityMeter.Update(availableSkeletons, timestamp);

            foreach (var skeleton in availableSkeletons)
            {
                UserActivityRecord record;
                if (this.activityMeter.TryGetActivityRecord(skeleton.TrackingId, out record))
                {
                    // The tracked skeletons become candidate skeletons for tracking if we have an activity record for them.
                    trackingCandidateSkeletons.Add(skeleton);
                }
            }

            // sort the currently tracked skeletons according to our tracking choice criteria
            trackingCandidateSkeletons.Sort((left, right) => this.ComputeTrackingMetric(right).CompareTo(this.ComputeTrackingMetric(left)));

            for (int i = 0; i < chosenTrackingIds.Length; ++i)
            {
                chosenTrackingIds[i] = (i < trackingCandidateSkeletons.Count) ? trackingCandidateSkeletons[i].TrackingId : SharedConstants.InvalidUserTrackingId;
            }
        }

        /// <summary>
        /// Called whenever the set of tracked users has changed.
        /// </summary>
        /// <param name="trackedUserInfo">
        /// User information from which we'll update the set of tracked users and the primary user.
        /// </param>
        /// <param name="timestamp">
        /// Interaction frame timestamp corresponding to given user information.
        /// </param>
        public void UpdateUserInformation(IEnumerable<UserInfo> trackedUserInfo, long timestamp)
        {
            bool foundEngagedUser = false;
            int firstTrackedUser = SharedConstants.InvalidUserTrackingId;

            using (var callbackLock = new CallbackLock(this.lockObject))
            {
                this.previousTrackedUserTrackingIds.Clear();
                var nextTrackedIds = this.previousTrackedUserTrackingIds;
                this.previousTrackedUserTrackingIds = this.TrackedUserTrackingIds;
                this.TrackedUserTrackingIds = nextTrackedIds;

                var trackedUserInfoArray = trackedUserInfo as UserInfo[] ?? trackedUserInfo.ToArray();

                foreach (var userInfo in trackedUserInfoArray)
                {
                    if (userInfo.SkeletonTrackingId == SharedConstants.InvalidUserTrackingId)
                    {
                        continue;
                    }

                    if (this.EngagedUserTrackingId == userInfo.SkeletonTrackingId)
                    {
                        this.TrackedUserTrackingIds.Add(userInfo.SkeletonTrackingId);

                        foundEngagedUser = true;
                    }
                    else if (HasTrackedHands(userInfo)
                             && (this.previousTrackedUserTrackingIds.Contains(userInfo.SkeletonTrackingId)
                                 || this.IsInactive(userInfo, timestamp)))
                    {
                        // Keep track of the non-engaged users we find that have at least one
                        // tracked hand pointer and also either (1) were previously tracked or
                        // (2) are not moving too much
                        this.TrackedUserTrackingIds.Add(userInfo.SkeletonTrackingId);

                        if (firstTrackedUser == SharedConstants.InvalidUserTrackingId)
                        {
                            // Consider the first non-engaged, stationary user as a candidate for engagement
                            firstTrackedUser = userInfo.SkeletonTrackingId;
                        }
                    }
                }

                // If engaged user was not found in list of candidate users, engaged user has become invalid.
                if (!foundEngagedUser)
                {
                    this.EngagedUserTrackingId = SharedConstants.InvalidUserTrackingId;
                }

                // Decide who should be the primary user, if anyone
                this.UpdatePrimaryUser(trackedUserInfoArray, callbackLock);

                // If there's a primary user, it is the preferred candidate for engagement.
                // Otherwise, the first tracked user seen is the preferred candidate.
                int candidateUserTrackingId = (this.PrimaryUserTrackingId != SharedConstants.InvalidUserTrackingId)
                                                  ? this.PrimaryUserTrackingId
                                                  : firstTrackedUser;

                // If there is a valid candidate user that is not already the engaged user
                if ((candidateUserTrackingId != SharedConstants.InvalidUserTrackingId)
                    && (candidateUserTrackingId != this.EngagedUserTrackingId))
                {
                    // If there is currently no engaged user, or if candidate user is the
                    // primary user controlling interactions while the currently engaged user
                    // is not interacting
                    if ((this.EngagedUserTrackingId == SharedConstants.InvalidUserTrackingId)
                        || (candidateUserTrackingId == this.PrimaryUserTrackingId))
                    {
                        this.PromoteCandidateToEngaged(candidateUserTrackingId);
                    }
                }

                // Update user states as the very last action, to include results from updates
                // performed so far
                this.UpdateUserStates(callbackLock);
            }
        }

        /// <summary>
        /// Promote candidate user to be the engaged user.
        /// </summary>
        /// <param name="candidateTrackingId">
        /// Tracking Id of user to be promoted to engaged user.
        /// If tracking Id does not match the Id of one of the currently tracked users,
        /// no action is taken.
        /// </param>
        /// <returns>
        /// True if specified candidate could be confirmed as the new engaged user,
        /// false otherwise.
        /// </returns>
        public bool PromoteCandidateToEngaged(int candidateTrackingId)
        {
            bool isConfirmed = false;

            if ((candidateTrackingId != SharedConstants.InvalidUserTrackingId) && this.TrackedUserTrackingIds.Contains(candidateTrackingId))
            {
                using (var callbackLock = new CallbackLock(this.lockObject))
                {
                    this.EngagedUserTrackingId = candidateTrackingId;
                    this.UpdateUserStates(callbackLock);
                }

                isConfirmed = true;
            }

            return isConfirmed;
        }

        /// <summary>
        /// Tries to get the last position observed for the specified user tracking Id.
        /// </summary>
        /// <param name="trackingId">
        /// User tracking Id for which we're finding the last position observed.
        /// </param>
        /// <returns>
        /// Skeleton point, if last position is being tracked for specified
        /// tracking Id, null otherwise.
        /// </returns>
        public SkeletonPoint? TryGetLastPositionForId(int trackingId)
        {
            if (SharedConstants.InvalidUserTrackingId == trackingId)
            {
                return null;
            }

            UserActivityRecord record;
            if (this.activityMeter.TryGetActivityRecord(trackingId, out record))
            {
                return record.LastPosition;
            }

            return null;
        }

        /// <summary>
        /// Get a JSON friendly array of user-tracking-id-to-state mapping entries
        /// representing the specified user state map.
        /// </summary>
        /// <param name="userStates">
        /// Dictionary mapping user tracking ids to user state names.
        /// </param>
        /// <returns>
        /// Array of <see cref="StateMappingEntry"/> objects.
        /// </returns>
        internal static StateMappingEntry[] GetStateMappingEntryArray(IDictionary<int, string> userStates)
        {
            var mappingEntries = new StateMappingEntry[userStates.Count];
            int entryIndex = 0;
            foreach (var userStateEntry in userStates)
            {
                mappingEntries[entryIndex] = new StateMappingEntry { id = userStateEntry.Key, userState = userStateEntry.Value };
                ++entryIndex;
            }

            return mappingEntries;
        }

        internal void SetPrimaryUserTrackingId(int newId, CallbackLock callbackLock)
        {
            int oldId = this.PrimaryUserTrackingId;
            this.PrimaryUserTrackingId = newId;

            if (oldId != newId)
            {
                callbackLock.LockExit +=
                    () =>
                    this.SendUserStateChanged(
                        new UserTrackingIdChangedEventMessage
                        {
                            category = EventCategory,
                            eventType = PrimaryUserChangedEventType,
                            oldValue = oldId,
                            newValue = newId
                        });
            }
        }

        /// <summary>
        /// Determine if any of the specified user's hands is tracked.
        /// </summary>
        /// <param name="userInfo">
        /// User information from which to determine hand tracking status.
        /// </param>
        /// <returns>
        /// True if user has at least one tracked hand pointer. False otherwise.
        /// </returns>
        private static bool HasTrackedHands(UserInfo userInfo)
        {
            return userInfo.HandPointers.Any(handPointer => handPointer.IsTracked);
        }
        
        /// <summary>
        /// Update the primary user being tracked.
        /// </summary>
        /// <param name="candidateUserInfo">
        /// User information collection from which we will choose a primary user.
        /// </param>
        /// <param name="callbackLock">
        /// Lock used to delay all events until after we exit lock section.
        /// </param>
        private void UpdatePrimaryUser(IEnumerable<UserInfo> candidateUserInfo, CallbackLock callbackLock)
        {
            int firstPrimaryUserCandidate = SharedConstants.InvalidUserTrackingId;
            bool currentPrimaryUserStillPrimary = false;
            bool engagedUserIsPrimary = false;

            var trackingIdsAvailable = new HashSet<int>();

            foreach (var userInfo in candidateUserInfo)
            {
                if (userInfo.SkeletonTrackingId == SharedConstants.InvalidUserTrackingId)
                {
                    continue;
                }

                trackingIdsAvailable.Add(userInfo.SkeletonTrackingId);

                foreach (var handPointer in userInfo.HandPointers)
                {
                    if (handPointer.IsPrimaryForUser)
                    {
                        if (this.PrimaryUserTrackingId == userInfo.SkeletonTrackingId)
                        {
                            // If the current primary user still has an active hand, we should continue to consider them the primary user.
                            currentPrimaryUserStillPrimary = true;
                        }
                        else if (SharedConstants.InvalidUserTrackingId == firstPrimaryUserCandidate)
                        {
                            // Else if this is the first user with an active hand, they are the alternative candidate for primary user.
                            firstPrimaryUserCandidate = userInfo.SkeletonTrackingId;
                        }

                        if (this.EngagedUserTrackingId == userInfo.SkeletonTrackingId)
                        {
                            engagedUserIsPrimary = true;
                        }
                    }
                }
            }

            // If engaged user has a primary hand, always pick that user as primary user.
            // If current primary user still has a primary hand, let them remain primary.
            // Otherwise default to first primary user candidate seen.
            int primaryUserTrackingId = engagedUserIsPrimary
                                            ? this.EngagedUserTrackingId
                                            : (currentPrimaryUserStillPrimary ? this.PrimaryUserTrackingId : firstPrimaryUserCandidate);
            this.SetPrimaryUserTrackingId(primaryUserTrackingId, callbackLock);
        }

        /// <summary>
        /// Calculate how valuable it will be to keep tracking the specified skeleton.
        /// </summary>
        /// <param name="skeleton">
        /// Skeleton that is one of several candidates for tracking.
        /// </param>
        /// <returns>
        /// A non-negative metric that estimates how valuable it is to keep tracking
        /// the specified skeleton. The higher the value, the more valuable the skeleton
        /// is estimated to be.
        /// </returns>
        private double ComputeTrackingMetric(Skeleton skeleton)
        {
            const double MaxCameraDistance = 4.0;

            // Give preference to engaged users, then to tracked users, then to users
            // near the center of the Kinect Sensor's field of view that are also
            // closer (distance) to the KinectSensor and not moving around too much.
            const double EngagedWeight = 100.0;
            const double TrackedWeight = 50.0;
            const double AngleFromCenterWeight = 1.30;
            const double DistanceFromCameraWeight = 1.15;
            const double BodyMovementWeight = 0.05;

            double engagedMetric = (skeleton.TrackingId == this.EngagedUserTrackingId) ? 1.0 : 0.0;
            double trackedMetric = this.TrackedUserTrackingIds.Contains(skeleton.TrackingId) ? 1.0 : 0.0;
            double angleFromCenterMetric = (skeleton.Position.Z > 0.0) ? (1.0 - Math.Abs(2 * Math.Atan(skeleton.Position.X / skeleton.Position.Z) / Math.PI)) : 0.0;
            double distanceFromCameraMetric = (MaxCameraDistance - skeleton.Position.Z) / MaxCameraDistance;
            UserActivityRecord activityRecord;
            double bodyMovementMetric = this.activityMeter.TryGetActivityRecord(skeleton.TrackingId, out activityRecord)
                                            ? 1.0 - activityRecord.ActivityLevel
                                            : 0.0;
            return (EngagedWeight * engagedMetric) +
                (TrackedWeight * trackedMetric) +
                (AngleFromCenterWeight * angleFromCenterMetric) +
                (DistanceFromCameraWeight * distanceFromCameraMetric) +
                (BodyMovementWeight * bodyMovementMetric);
        }

        /// <summary>
        /// Determine if the specified user information represents a user that has been
        /// relatively inactive for at least a minimum period of time required for tracking.
        /// </summary>
        /// <param name="userInfo">
        /// User information from which to determine inactivity.
        /// </param>
        /// <param name="timestamp">
        /// Current timestamp used to determine how long user has been inactive.
        /// </param>
        /// <returns>
        /// True if user is present in scene and has been inactive for a minimum threshold
        /// period of time.
        /// </returns>
        private bool IsInactive(UserInfo userInfo, long timestamp)
        {
            UserActivityRecord record;
            return this.activityMeter.TryGetActivityRecord(userInfo.SkeletonTrackingId, out record) && !record.IsActive
                   && (record.StateTransitionTimestamp + MinimumInactivityBeforeTrackingMilliseconds <= timestamp);
        }

        /// <summary>
        /// Determines if user states have changed.
        /// </summary>
        /// <returns>
        /// true if accumulated user states are different from the ones currently visible
        /// to clients.
        /// </returns>
        private bool HaveUserStatesChanged()
        {
            if (this.publicUserStates.Count != this.userStatesAccumulator.Count)
            {
                return true;
            }

            foreach (var stateEntry in this.publicUserStates)
            {
                string accumulatorState;
                if (!this.userStatesAccumulator.TryGetValue(stateEntry.Key, out accumulatorState))
                {
                    // Key is absent from accumulator but present in current state map
                    return true;
                }

                if (!stateEntry.Value.Equals(accumulatorState))
                {
                    // state names are present in both maps, but they're different
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Update user states exposed to clients, if necessary.
        /// </summary>
        /// <param name="callbackLock">
        /// Lock used to delay all events until after we exit lock section.
        /// </param>
        private void UpdateUserStates(CallbackLock callbackLock)
        {
            this.userStatesAccumulator.Clear();

            // Add states for tracked users
            foreach (var trackingId in this.TrackedUserTrackingIds)
            {
                this.userStatesAccumulator.Add(trackingId, TrackedStateName);
            }

            if (this.EngagedUserTrackingId != SharedConstants.InvalidUserTrackingId)
            {
                // Engaged state supersedes all other states
                this.userStatesAccumulator[this.EngagedUserTrackingId] = EngagedStateName;
            }

            if (this.HaveUserStatesChanged())
            {
                var temporaryMap = this.publicUserStates;
                this.publicUserStates = this.userStatesAccumulator;
                this.userStatesAccumulator = temporaryMap;

                var userStatesToSend = GetStateMappingEntryArray(this.publicUserStates);

                callbackLock.LockExit +=
                    () =>
                    this.SendUserStateChanged(
                        new UserStatesChangedEventMessage
                        {
                            category = EventCategory,
                            eventType = UserStatesChangedEventType,
                            userStates = userStatesToSend
                        });
            }
        }

        /// <summary>
        /// Send UserStateChanged event if there are any subscribers.
        /// </summary>
        /// <param name="message">
        /// Message to send.
        /// </param>
        private void SendUserStateChanged(EventMessage message)
        {
            if (this.UserStateChanged != null)
            {
                this.UserStateChanged(this, new UserStateChangedEventArgs(message));
            }
        }
    }
}
