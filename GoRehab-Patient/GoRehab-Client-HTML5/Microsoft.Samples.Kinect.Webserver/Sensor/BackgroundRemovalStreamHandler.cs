// -----------------------------------------------------------------------
// <copyright file="BackgroundRemovalStreamHandler.cs" company="Microsoft">
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
    using System.Text.RegularExpressions;
    using System.Windows;

    using Microsoft.Kinect;
    using Microsoft.Kinect.Toolkit.BackgroundRemoval;
    using Microsoft.Samples.Kinect.Webserver.Properties;
    using Microsoft.Samples.Kinect.Webserver.Sensor.Serialization;

    /// <summary>
    /// Implementation of ISensorStreamHandler that exposes background removal streams.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Disposable background removal stream is disposed when sensor is set to null")]
    public class BackgroundRemovalStreamHandler : SensorStreamHandlerBase
    {
        /// <summary>
        /// JSON name of background removal stream.
        /// </summary>
        internal const string BackgroundRemovalStreamName = "backgroundRemoval";

        /// <summary>
        /// JSON name for property representing the tracking user id.
        /// </summary>
        internal const string TrackingIdPropertyName = "trackingId";

        /// <summary>
        /// JSON name for property representing the background removed color image resolution.
        /// </summary>
        internal const string ResolutionPropertyName = "resolution";

        /// <summary>
        /// Regular expression that matches the background removed frame resolution property.
        /// </summary>
        private static readonly Regex BackgroundRemovalResolutionRegex = new Regex(@"^(?i)(\d+)x(\d+)$");

        private static readonly KeyValuePair<ColorImageFormat, Size>[] BackgroundRemovalResolutions =
        {
            new KeyValuePair<ColorImageFormat, Size>(ColorImageFormat.RgbResolution640x480Fps30, new Size(640, 480)),
            new KeyValuePair<ColorImageFormat, Size>(ColorImageFormat.RgbResolution1280x960Fps12, new Size(1280, 960))
        };

        /// <summary>
        /// Context that allows this stream handler to communicate with its owner.
        /// </summary>
        private readonly SensorStreamHandlerContext ownerContext;

        /// <summary>
        /// Serializable background removal stream message, reused as background removed color frames arrive.
        /// </summary>
        private readonly BackgroundRemovalStreamMessage backgroundRemovalStreamMessage = new BackgroundRemovalStreamMessage { stream = BackgroundRemovalStreamName };

        /// <summary>
        /// Sensor providing data to background removal stream.
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Entry point for background removal stream functionality.
        /// </summary>
        private BackgroundRemovedColorStream backgroundRemovalStream;

        /// <summary>
        /// Id of the user we choose to track.
        /// </summary>
        private int trackingId;

        /// <summary>
        /// True if background removal stream is enabled. Background removal stream is disabled by default.
        /// </summary>
        private bool backgroundRemovalStreamIsEnabled;

        /// <summary>
        /// The background removed color image format.
        /// </summary>
        private ColorImageFormat colorImageFormat = ColorImageFormat.RgbResolution640x480Fps30;

        /// <summary>
        /// Keep track if we're in the middle of processing a background removed color frame.
        /// </summary>
        private bool isProcessingBackgroundRemovedFrame;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundRemovalStreamHandler"/> class
        /// and associates it with a context that allows it to communicate with its owner.
        /// </summary>
        /// <param name="ownerContext">
        /// An instance of <see cref="SensorStreamHandlerContext"/> class.
        /// </param>
        internal BackgroundRemovalStreamHandler(SensorStreamHandlerContext ownerContext)
        {
            this.ownerContext = ownerContext;

            this.AddStreamConfiguration(BackgroundRemovalStreamName, new StreamConfiguration(this.GetProperties, this.SetProperty));
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
                    this.backgroundRemovalStream.BackgroundRemovedFrameReady -= this.BackgroundRemovedFrameReadyAsync;
                    this.backgroundRemovalStream.Dispose();
                    this.backgroundRemovalStream = null;

                    this.sensor.ColorStream.Disable();
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }

            this.sensor = newSensor;

            if (newSensor != null)
            {
                this.backgroundRemovalStream = new BackgroundRemovedColorStream(newSensor);
                this.backgroundRemovalStream.BackgroundRemovedFrameReady += this.BackgroundRemovedFrameReadyAsync;

                // Force enabling the background removal stream because it hasn't been enabled before.
                this.UpdateBackgroundRemovalFrameFormat(this.colorImageFormat, true);
            }
        }

        /// <summary>
        /// Process data from one Kinect color frame.
        /// </summary>
        /// <param name="colorData">
        /// Kinect color data.
        /// </param>
        /// <param name="colorFrame">
        /// <see cref="ColorImageFrame"/> from which we obtained color data.
        /// </param>
        public override void ProcessColor(byte[] colorData, ColorImageFrame colorFrame)
        {
            if (colorData == null)
            {
                throw new ArgumentNullException("colorData");
            }

            if (colorFrame == null)
            {
                throw new ArgumentNullException("colorFrame");
            }

            if (this.backgroundRemovalStreamIsEnabled)
            {
                this.backgroundRemovalStream.ProcessColor(colorData, colorFrame.Timestamp);
            }
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

            if (this.backgroundRemovalStreamIsEnabled)
            {
                this.backgroundRemovalStream.ProcessDepth(depthData, depthFrame.Timestamp);
            }
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

            if (this.backgroundRemovalStreamIsEnabled)
            {
                this.backgroundRemovalStream.ProcessSkeleton(skeletons, skeletonFrame.Timestamp);
            }
        }

        /// <summary>
        /// Event handler for BackgroundRemovedColorStream's BackgroundRemovedFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        internal async void BackgroundRemovedFrameReadyAsync(object sender,  BackgroundRemovedColorFrameReadyEventArgs e)
        {
            if (!this.backgroundRemovalStreamIsEnabled)
            {
                // Directly return if the stream is not enabled.
                return;
            }

            if (this.isProcessingBackgroundRemovedFrame)
            {
                // Re-entered BackgroundRemovedFrameReadyAsync while a previous frame is already being processed.
                // Just ignore new frames until the current one finishes processing.
                return;
            }

            this.isProcessingBackgroundRemovedFrame = true;

            try
            {
                bool haveFrameData = false;

                using (var backgroundRemovedFrame = e.OpenBackgroundRemovedColorFrame())
                {
                    if (backgroundRemovedFrame != null)
                    {
                        this.backgroundRemovalStreamMessage.UpdateBackgroundRemovedColorFrame(backgroundRemovedFrame);

                        haveFrameData = true;
                    }
                }

                if (haveFrameData)
                {
                    await this.ownerContext.SendTwoPartStreamMessageAsync(this.backgroundRemovalStreamMessage, this.backgroundRemovalStreamMessage.Buffer);
                }
            }
            finally
            {
                this.isProcessingBackgroundRemovedFrame = false;
            }
        }

        /// <summary>
        /// Get the size for the given color image format.
        /// </summary>
        /// <param name="format">The color image format.</param>
        /// <returns>The width and height of the given image.</returns>
        private static Size GetColorImageSize(ColorImageFormat format)
        {
            try
            {
                var q = from item in BackgroundRemovalResolutions
                    where item.Key == format
                    select item.Value;

                return q.Single();
            }
            catch (InvalidOperationException)
            {
                throw new ArgumentException(Resources.UnsupportedColorFormat, "format");
            }
        }

        /// <summary>
        /// Get the color image format for the specified width and height.
        /// </summary>
        /// <param name="width">
        /// Image width.
        /// </param>
        /// <param name="height">
        /// Image height.
        /// </param>
        /// <returns>
        /// The color image format enumeration value.
        /// </returns>
        private static ColorImageFormat GetColorImageFormat(int width, int height)
        {
            try
            {
                var q = from item in BackgroundRemovalResolutions
                        where (int)item.Value.Width == width && (int)item.Value.Height == height
                        select item.Key;

                return q.Single();
            }
            catch (InvalidOperationException)
            {
                throw new ArgumentException(Resources.UnsupportedColorFormat);
            }
        }

        /// <summary>
        /// Set the background removed color frame format.
        /// </summary>
        /// <param name="format">
        /// The given color image format.
        /// </param>
        /// <param name="forceEnable">
        /// Streams should be enabled even if new color image format is the same as the old one.
        /// This is useful for the initial enabling of the stream.
        /// </param>
        private void UpdateBackgroundRemovalFrameFormat(ColorImageFormat format, bool forceEnable)
        {
            if (!forceEnable && (format == this.colorImageFormat))
            {
                // No work to do
                return;
            }

            if (this.sensor != null)
            {
                try
                {
                    this.sensor.ColorStream.Enable(format);
                    this.backgroundRemovalStream.Enable(format, DepthImageFormat.Resolution640x480Fps30);
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }

            // Update the image format property if the action succeeded.
            this.colorImageFormat = format;
        }

        /// <summary>
        /// Gets a background removal stream property value.
        /// </summary>
        /// <param name="propertyMap">
        /// Property name->value map where property values should be set.
        /// </param>
        private void GetProperties(Dictionary<string, object> propertyMap)
        {
            propertyMap.Add(KinectRequestHandler.EnabledPropertyName, this.backgroundRemovalStreamIsEnabled);
            propertyMap.Add(TrackingIdPropertyName, this.trackingId);

            var size = GetColorImageSize(this.colorImageFormat);
            propertyMap.Add(ResolutionPropertyName, string.Format(CultureInfo.InvariantCulture, @"{0}x{1}", (int)size.Width, (int)size.Height));
        }

        /// <summary>
        /// Set a background removal stream property value.
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
        private string SetProperty(string propertyName, object propertyValue)
        {
            bool recognized = true;

            if (propertyValue == null)
            {
                return Resources.PropertyValueInvalidFormat;
            }

            try
            {
                switch (propertyName)
                {
                    case KinectRequestHandler.EnabledPropertyName:
                        this.backgroundRemovalStreamIsEnabled = (bool)propertyValue;
                        break;

                    case TrackingIdPropertyName:
                        {
                            var oldTrackingId = this.trackingId;
                            this.trackingId = (int)propertyValue;

                            if (this.trackingId != oldTrackingId)
                            {
                                this.backgroundRemovalStream.SetTrackedPlayer(this.trackingId);
                            }
                        }

                        break;

                    case ResolutionPropertyName:
                        var match = BackgroundRemovalResolutionRegex.Match((string)propertyValue);
                        if (!match.Success || (match.Groups.Count != 3))
                        {
                            return Resources.PropertyValueInvalidFormat;
                        }

                        int width = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                        int height = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);

                        try
                        {
                            var format = GetColorImageFormat(width, height);
                            this.UpdateBackgroundRemovalFrameFormat(format, false);
                        }
                        catch (ArgumentException)
                        {
                            return Resources.PropertyValueUnsupportedResolution;
                        }

                        break;

                    default:
                        recognized = false;
                        break;
                }

                if (!recognized)
                {
                    return Resources.PropertyNameUnrecognized;
                }
            }
            catch (InvalidCastException)
            {
                return Resources.PropertyValueInvalidFormat;
            }

            return null;
        }
    }
}
