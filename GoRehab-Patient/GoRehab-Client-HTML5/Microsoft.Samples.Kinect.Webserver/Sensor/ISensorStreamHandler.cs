// -----------------------------------------------------------------------
// <copyright file="ISensorStreamHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver.Sensor
{
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;

    using Microsoft.Kinect;

    /// <summary>
    /// Interface called upon to:
    /// a) Process Kinect sensor data associated with a specific stream or set of streams.
    /// b) Handle requests to deliver data associated with the same stream(s).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Objects that implement this interface are not expected to be thread-safe but are expected
    /// to support async operations, so instances should be created and instance methods should
    /// always be called from a single thread that is associated with a SynchronizationContext
    /// that provides single-threaded message dispatch
    /// (e.g.: System.Windows.Threading.DispatcherSynchronizationContext).
    /// </para>
    /// <para>
    /// This means that IHttpRequestHandlers still have to be aware of potential reentrancy
    /// resulting from asynchronous operations, but they don't have to protect data access with
    /// locks.
    /// </para>
    /// </remarks>
    public interface ISensorStreamHandler
    {
        /// <summary>
        /// Get the names of the stream(s) supported by this stream handler.
        /// </summary>
        /// <returns>
        /// An array of stream names.
        /// </returns>
        /// <remarks>
        /// These names will be used in JSON objects to refer to individual streams.
        /// </remarks>
        string[] GetSupportedStreamNames();

        /// <summary>
        /// Lets ISensorStreamHandler know that Kinect Sensor associated with this stream
        /// handler has changed.
        /// </summary>
        /// <param name="newSensor">
        /// New KinectSensor.
        /// </param>
        void OnSensorChanged(KinectSensor newSensor);

        /// <summary>
        /// Process data from one Kinect color frame.
        /// </summary>
        /// <param name="colorData">
        /// Kinect color data.
        /// </param>
        /// <param name="colorFrame">
        /// <see cref="ColorImageFrame"/> from which we obtained color data.
        /// </param>
        void ProcessColor(byte[] colorData, ColorImageFrame colorFrame);

        /// <summary>
        /// Process data from one Kinect depth frame.
        /// </summary>
        /// <param name="depthData">
        /// Kinect depth data.
        /// </param>
        /// <param name="depthFrame">
        /// <see cref="DepthImageFrame"/> from which we obtained depth data.
        /// </param>
        void ProcessDepth(DepthImagePixel[] depthData, DepthImageFrame depthFrame);

        /// <summary>
        /// Process data from one Kinect skeleton frame.
        /// </summary>
        /// <param name="skeletons">
        /// Kinect skeleton data.
        /// </param>
        /// <param name="skeletonFrame">
        /// <see cref="SkeletonFrame"/> from which we obtained skeleton data.
        /// </param>
        void ProcessSkeleton(Skeleton[] skeletons, SkeletonFrame skeletonFrame);

        /// <summary>
        /// Gets the state property values associated with the specified stream name.
        /// </summary>
        /// <param name="streamName">
        /// Name of stream for which property values should be returned.
        /// </param>
        /// <returns>
        /// Dictionary mapping property names to property values.
        /// </returns>
        IDictionary<string, object> GetState(string streamName);

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
        bool SetState(string streamName, IReadOnlyDictionary<string, object> properties, IDictionary<string, object> errors);

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
        Task HandleRequestAsync(string streamName, HttpListenerContext requestContext, string subpath);

        /// <summary>
        /// Cancel all pending operations
        /// </summary>
        void Cancel();

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
        Task UninitializeAsync();
    }
}
