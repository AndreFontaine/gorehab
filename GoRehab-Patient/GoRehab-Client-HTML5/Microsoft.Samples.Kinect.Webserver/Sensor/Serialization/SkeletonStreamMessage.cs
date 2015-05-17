// -----------------------------------------------------------------------
// <copyright file="SkeletonStreamMessage.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver.Sensor.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    using Microsoft.Kinect;

    /// <summary>
    /// Serializable representation of an skeleton stream message to send to client.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter",
        Justification = "Lower case names allowed for JSON serialization.")]
    public class SkeletonStreamMessage : StreamMessage
    {
        /// <summary>
        /// Serializable skeleton array.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "skeletons", Justification = "Lower case names allowed for JSON serialization.")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Array properties allowed for JSON serialization.")]
        public object[] skeletons { get; set; }

        /// <summary>
        /// Update hand pointers from specified user info data.
        /// </summary>
        /// <param name="skeletons">
        /// Enumeration of UserInfo structures.
        /// </param>
        public void UpdateSkeletons(Skeleton[] skeletons)
        {
            if (skeletons == null)
            {
                throw new ArgumentNullException("skeletons");
            }

            if (this.skeletons == null || this.skeletons.Length != skeletons.Length)
            {
                this.skeletons = new object[skeletons.Length];
            }

            for (int i = 0; i < this.skeletons.Length; i ++)
            {
                this.skeletons[i] = JsonSerializationExtensions.ExtractSerializableJsonData(skeletons[i]);
            }
        }
    }
}