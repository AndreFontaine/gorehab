// -----------------------------------------------------------------------
// <copyright file="SensorStreamHandlerFactory.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver.Sensor
{
    using System;
    using Microsoft.Samples.Kinect.Webserver.Properties;

    /// <summary>
    /// The supported sensor stream type.
    /// </summary>
    public enum StreamHandlerType
    {
        Skeleton,
        Interaction,
        BackgroundRemoval,
        SensorStatus,
    }

    /// <summary>
    /// Implementation of ISensorStreamHandlerFactory used to create instances of
    /// sensor stream handler objects.
    /// </summary>
    public class SensorStreamHandlerFactory : ISensorStreamHandlerFactory
    {
        /// <summary>
        /// The type of the created stream.
        /// </summary>
        private readonly StreamHandlerType streamType;

        /// <summary>
        /// Initializes a new instance of the <see cref="SensorStreamHandlerFactory"/> class.
        /// </summary>
        /// <param name="streamType">The stream type.</param>
        public SensorStreamHandlerFactory(StreamHandlerType streamType)
        {
            this.streamType = streamType;
        }

        /// <summary>
        /// Creates a sensor stream handler object and associates it with a context that
        /// allows it to communicate with its owner.
        /// </summary>
        /// <param name="context">
        /// An instance of <see cref="SensorStreamHandlerContext"/> class.
        /// </param>
        /// <returns>
        /// A new <see cref="ISensorStreamHandler"/> instance.
        /// </returns>
        public ISensorStreamHandler CreateHandler(SensorStreamHandlerContext context)
        {
            switch (streamType)
            {
                case StreamHandlerType.Skeleton:
                    return new SkeletonStreamHandler(context);
                case StreamHandlerType.Interaction:
                    return new InteractionStreamHandler(context);
                case StreamHandlerType.BackgroundRemoval:
                    return new BackgroundRemovalStreamHandler(context);
                case StreamHandlerType.SensorStatus:
                    return new SensorStatusStreamHandler(context);
                default:
                    throw new NotSupportedException(Resources.UnsupportedStreamType);
            }
        }
    }
}
