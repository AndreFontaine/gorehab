// -----------------------------------------------------------------------
// <copyright file="ISensorStreamHandlerFactory.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver.Sensor
{
    public interface ISensorStreamHandlerFactory
    {
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
        ISensorStreamHandler CreateHandler(SensorStreamHandlerContext context);
    }
}
