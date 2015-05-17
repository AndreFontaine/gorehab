// -----------------------------------------------------------------------
// <copyright file="SharedConstants.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver
{
    using System.Threading.Tasks;

    /// <summary>
    /// Placeholder class that declares commonly used constants and configuration values.
    /// </summary>
    internal static class SharedConstants
    {
        /// <summary>
        /// Invalid tracking identifier.
        /// </summary>
        internal const int InvalidUserTrackingId = 0;

        /// <summary>
        /// Maximum number of users that can get assigned a skeleton tracking id.
        /// </summary>
        internal const int MaxUsersTracked = 6;

        /// <summary>
        /// Array of standard character delimiters used to separate path components in a URI
        /// string.
        /// </summary>
        internal static readonly char[] UriPathComponentDelimiters = new[] { '/', '?' };

        /// <summary>
        /// Empty task with a <see cref="TaskStatus.RanToCompletion"/> status, so awaiting
        /// on task will return immediately.
        /// </summary>
        /// <remarks>
        /// This is a helper value to be returned from synchronous functions that implement
        /// an asynchronous contract, so clients can always await without needing to check
        /// for null.
        /// </remarks>
        internal static readonly Task EmptyCompletedTask = Task.FromResult(0);

        /// <summary>
        /// Create a new task that will block all clients awaiting on it until
        /// <see cref="Task.Start()"/> is called on it.
        /// </summary>
        /// <returns>
        /// New await-able task.
        /// </returns>
        internal static Task CreateNonstartedTask()
        {
            return new Task(() => { });
        }
    }
}
