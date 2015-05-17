// -----------------------------------------------------------------------
// <copyright file="IHttpRequestHandlerFactory.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver
{
    /// <summary>
    /// Factory of <see cref="IHttpRequestHandler"/> objects.
    /// </summary>
    public interface IHttpRequestHandlerFactory
    {
        /// <summary>
        /// Creates a request handler object.
        /// </summary>
        /// <returns>
        /// A new <see cref="IHttpRequestHandler"/> instance.
        /// </returns>
        IHttpRequestHandler CreateHandler();
    }
}
