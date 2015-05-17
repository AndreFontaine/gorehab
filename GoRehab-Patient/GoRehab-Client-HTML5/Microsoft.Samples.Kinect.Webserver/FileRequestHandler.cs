// -----------------------------------------------------------------------
// <copyright file="FileRequestHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;
    using System.Web;

    /// <summary>
    /// Implementation of IHttpRequestHandler used to serve static file content.
    /// </summary>
    public class FileRequestHandler : IHttpRequestHandler
    {
        /// <summary>
        /// Root directory in server's file system from which we're serving files.
        /// </summary>
        private readonly DirectoryInfo rootDirectory;

        /// <summary>
        /// Origin used as a helper for URI parsing and canonicalization.
        /// </summary>
        private readonly Uri parseHelperOrigin = new Uri("http://a");

        /// <summary>
        /// Initializes a new instance of the <see cref="FileRequestHandler"/> class.
        /// </summary>
        /// <param name="rootDirectory">
        /// Root directory in server's file system from which files should be served.
        /// </param>
        internal FileRequestHandler(DirectoryInfo rootDirectory)
        {
            if (rootDirectory == null)
            {
                throw new ArgumentNullException("rootDirectory");
            }
            
            // Re-create directory name to ensure equivalence between directory names
            // that end in "\" character and directory names that are identical except
            // that they don't end in "\" character.
            var normalizedDirectoryName = rootDirectory.Parent != null
                                              ? Path.Combine(rootDirectory.Parent.FullName, rootDirectory.Name)
                                              : rootDirectory.Name;
            this.rootDirectory = new DirectoryInfo(normalizedDirectoryName);
        }

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
        public Task InitializeAsync()
        {
            return SharedConstants.EmptyCompletedTask;
        }

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
        public async Task HandleRequestAsync(HttpListenerContext requestContext, string subpath)
        {
            if (requestContext == null)
            {
                throw new ArgumentNullException("requestContext");
            }

            if (subpath == null)
            {
                throw new ArgumentNullException("subpath");
            }

            try
            {
                var statusCode = await this.ServeFileAsync(requestContext.Response, subpath);
                CloseResponse(requestContext, statusCode);
            }
            catch (HttpListenerException e)
            {
                Trace.TraceWarning(
                    "Problem encountered while serving file at subpath {0}. Client might have aborted request. Cause: \"{1}\"", subpath, e.Message);
            }
        }

        /// <summary>
        /// Cancel all pending operations.
        /// </summary>
        public void Cancel()
        {
        }

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
        public Task UninitializeAsync()
        {
            return SharedConstants.EmptyCompletedTask;
        }

        /// <summary>
        /// Close response stream and associate a status code with response.
        /// </summary>
        /// <param name="context">
        /// Context whose response we should close.
        /// </param>
        /// <param name="statusCode">
        /// Status code.
        /// </param>
        internal static void CloseResponse(HttpListenerContext context, HttpStatusCode statusCode)
        {
            context.Response.StatusCode = (int)statusCode;
            context.Response.Close();
        }

        /// <summary>
        /// Determine if the specified directory is an ancestor of the specified file.
        /// </summary>
        /// <param name="ancestorDirectory">
        /// Information for potential ancestor directory.
        /// </param>
        /// <param name="file">
        /// File for which to determine directory ancestry.
        /// </param>
        /// <returns>
        /// True if <paramref name="ancestorDirectory"/> is an ancestor of <paramref name="file"/>.
        /// </returns>
        internal static bool IsAncestorDirectory(DirectoryInfo ancestorDirectory, FileInfo file)
        {
            var parentDirectory = file.Directory;

            while (parentDirectory != null)
            {
                if (string.Compare(parentDirectory.FullName, ancestorDirectory.FullName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return true;
                }

                parentDirectory = parentDirectory.Parent;
            }

            return false;
        }

        /// <summary>
        /// Serve the file corresponding to the specified URI path.
        /// </summary>
        /// <param name="response">
        /// HTTP response where file will be streamed, on success.
        /// </param>
        /// <param name="subpath">
        /// Request URI path relative to the URI prefix associated with this request
        /// handler in the HttpListener.
        /// </param>
        /// <returns>
        /// Await-able task holding an HTTP status code that should be part of response.
        /// </returns>
        private async Task<HttpStatusCode> ServeFileAsync(HttpListenerResponse response, string subpath)
        {
            Uri uri;

            try
            {
                uri = new Uri(this.parseHelperOrigin, subpath);
            }
            catch (UriFormatException)
            {
                return HttpStatusCode.Forbidden;
            }

            var filePath = Path.Combine(this.rootDirectory.FullName, uri.AbsolutePath.Substring(1));
            var fileInfo = new FileInfo(filePath);

            if (!IsAncestorDirectory(this.rootDirectory, fileInfo))
            {
                return HttpStatusCode.Forbidden;
            }

            if (!fileInfo.Exists)
            {
                return HttpStatusCode.NotFound;
            }

            response.ContentType = MimeMapping.GetMimeMapping(fileInfo.FullName);

            using (var fileStream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                await fileStream.CopyToAsync(response.OutputStream);
            }

            return HttpStatusCode.OK;
        }
    }
}
