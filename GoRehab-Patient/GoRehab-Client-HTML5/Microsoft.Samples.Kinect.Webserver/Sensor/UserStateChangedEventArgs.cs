// -----------------------------------------------------------------------
// <copyright file="UserStateChangedEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver.Sensor
{
    using System;

    using Microsoft.Samples.Kinect.Webserver.Sensor.Serialization;

    /// <summary>
    /// Event arguments for IUserStateManager.UserStateChanged event.
    /// </summary>
    public class UserStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserStateChangedEventArgs"/> class.
        /// </summary>
        /// <param name="message">
        /// Representation of event as a web message to be sent.
        /// </param>
        public UserStateChangedEventArgs(EventMessage message)
        {
            this.Message = message;
        }

        /// <summary>
        /// Representation of event as a web message to be sent.
        /// </summary>
        public EventMessage Message { get; private set; }
    }
}
