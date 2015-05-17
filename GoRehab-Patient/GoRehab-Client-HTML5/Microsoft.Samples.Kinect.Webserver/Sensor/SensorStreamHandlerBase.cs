//------------------------------------------------------------------------------
// <copyright file="SensorStreamHandlerBase.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver.Sensor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    using Microsoft.Kinect;

    using PropertyMap = System.Collections.Generic.Dictionary<string, object>;

    /// <summary>
    /// Base implementation for <see cref="ISensorStreamHandler"/> interface.
    /// </summary>
    public class SensorStreamHandlerBase : ISensorStreamHandler
    {
        /// <summary>
        /// Map of supported stream names to corresponding stream configurations.
        /// </summary>
        private readonly Dictionary<string, StreamConfiguration> streamHandlerConfiguration = new Dictionary<string, StreamConfiguration>();

        /// <summary>
        /// Object used to discard errors when clients didn't specify an error dictionary.
        /// </summary>
        private readonly IDictionary<string, object> errorSink = new PropertyMap();

        /// <summary>
        /// Array of supported stream names.
        /// </summary>
        private string[] supportedStreams;

        /// <summary>
        /// Initializes a new instance of the <see cref="SensorStreamHandlerBase"/> class.
        /// </summary>
        protected SensorStreamHandlerBase()
        {
        }

        /// <summary>
        /// Get the names of the stream(s) supported by this stream handler.
        /// </summary>
        /// <returns>
        /// An array of stream names.
        /// </returns>
        /// <remarks>
        /// These names will be used in JSON objects to refer to individual streams.
        /// </remarks>
        public string[] GetSupportedStreamNames()
        {
            return this.supportedStreams ?? (this.supportedStreams = this.streamHandlerConfiguration.Keys.ToArray());
        }

        /// <summary>
        /// Lets ISensorStreamHandler know that Kinect Sensor associated with this stream
        /// handler has changed.
        /// </summary>
        /// <param name="newSensor">
        /// New KinectSensor.
        /// </param>
        public virtual void OnSensorChanged(KinectSensor newSensor)
        {
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
        public virtual void ProcessColor(byte[] colorData, ColorImageFrame colorFrame)
        {
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
        public virtual void ProcessDepth(DepthImagePixel[] depthData, DepthImageFrame depthFrame)
        {
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
        public virtual void ProcessSkeleton(Skeleton[] skeletons, SkeletonFrame skeletonFrame)
        {
        }

        /// <summary>
        /// Gets the state property values associated with the specified stream name.
        /// </summary>
        /// <param name="streamName">
        /// Name of stream for which property values should be returned.
        /// </param>
        /// <returns>
        /// Dictionary mapping property names to property values.
        /// </returns>
        public IDictionary<string, object> GetState(string streamName)
        {
            var propertyMap = new Dictionary<string, object>();

            StreamConfiguration config;
            if (!this.streamHandlerConfiguration.TryGetValue(streamName, out config))
            {
                throw new ArgumentException(@"Unsupported stream name", "streamName");
            }

            config.GetPropertiesCallback(propertyMap);

            return propertyMap;
        }

        /// <summary>
        /// Attempts to set the specified state property values associated with the specified
        /// stream name.
        /// </summary>
        /// <param name="streamName">
        /// Name of stream for which property values should be set.
        /// </param>
        /// <param name="properties">
        /// Dictionary mapping property names to property values that should be set.
        /// Must not be null.
        /// </param>
        /// <param name="errors">
        /// Dictionary meant to receive mappings between property names to errors encountered
        /// while trying to set each property value.
        /// May be null.
        /// </param>
        /// <returns>
        /// true if there were no errors encountered while setting state. false otherwise.
        /// </returns>
        /// <remarks>
        /// If <paramref name="errors"/> is non-null, it is expected that it will be empty
        /// when method is called.
        /// </remarks>
        public bool SetState(string streamName, IReadOnlyDictionary<string, object> properties, IDictionary<string, object> errors)
        {
            bool successful = true;

            if (properties == null)
            {
                throw new ArgumentException(@"properties must not be null", "properties");
            }

            if (errors == null)
            {
                // Guarantee code down the line that errors will not be null, but don't let
                // the error sink ever get too large.
                this.errorSink.Clear();
                errors = this.errorSink;
            }

            StreamConfiguration config;
            if (!this.streamHandlerConfiguration.TryGetValue(streamName, out config))
            {
                throw new ArgumentException(@"Unsupported stream name", "streamName");
            }

            foreach (var keyValuePair in properties)
            {
                try
                {
                    var error = config.SetPropertyCallback(keyValuePair.Key, keyValuePair.Value);
                    if (error != null)
                    {
                        errors.Add(keyValuePair.Key, error);
                        successful = false;
                    }
                }
                catch (InvalidOperationException)
                {
                    successful = false;
                    errors.Add(keyValuePair.Key, Properties.Resources.PropertySetError);
                }
            }

            return successful;
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
        public virtual Task HandleRequestAsync(string streamName, HttpListenerContext requestContext, string subpath)
        {
            KinectRequestHandler.CloseResponse(requestContext, HttpStatusCode.NotFound);
            return SharedConstants.EmptyCompletedTask;
        }

        /// <summary>
        /// Cancel all pending operations
        /// </summary>
        public virtual void Cancel()
        {
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
        public virtual Task UninitializeAsync()
        {
            return SharedConstants.EmptyCompletedTask;
        }

        /// <summary>
        /// Add a configuration corresponding to the specified stream name.
        /// </summary>
        /// <param name="name">
        /// Stream name.
        /// </param>
        /// <param name="configuration">
        /// Stream configuration.
        /// </param>
        protected void AddStreamConfiguration(string name, StreamConfiguration configuration)
        {
            this.streamHandlerConfiguration.Add(name, configuration);
            this.supportedStreams = null;
        }

        /// <summary>
        /// Helper class used to configure SensorStreamHandlerBase subclass behavior.
        /// </summary>
        protected class StreamConfiguration
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="StreamConfiguration"/> class.
            /// </summary>
            /// <param name="getPropertiesCallback">
            /// Callback function used to get all state property values.
            /// </param>
            /// <param name="setPropertyCallback">
            /// Callback function used to set individual state property values.
            /// </param>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Outer generic is a functional type and won't require cumbersome syntax to use")]
            public StreamConfiguration(Action<PropertyMap> getPropertiesCallback, Func<string, object, string> setPropertyCallback)
            {
                this.GetPropertiesCallback = getPropertiesCallback;
                this.SetPropertyCallback = setPropertyCallback;
            }

            /// <summary>
            /// Gets the callback function used to get all state property values.
            /// </summary>
            /// <remarks>
            /// Callback parameter is a property name->value map where property values should
            /// be set.
            /// </remarks>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Outer generic is a functional type and won't require cumbersome syntax to use")]
            public Action<PropertyMap> GetPropertiesCallback { get; private set; }

            /// <summary>
            /// Gets the callback function used to set individual state property values.
            /// </summary>
            public Func<string, object, string> SetPropertyCallback { get; private set; }
        }
    }
}
