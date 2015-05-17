// -----------------------------------------------------------------------
// <copyright file="KinectRequestHandlerFactory.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver.Sensor
{
    using System.Collections.ObjectModel;

    using Microsoft.Kinect.Toolkit;
    using Microsoft.Samples.Kinect.Webserver;

    /// <summary>
    /// Implementation of IHttpRequestHandlerFactory used to create instances of
    /// <see cref="KinectRequestHandler"/> objects.
    /// </summary>
    public class KinectRequestHandlerFactory : IHttpRequestHandlerFactory
    {
        /// <summary>
        /// Sensor chooser used to obtain a KinectSensor.
        /// </summary>
        private readonly KinectSensorChooser sensorChooser;

        /// <summary>
        /// Collection of sensor stream handler factories used to process kinect data and deliver
        /// a data streams ready for web consumption.
        /// </summary>
        private readonly Collection<ISensorStreamHandlerFactory> streamHandlerFactories;

        /// <summary>
        /// Initializes a new instance of the KinectRequestHandlerFactory class.
        /// </summary>
        /// <param name="sensorChooser">
        /// Sensor chooser that will be used to obtain a KinectSensor.
        /// </param>
        /// <remarks>
        /// Default set of sensor stream handler factories will be used.
        /// </remarks>
        public KinectRequestHandlerFactory(KinectSensorChooser sensorChooser)
        {
            this.sensorChooser = sensorChooser;
            this.streamHandlerFactories = CreateDefaultStreamHandlerFactories();
        }

        /// <summary>
        /// Initializes a new instance of the KinectRequestHandlerFactory class.
        /// </summary>
        /// <param name="sensorChooser">
        /// Sensor chooser that will be used to obtain a KinectSensor.
        /// </param>
        /// <param name="streamHandlerFactories">
        /// Collection of stream handler factories to be used to process kinect data and deliver
        /// data streams ready for web consumption.
        /// </param>
        public KinectRequestHandlerFactory(KinectSensorChooser sensorChooser, Collection<ISensorStreamHandlerFactory> streamHandlerFactories)
        {
            this.sensorChooser = sensorChooser;
            this.streamHandlerFactories = streamHandlerFactories;
        }

        /// <summary>
        /// Create collection of default stream handler factories.
        /// </summary>
        /// <returns>
        /// Collection containing default stream handler factories.
        /// </returns>
        public static Collection<ISensorStreamHandlerFactory> CreateDefaultStreamHandlerFactories()
        {
            var streamHandlerTypes = new[]
            {
                StreamHandlerType.Interaction,
                StreamHandlerType.Skeleton,
                StreamHandlerType.BackgroundRemoval,
                StreamHandlerType.SensorStatus
            };

            var factoryCollection = new Collection<ISensorStreamHandlerFactory>();
            foreach (var type in streamHandlerTypes)
            {
                factoryCollection.Add(new SensorStreamHandlerFactory(type));
            }

            return factoryCollection;
        }

        /// <summary>
        /// Creates a request handler object.
        /// </summary>
        /// <returns>
        /// A new <see cref="IHttpRequestHandler"/> instance.
        /// </returns>
        public IHttpRequestHandler CreateHandler()
        {
            return new KinectRequestHandler(this.sensorChooser, this.streamHandlerFactories);
        }
    }
}
