// -----------------------------------------------------------------------
// <copyright file="FunctionCallRequest.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver.Sensor.Serialization
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Serializable representation of a function call request.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Lower case names allowed for JSON serialization.")]
    internal class FunctionCallRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionCallRequest"/> class.
        /// </summary>
        /// <param name="functionName">
        /// Name of remote function to invoke.
        /// </param>
        /// <param name="args">
        /// Function arguments.
        /// </param>
        /// <param name="sequenceId">
        /// Sequence Id used to match function call request with its response.
        /// </param>
        public FunctionCallRequest(string functionName, object[] args, int sequenceId)
        {
            this.name = functionName;
            this.args = args;
            this.id = sequenceId;
        }

        public string name { get; set; }

        /// <summary>
        /// Function arguments.
        /// </summary>
        public object[] args { get; set; }

        /// <summary>
        /// Sequence Id used to match function call request with its response.
        /// </summary>
        public int id { get; set; }
    }
}
