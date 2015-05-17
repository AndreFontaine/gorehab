// -----------------------------------------------------------------------
// <copyright file="FunctionCallResponse.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver.Sensor.Serialization
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Serializable representation of a function call response.
    /// </summary>
    /// <typeparam name="T">
    /// Type of function call result.
    /// </typeparam>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Lower case names allowed for JSON serialization.")]
    internal class FunctionCallResponse<T>
    {
        /// <summary>
        /// Sequence Id used to match function call request with its response.
        /// </summary>
        public int id { get; set; }

        /// <summary>
        /// Result of remote function call.
        /// </summary>
        public T result { get; set; }
    }
}
