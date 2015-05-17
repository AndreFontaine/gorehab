// -----------------------------------------------------------------------
// <copyright file="InteractionStreamHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver.Sensor
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Media;

    using Microsoft.Kinect;
    using Microsoft.Kinect.Toolkit.Interaction;
    using Microsoft.Samples.Kinect.Webserver.Sensor.Serialization;

    /// <summary>
    /// Implementation of ISensorStreamHandler that exposes interaction and user viewer
    /// streams.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Disposable interaction stream is disposed when sensor is set to null")]
    public class InteractionStreamHandler : SensorStreamHandlerBase, IInteractionClient
    {
        /// <summary>
        /// JSON name of interaction stream.
        /// </summary>
        internal const string InteractionStreamName = "interaction";

        /// <summary>
        /// JSON name for property representing primary user tracking ID.
        /// </summary>
        internal const string InteractionPrimaryUserPropertyName = "primaryUser";

        /// <summary>
        /// JSON name for property representing user states.
        /// </summary>
        internal const string InteractionUserStatesPropertyName = "userStates";

        /// <summary>
        /// JSON name of user viewer stream.
        /// </summary>
        internal const string UserViewerStreamName = "userviewer";

        /// <summary>
        /// JSON name for property representing user viewer image resolution.
        /// </summary>
        internal const string UserViewerResolutionPropertyName = "resolution";

        /// <summary>
        /// Default width for user viewer image.
        /// </summary>
        internal const int UserViewerDefaultWidth = 128;

        /// <summary>
        /// Default height for user viewer image.
        /// </summary>
        internal const int UserViewerDefaultHeight = 96;

        /// <summary>
        /// JSON name for property representing default color for users in user viewer image.
        /// </summary>
        internal const string UserViewerDefaultUserColorPropertyName = "defaultUserColor";

        /// <summary>
        /// JSON name for property representing a map between user states and colors that should
        /// be used to represent those states in user viewer image.
        /// </summary>
        internal const string UserViewerUserColorsPropertyName = "userColors";

        /// <summary>
        /// Sub path for interaction client web-socket RPC endpoint owned by this handler.
        /// </summary>
        internal const string ClientUriSubpath = "CLIENT";

        /// <summary>
        /// Default value for default color for users in user viewer image (light gray).
        /// </summary>
        internal static readonly Color UserViewerDefaultDefaultUserColor = new Color { R = 0xd3, G = 0xd3, B = 0xd3, A = 0xff };

        /// <summary>
        /// Default color for tracked users in user viewer image (Kinect blue).
        /// </summary>
        internal static readonly Color UserViewerDefaultTrackedUserColor = new Color { R = 0x00, G = 0xbc, B = 0xf2, A = 0xff };

        /// <summary>
        /// Default color for engaged users in user viewer image (Kinect purple).
        /// </summary>
        internal static readonly Color UserViewerDefaultEngagedUserColor = new Color { R = 0x51, G = 0x1c, B = 0x74, A = 0xff };

        /// <summary>
        /// Regular expression that matches the user viewer resolution property.
        /// </summary>
        private static readonly Regex UserViewerResolutionRegex = new Regex(@"^(?i)(\d+)x(\d+)$");

        private static readonly Size[] UserViewerSupportedResolutions =
        {
            new Size(640, 480), new Size(320, 240), new Size(160, 120),
            new Size(128, 96), new Size(80, 60)
        };

        /// <summary>
        /// Context that allows this stream handler to communicate with its owner.
        /// </summary>
        private readonly SensorStreamHandlerContext ownerContext;

        /// <summary>
        /// Serializable interaction stream message, reused as interaction frames arrive.
        /// </summary>
        private readonly InteractionStreamMessage interactionStreamMessage = new InteractionStreamMessage { stream = InteractionStreamName };

        /// <summary>
        /// Serializable user viewer stream message header, reused as depth frames arrive
        /// and are colorized into the user viewer image.
        /// </summary>
        private readonly ImageHeaderStreamMessage userViewerStreamMessage = new ImageHeaderStreamMessage { stream = UserViewerStreamName };

        /// <summary>
        /// Ids of users we choose to track.
        /// </summary>
        private readonly int[] recommendedUserTrackingIds = new int[2];

        /// <summary>
        /// A map between user state names and colors that should be used to represent those
        /// states in user viewer image.
        /// </summary>
        private readonly Dictionary<string, int> userViewerUserColors = new Dictionary<string, int>();

        /// <summary>
        /// Width of user viewer image.
        /// </summary>
        private readonly UserViewerColorizer userViewerColorizer = new UserViewerColorizer(UserViewerDefaultWidth, UserViewerDefaultHeight);

        /// <summary>
        /// User state manager.
        /// </summary>
        private readonly IUserStateManager userStateManager = new DefaultUserStateManager();

        /// <summary>
        /// Sensor providing data to interaction stream.
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Entry point for interaction stream functionality.
        /// </summary>
        private InteractionStream interactionStream;

        /// <summary>
        /// Intermediate storage for the user information received from interaction stream.
        /// </summary>
        private UserInfo[] userInfos;

        /// <summary>
        /// true if interaction stream is enabled.
        /// </summary>
        private bool interactionIsEnabled;

        /// <summary>
        /// true if user viewer stream is enabled.
        /// </summary>
        private bool userViewerIsEnabled;

        /// <summary>
        /// Default color for users in user viewer image, in 32-bit RGBA format.
        /// </summary>
        private int userViewerDefaultUserColor;

        /// <summary>
        /// Keep track if we're in the middle of processing an interaction frame.
        /// </summary>
        private bool isProcessingInteractionFrame;

        /// <summary>
        /// Keep track if we're in the middle of processing a user viewer image.
        /// </summary>
        private bool isProcessingUserViewerImage;

        /// <summary>
        /// Channel used to perform remote procedure calls regarding IInteractionClient state.
        /// </summary>
        private WebSocketRpcChannel clientRpcChannel;

        /// <summary>
        /// Initializes a new instance of the <see cref="InteractionStreamHandler"/> class
        /// and associates it with a context that allows it to communicate with its owner.
        /// </summary>
        /// <param name="ownerContext">
        /// An instance of <see cref="SensorStreamHandlerContext"/> class.
        /// </param>
        internal InteractionStreamHandler(SensorStreamHandlerContext ownerContext)
        {
            this.userViewerDefaultUserColor = GetRgbaColorInt(UserViewerDefaultDefaultUserColor);
            this.userViewerUserColors[DefaultUserStateManager.TrackedStateName] = GetRgbaColorInt(UserViewerDefaultTrackedUserColor);
            this.userViewerUserColors[DefaultUserStateManager.EngagedStateName] = GetRgbaColorInt(UserViewerDefaultEngagedUserColor);
            
            this.ownerContext = ownerContext;
            this.userStateManager.UserStateChanged += this.OnUserStateChanged;

            this.AddStreamConfiguration(InteractionStreamName, new StreamConfiguration(this.GetInteractionStreamProperties, this.SetInteractionStreamProperty));
            this.AddStreamConfiguration(UserViewerStreamName, new StreamConfiguration(this.GetUserViewerStreamProperties, this.SetUserViewerStreamProperty));
        }

        /// <summary>
        /// True if we should process interaction data fed into interaction stream and user state manager.
        /// False otherwise.
        /// </summary>
        private bool ShouldProcessInteractionData
        {
            get
            {
                // Check for a null userInfos since we may still get posted events from
                // the stream after we have unregistered our event handler and deleted
                // our buffers.
                // Also we process interaction data even if only user viewer stream is
                // enabled since data is used by IUserStateManager and UserViewerColorizer
                // as well as by clients listening directly to interaction stream.
                return (this.userInfos != null) && (this.interactionIsEnabled || this.userViewerIsEnabled);
            }
        }

        /// <summary>
        /// Lets ISensorStreamHandler know that Kinect Sensor associated with this stream
        /// handler has changed.
        /// </summary>
        /// <param name="newSensor">
        /// New KinectSensor.
        /// </param>
        public override void OnSensorChanged(KinectSensor newSensor)
        {
            if (this.sensor != null)
            {
                try
                {
                    this.interactionStream.InteractionFrameReady -= this.InteractionFrameReadyAsync;
                    this.interactionStream.Dispose();
                    this.interactionStream = null;

                    this.sensor.SkeletonStream.AppChoosesSkeletons = false;
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }

                this.userInfos = null;
            }

            this.sensor = newSensor;

            if (newSensor != null)
            {
                try
                {
                    this.interactionStream = new InteractionStream(newSensor, this);
                    this.interactionStream.InteractionFrameReady += this.InteractionFrameReadyAsync;

                    this.sensor.SkeletonStream.AppChoosesSkeletons = true;

                    this.userInfos = new UserInfo[InteractionFrame.UserInfoArrayLength];
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }

            this.userStateManager.Reset();
            this.userViewerColorizer.ResetColorLookupTable();
        }

        /// <summary>
        /// Process data from one Kinect depth frame.
        /// </summary>
        /// <param name="depthData">
        /// Kinect depth data.
        /// </param>
        /// <param name="depthFrame">
        /// <see cref="DepthImageFrame"/> from which we obtained depth data.
        /// </param>
        public override void ProcessDepth(DepthImagePixel[] depthData, DepthImageFrame depthFrame)
        {
            if (depthData == null)
            {
                throw new ArgumentNullException("depthData");
            }

            if (depthFrame == null)
            {
                throw new ArgumentNullException("depthFrame");
            }

            if (this.ShouldProcessInteractionData)
            {
                this.interactionStream.ProcessDepth(depthData, depthFrame.Timestamp);
            }

            this.ProcessUserViewerImageAsync(depthData, depthFrame);
        }

        /// <summary>
        /// Process data from one Kinect skeleton frame.
        /// </summary>
        /// <param name="skeletons">
        /// Kinect skeleton data.
        /// </param>
        /// <param name="skeletonFrame">
        /// <see cref="SkeletonFrame"/> from which we obtained skeleton data.
        /// </param>
        public override void ProcessSkeleton(Skeleton[] skeletons, SkeletonFrame skeletonFrame)
        {
            if (skeletons == null)
            {
                throw new ArgumentNullException("skeletons");
            }

            if (skeletonFrame == null)
            {
                throw new ArgumentNullException("skeletonFrame");
            }

            if (this.ShouldProcessInteractionData)
            {
                this.interactionStream.ProcessSkeleton(skeletons, this.sensor.AccelerometerGetCurrentReading(), skeletonFrame.Timestamp);
            }

            this.userStateManager.ChooseTrackedUsers(skeletons, skeletonFrame.Timestamp, this.recommendedUserTrackingIds);

            try
            {
                this.sensor.SkeletonStream.ChooseSkeletons(
                    this.recommendedUserTrackingIds[0], this.recommendedUserTrackingIds[1]);
            }
            catch (InvalidOperationException)
            {
                // KinectSensor might enter an invalid state while choosing skeletons.
                // E.g.: sensor might be abruptly unplugged.
            }
        }

        /// <summary>
        /// Handle an http request.
        /// </summary>
        /// <param name="streamName">
        /// Name of stream for which property values should be set.
        /// </param>
        /// <param name="requestContext">
        /// Context containing HTTP request data, which will also contain associated
        /// response upon return.
        /// </param>
        /// <param name="subpath">
        /// Request URI path relative to the stream name associated with this sensor stream
        /// handler in the stream handler owner.
        /// </param>
        /// <returns>
        /// Await-able task.
        /// </returns>
        /// <remarks>
        /// Return value should never be null. Implementations should use Task.FromResult(0)
        /// if function is implemented synchronously so that callers can await without
        /// needing to check for null.
        /// </remarks>
        public override Task HandleRequestAsync(string streamName, HttpListenerContext requestContext, string subpath)
        {
            if (streamName == null)
            {
                throw new ArgumentNullException("streamName");
            }

            if (requestContext == null)
            {
                throw new ArgumentNullException("requestContext");
            }

            if (subpath == null)
            {
                throw new ArgumentNullException("subpath");
            }

            if (!InteractionStreamName.Equals(streamName))
            {
                // Only supported endpoints are related to interaction stream
                KinectRequestHandler.CloseResponse(requestContext, HttpStatusCode.NotFound);
                return SharedConstants.EmptyCompletedTask;
            }

            var splitPath = KinectRequestHandler.SplitUriSubpath(subpath);

            if (splitPath == null)
            {
                KinectRequestHandler.CloseResponse(requestContext, HttpStatusCode.NotFound);
                return SharedConstants.EmptyCompletedTask;
            }

            var pathComponent = splitPath.Item1;
            switch (pathComponent)
            {
            case ClientUriSubpath:
                // Only support one client at any one time
                if (this.clientRpcChannel != null)
                {
                    if (this.clientRpcChannel.CheckConnectionStatus())
                    {
                        KinectRequestHandler.CloseResponse(requestContext, HttpStatusCode.Conflict);
                        return SharedConstants.EmptyCompletedTask;
                    }

                    this.clientRpcChannel = null;
                }

                WebSocketRpcChannel.TryOpenAsync(
                    requestContext,
                    channel =>
                        {
                            // Check again in case another request came in before connection was established
                            if (this.clientRpcChannel != null)
                            {
                                channel.Dispose();
                            }

                            this.clientRpcChannel = channel;
                        },
                    channel =>
                        {
                            // Only forget the current channel if it matches the channel being closed
                            if (this.clientRpcChannel == channel)
                            {
                                this.clientRpcChannel = null;
                            }
                        });

                break;

            default:
                KinectRequestHandler.CloseResponse(requestContext, HttpStatusCode.NotFound);
                break;
            }

            return SharedConstants.EmptyCompletedTask;
        }

        /// <summary>
        /// Cancel all pending operations.
        /// </summary>
        public override void Cancel()
        {
            if (this.clientRpcChannel != null)
            {
                this.clientRpcChannel.Cancel();
            }
        }

        /// <summary>
        /// Lets handler know that it should clean up resources associated with sensor stream
        /// handling.
        /// </summary>
        /// <returns>
        /// Await-able task.
        /// </returns>
        /// <remarks>
        /// Return value should never be null. Implementations should use Task.FromResult(0)
        /// if function is implemented synchronously so that callers can await without
        /// needing to check for null.
        /// </remarks>
        public override async Task UninitializeAsync()
        {
            if (this.clientRpcChannel != null)
            {
                await this.clientRpcChannel.CloseAsync();
            }
        }

        /// <summary>
        /// Gets interaction information available for a specified location in UI.
        /// </summary>
        /// <param name="skeletonTrackingId">
        /// The skeleton tracking ID for which interaction information is being retrieved.
        /// </param>
        /// <param name="handType">
        /// The hand type for which interaction information is being retrieved.
        /// </param>
        /// <param name="x">
        /// X-coordinate of UI location for which interaction information is being retrieved.
        /// 0.0 corresponds to left edge of interaction region and 1.0 corresponds to right edge
        /// of interaction region.
        /// </param>
        /// <param name="y">
        /// Y-coordinate of UI location for which interaction information is being retrieved.
        /// 0.0 corresponds to top edge of interaction region and 1.0 corresponds to bottom edge
        /// of interaction region.
        /// </param>
        /// <returns>
        /// An <see cref="InteractionInfo"/> object instance.
        /// </returns>
        public InteractionInfo GetInteractionInfoAtLocation(int skeletonTrackingId, InteractionHandType handType, double x, double y)
        {
            var interactionInfo = new InteractionInfo { IsPressTarget = false, IsGripTarget = false };

            if (this.interactionIsEnabled && (this.clientRpcChannel != null))
            {
                var result = this.clientRpcChannel.CallFunction<InteractionStreamHitTestInfo>("getInteractionInfoAtLocation", skeletonTrackingId, handType.ToString(), x, y);
                if (result.Success)
                {
                    interactionInfo.IsGripTarget = result.Result.isGripTarget;
                    interactionInfo.IsPressTarget = result.Result.isPressTarget;
                    var elementId = result.Result.pressTargetControlId;
                    interactionInfo.PressTargetControlId = (elementId != null) ? elementId.GetHashCode() : 0;
                    interactionInfo.PressAttractionPointX = result.Result.pressAttractionPointX;
                    interactionInfo.PressAttractionPointY = result.Result.pressAttractionPointY;
                }
            }

            return interactionInfo;
        }

        /// <summary>
        /// Converts a color into the corresponding 32-bit integer RGBA representation.
        /// </summary>
        /// <param name="color">
        /// Color to convert
        /// </param>
        /// <returns>
        /// 32-bit integer RGBA representation of color.
        /// </returns>
        internal static int GetRgbaColorInt(Color color)
        {
            return (color.A << 24) | (color.B << 16) | (color.G << 8) | color.R;
        }

        /// <summary>
        /// Event handler for InteractionStream's InteractionFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        internal async void InteractionFrameReadyAsync(object sender, InteractionFrameReadyEventArgs e)
        {
            if (!this.ShouldProcessInteractionData)
            {
                return;
            }

            if (this.isProcessingInteractionFrame)
            {
                // Re-entered InteractionFrameReadyAsync while a previous frame is already being processed.
                // Just ignore new frames until the current one finishes processing.
                return;
            }

            this.isProcessingInteractionFrame = true;

            try
            {
                bool haveFrameData = false;

                using (var interactionFrame = e.OpenInteractionFrame())
                {
                    // Even though we checked value of userInfos above as part of
                    // ShouldProcessInteractionData check, callbacks happening while
                    // opening an interaction frame might have invalidated it, so we
                    // check value again. 
                    if ((interactionFrame != null) && (this.userInfos != null))
                    {
                        // Copy interaction frame data so we can dispose interaction frame
                        // right away, even if data processing/event handling takes a while.
                        interactionFrame.CopyInteractionDataTo(this.userInfos);
                        this.interactionStreamMessage.timestamp = interactionFrame.Timestamp;
                        haveFrameData = true;
                    }
                }

                if (haveFrameData)
                {
                    this.userStateManager.UpdateUserInformation(this.userInfos, this.interactionStreamMessage.timestamp);
                    this.userViewerColorizer.UpdateColorLookupTable(this.userInfos, this.userViewerDefaultUserColor, this.userStateManager.UserStates, this.userViewerUserColors);

                    if (this.interactionIsEnabled)
                    {
                        this.interactionStreamMessage.UpdateHandPointers(this.userInfos, this.userStateManager.PrimaryUserTrackingId);
                        await this.ownerContext.SendStreamMessageAsync(this.interactionStreamMessage);
                    }
                }
            }
            finally
            {
                this.isProcessingInteractionFrame = false;
            }
        }

        /// <summary>
        /// Gets all interaction stream property value.
        /// </summary>
        /// <param name="propertyMap">
        /// Property name->value map where property values should be set.
        /// </param>
        internal void GetInteractionStreamProperties(Dictionary<string, object> propertyMap)
        {
            propertyMap.Add(KinectRequestHandler.EnabledPropertyName, this.interactionIsEnabled);
            propertyMap.Add(InteractionPrimaryUserPropertyName, this.userStateManager.PrimaryUserTrackingId);
            propertyMap.Add(InteractionUserStatesPropertyName, DefaultUserStateManager.GetStateMappingEntryArray(this.userStateManager.UserStates));
        }

        /// <summary>
        /// Set an interaction stream property value.
        /// </summary>
        /// <param name="propertyName">
        /// Name of property to set.
        /// </param>
        /// <param name="propertyValue">
        /// Property value to set.
        /// </param>
        /// <returns>
        /// null if property setting was successful, error message otherwise.
        /// </returns>
        internal string SetInteractionStreamProperty(string propertyName, object propertyValue)
        {
            bool recognized = true;

            if (propertyValue == null)
            {
                // None of the interaction stream properties accept a null value
                return Properties.Resources.PropertyValueInvalidFormat;
            }

            try
            {
                switch (propertyName)
                {
                case KinectRequestHandler.EnabledPropertyName:
                    this.interactionIsEnabled = (bool)propertyValue;
                    break;

                default:
                    recognized = false;
                    break;
                }

                if (!recognized)
                {
                    return Properties.Resources.PropertyNameUnrecognized;
                }
            }
            catch (InvalidCastException)
            {
                return Properties.Resources.PropertyValueInvalidFormat;
            }

            return null;
        }

        /// <summary>
        /// Gets all user viewer stream property value.
        /// </summary>
        /// <param name="propertyMap">
        /// Property name->value map where property values should be set.
        /// </param>
        internal void GetUserViewerStreamProperties(Dictionary<string, object> propertyMap)
        {
            propertyMap.Add(KinectRequestHandler.EnabledPropertyName, this.userViewerIsEnabled);
            propertyMap.Add(UserViewerResolutionPropertyName, string.Format(CultureInfo.InvariantCulture, @"{0}x{1}", this.userViewerColorizer.Width, this.userViewerColorizer.Height));
            propertyMap.Add(UserViewerDefaultUserColorPropertyName, this.userViewerDefaultUserColor);
            propertyMap.Add(UserViewerUserColorsPropertyName, this.userViewerUserColors);
        }

        /// <summary>
        /// Set a user viewer stream property.
        /// </summary>
        /// <param name="propertyName">
        /// Name of property to set.
        /// </param>
        /// <param name="propertyValue">
        /// Property value to set.
        /// </param>
        /// <returns>
        /// null if property setting was successful, error message otherwise.
        /// </returns>
        internal string SetUserViewerStreamProperty(string propertyName, object propertyValue)
        {
            bool recognized = true;

            try
            {
                switch (propertyName)
                {
                case KinectRequestHandler.EnabledPropertyName:
                    this.userViewerIsEnabled = (bool)propertyValue;
                    break;

                case UserViewerResolutionPropertyName:
                    if (propertyValue == null)
                    {
                        return Properties.Resources.PropertyValueInvalidFormat;
                    }

                    var match = UserViewerResolutionRegex.Match((string)propertyValue);
                    if (!match.Success || (match.Groups.Count != 3))
                    {
                        return Properties.Resources.PropertyValueInvalidFormat;
                    }

                    int width = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                    int height = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);

                    if (!IsSupportedUserViewerResolution(width, height))
                    {
                        return Properties.Resources.PropertyValueUnsupportedResolution;
                    }

                    this.userViewerColorizer.SetResolution(width, height);
                    break;

                case UserViewerDefaultUserColorPropertyName:
                    this.userViewerDefaultUserColor = (int)propertyValue;

                    this.UpdateColorizerLookupTable();
                    break;

                case UserViewerUserColorsPropertyName:
                    if (propertyValue == null)
                    {
                        // Null values just clear the set of user colors
                        this.userViewerUserColors.Clear();
                        break;
                    }

                    var userColors = (Dictionary<string, object>)propertyValue;

                    // Verify that all dictionary values are integers
                    bool allIntegers = userColors.Values.Select(color => color as int?).All(colorInt => colorInt != null);
                    if (!allIntegers)
                    {
                        return Properties.Resources.PropertyValueInvalidFormat;
                    }

                    // If property value specified is compatible, copy values over
                    this.userViewerUserColors.Clear();
                    foreach (var entry in userColors)
                    {
                        this.userViewerUserColors.Add(entry.Key, (int)entry.Value);
                    }

                    this.UpdateColorizerLookupTable();
                    break;

                default:
                    recognized = false;
                    break;
                }

                if (!recognized)
                {
                    return Properties.Resources.PropertyNameUnrecognized;
                }
            }
            catch (InvalidCastException)
            {
                return Properties.Resources.PropertyValueInvalidFormat;
            }
            catch (NullReferenceException)
            {
                return Properties.Resources.PropertyValueInvalidFormat;
            }

            return null;
        }

        /// <summary>
        /// Determine if the specified resolution is supported for user viewer images.
        /// </summary>
        /// <param name="width">
        /// Image width.
        /// </param>
        /// <param name="height">
        /// Image height.
        /// </param>
        /// <returns>
        /// True if specified resolution is supported, false otherwise.
        /// </returns>
        private static bool IsSupportedUserViewerResolution(int width, int height)
        {
            return UserViewerSupportedResolutions.Any(resolution => ((int)resolution.Width == width) && ((int)resolution.Height == height));
        }

        /// <summary>
        /// Handler for IUserStateManager.UserStateChanged event.
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private async void OnUserStateChanged(object sender, UserStateChangedEventArgs e)
        {
            if (this.interactionIsEnabled)
            {
                // If enabled, forward all user state events to client
                await this.ownerContext.SendEventMessageAsync(e.Message);
            }
        }

        /// <summary>
        /// Update colorizer lookup table from color-related configuration.
        /// </summary>
        private void UpdateColorizerLookupTable()
        {
            if (this.ShouldProcessInteractionData)
            {
                this.userViewerColorizer.UpdateColorLookupTable(
                    this.userInfos, this.userViewerDefaultUserColor, this.userStateManager.UserStates, this.userViewerUserColors);
            }
        }

        /// <summary>
        /// Process depth data to obtain user viewer image.
        /// </summary>
        /// <param name="depthData">
        /// Kinect depth data.
        /// </param>
        /// <param name="depthFrame">
        /// <see cref="DepthImageFrame"/> from which we obtained depth data.
        /// </param>
        private async void ProcessUserViewerImageAsync(DepthImagePixel[] depthData, DepthImageFrame depthFrame)
        {
            if (this.userViewerIsEnabled)
            {
                if (this.isProcessingUserViewerImage)
                {
                    // Re-entered ProcessUserViewerImageAsync while a previous image is already being processed.
                    // Just ignore new depth frames until the current one finishes processing.
                    return;
                }

                this.isProcessingUserViewerImage = true;

                try
                {
                    this.userViewerColorizer.ColorizeDepthPixels(depthData, depthFrame.Width, depthFrame.Height);
                    this.userViewerStreamMessage.timestamp = depthFrame.Timestamp;
                    this.userViewerStreamMessage.width = this.userViewerColorizer.Width;
                    this.userViewerStreamMessage.height = this.userViewerColorizer.Height;
                    this.userViewerStreamMessage.bufferLength = this.userViewerColorizer.Buffer.Length;

                    await this.ownerContext.SendTwoPartStreamMessageAsync(this.userViewerStreamMessage, this.userViewerColorizer.Buffer);
                }
                finally
                {
                    this.isProcessingUserViewerImage = false;
                }
            }
        }
    }
}
