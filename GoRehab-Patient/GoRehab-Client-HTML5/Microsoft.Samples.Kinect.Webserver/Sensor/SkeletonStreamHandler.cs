// -----------------------------------------------------------------------
// <copyright file="SkeletonStreamHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver.Sensor
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Kinect;
    using Microsoft.Samples.Kinect.Webserver.Sensor.Serialization;

    /// <summary>
    /// Implementation of ISensorStreamHandler that exposes skeleton streams.
    /// </summary>
    public class SkeletonStreamHandler : SensorStreamHandlerBase
    {
        /// <summary>
        /// JSON name of skeleton stream.
        /// </summary>
        internal const string SkeletonStreamName = "skeleton";

        /// <summary>
        /// Context that allows this stream handler to communicate with its owner.
        /// </summary>
        private readonly SensorStreamHandlerContext ownerContext;

        /// <summary>
        /// Serializable skeleton stream message, reused as skeleton frames arrive.
        /// </summary>
        private readonly SkeletonStreamMessage skeletonStreamMessage = new SkeletonStreamMessage { stream = SkeletonStreamName };

        /// <summary>
        /// true if skeleton stream is enabled. Skeleton stream is disabled by default.
        /// </summary>
        private bool skeletonIsEnabled;

        /// <summary>
        /// Keep track if we're in the middle of processing an skeleton frame.
        /// </summary>
        private bool isProcessingSkeletonFrame;

        /// <summary>
        /// Initializes a new instance of the <see cref="SkeletonStreamHandler"/> class
        /// and associates it with a context that allows it to communicate with its owner.
        /// </summary>
        /// <param name="ownerContext">
        /// An instance of <see cref="SensorStreamHandlerContext"/> class.
        /// </param>
        internal SkeletonStreamHandler(SensorStreamHandlerContext ownerContext)
        {
            this.ownerContext = ownerContext;

            this.AddStreamConfiguration(SkeletonStreamName, new StreamConfiguration(this.GetProperties, this.SetProperty));
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
            if (skeletonFrame == null)
            {
                throw new ArgumentNullException("skeletonFrame");
            }

            this.ProcessSkeletonAsync(skeletons, skeletonFrame.Timestamp);
        }

        /// <summary>
        /// Process skeletons in async mode.
        /// </summary>
        /// <param name="skeletons">
        /// Kinect skeleton data.
        /// </param>
        /// <param name="timestamp">
        /// Timestamp of <see cref="SkeletonFrame"/> from which we obtained skeleton data.
        /// </param>
        internal async void ProcessSkeletonAsync(Skeleton[] skeletons, long timestamp)
        {
            if (!this.skeletonIsEnabled)
            {
                return;
            }

            if (this.isProcessingSkeletonFrame)
            {
                // Re-entered SkeletonFrameReadyAsync while a previous frame is already being processed.
                // Just ignore new frames until the current one finishes processing.
                return;
            }

            this.isProcessingSkeletonFrame = true;

            try
            {
                if (skeletons != null)
                {
                    this.skeletonStreamMessage.timestamp = timestamp;
                    this.skeletonStreamMessage.UpdateSkeletons(skeletons);

                    await this.ownerContext.SendStreamMessageAsync(this.skeletonStreamMessage);
                }
            }
            finally
            {
                this.isProcessingSkeletonFrame = false;
            }
        }

        /// <summary>
        /// Gets a skeleton stream property value.
        /// </summary>
        /// <param name="propertyMap">
        /// Property name->value map where property values should be set.
        /// </param>
        private void GetProperties(Dictionary<string, object> propertyMap)
        {
            propertyMap.Add(KinectRequestHandler.EnabledPropertyName, this.skeletonIsEnabled);
        }

        /// <summary>
        /// Set a skeleton stream property value.
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
                // None of the skeleton stream properties accept a null value
                return Properties.Resources.PropertyValueInvalidFormat;
            }

            try
            {
                switch (propertyName)
                {
                    case KinectRequestHandler.EnabledPropertyName:
                        this.skeletonIsEnabled = (bool)propertyValue;
                        break;

                    default:
                        recognized = false;
                        break;
                }
            }
            catch (InvalidCastException)
            {
                return Properties.Resources.PropertyValueInvalidFormat;
            }

            if (!recognized)
            {
                return Properties.Resources.PropertyNameUnrecognized;
            }

            return null;
        }
    }
}
