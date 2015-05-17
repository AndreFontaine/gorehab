// -----------------------------------------------------------------------
// <copyright file="IHttpRequestHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver
{
    using System.Net;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface called upon to handle HTTP requests.
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
    public interface IHttpRequestHandler
    {
        /// <summary>
        /// Prepares handler to start receiving HTTP requests.
        /// </summary>
        /// <returns>
        /// Await-able task.
        /// </returns>
        /// <remarks>
        /// Return value should never be null. Implementations should use Task.FromResult(0)
        /// if function is implemented synchronously so that callers can await without
        /// needing to check for null.
        /// </remarks>
        Task InitializeAsync();

        /// <summary>
        /// Handle an http request.
        /// </summary>
        /// <param name="requestContext">
        /// Context containing HTTP request data, which will also contain associated
        /// response upon return.
        /// </param>
        /// <param name="subpath">
        /// Request URI path relative to the URI prefix associated with this request
        /// handler in the HttpListener.
        /// </param>
        /// <returns>
        /// Await-able task.
        /// </returns>
        /// <remarks>
        /// Return value should never be null. Implementations should use Task.FromResult(0)
        /// if function is implemented synchronously so that callers can await without
        /// needing to check for null.
        /// </remarks>
        Task HandleRequestAsync(HttpListenerContext requestContext, string subpath);

        /// <summary>
        /// Cancel all pending operations.
        /// </summary>
        void Cancel();

        /// <summary>
        /// Lets handler know that no more HTTP requests will be received, so that it can
        /// clean up resources associated with request handling.
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
