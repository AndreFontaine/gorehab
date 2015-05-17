// -----------------------------------------------------------------------
// <copyright file="SensorStatusStreamHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver.Sensor
{
    using System.Collections.Generic;

    using Microsoft.Kinect;
    using Microsoft.Samples.Kinect.Webserver.Sensor.Serialization;

    /// <summary>
    /// Implementation of ISensorStatusStreamHandler that exposes sensor status events
    /// </summary>
    public class SensorStatusStreamHandler : SensorStreamHandlerBase
    {
        /// <summary>
        /// JSON name of sensor stream.
        /// </summary>
        internal const string SensorStreamName = "sensorStatus";

        /// <summary>
        /// JSON name of sensor event category.
        /// </summary>
        internal const string SensorStatusEventCategory = "sensorStatus";

        /// <summary>
        /// JSON name of sensor event type.
        /// </summary>
        internal const string SensorStatusEventType = "statusChanged";

        /// <summary>
        /// JSON name of sensor status connected property.
        /// </summary>
        internal const string SensorStatusConnectedPropertyName = "connected";

        /// <summary>
        /// Context that allows this stream handler to communicate with its owner.
        /// </summary>
        private readonly SensorStreamHandlerContext ownerContext;

        /// <summary>
        /// Sensor providing data to sensor state.
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// true if kinect sensor is connected.
        /// </summary>
        private bool isConnected;

        /// <summary>
        /// Initializes a new instance of the <see cref="SensorStatusStreamHandler"/> class
        /// and associates it with a context that allows it to communicate with its owner.
        /// </summary>
        /// <param name="ownerContext">
        /// An instance of <see cref="SensorStreamHandlerContext"/> class.
        /// </param>
        internal SensorStatusStreamHandler(SensorStreamHandlerContext ownerContext)
        {
            this.ownerContext = ownerContext;

            this.AddStreamConfiguration(SensorStreamName, new StreamConfiguration(this.GetSensorStreamProperties, this.SetSensorStreamProperty));
        }

        /// <summary>
        /// Lets ISensorStatusStreamHandler know that Kinect Sensor associated with this stream
        /// handler has changed.
        /// </summary>
        /// <param name="newSensor">
        /// New KinectSensor.
        /// </param>
        public async override void OnSensorChanged(KinectSensor newSensor)
        {
            if (this.sensor != newSensor)
            {
                bool oldConnected = this.isConnected;
                this.isConnected = newSensor != null;

                if (oldConnected != this.isConnected)
                {
                    await this.ownerContext.SendEventMessageAsync(new SensorStatusEventMessage
                    {
                        category = SensorStatusEventCategory,
                        eventType = SensorStatusEventType,
                        connected = this.isConnected
                    });
                }
            }

            this.sensor = newSensor;
        }

        /// <summary>
        /// Gets a sensor stream property value.
        /// </summary>
        /// <param name="propertyMap">
        /// Property name->value map where property values should be set.
        /// </param>
        private void GetSensorStreamProperties(Dictionary<string, object> propertyMap)
        {
            propertyMap.Add(SensorStatusConnectedPropertyName, this.isConnected);
        }

        /// <summary>
        /// Set a sensor stream property value.
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
        private string SetSensorStreamProperty(string propertyName, object propertyValue)
        {
            if (propertyName == SensorStatusConnectedPropertyName)
            {
                return Properties.Resources.PropertyReadOnly;
            }
            else
            {
                return Properties.Resources.PropertyNameUnrecognized;
            }
        }
    }
}
