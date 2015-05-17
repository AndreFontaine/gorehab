// -----------------------------------------------------------------------
// <copyright file="FileRequestHandlerFactory.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver
{
    using System;
    using System.Globalization;
    using System.IO;

    /// <summary>
    /// Implementation of IHttpRequestHandlerFactory used to create instances of
    /// <see cref="FileRequestHandler"/> objects.
    /// </summary>
    public class FileRequestHandlerFactory : IHttpRequestHandlerFactory
    {
        /// <summary>
        /// Root directory in server's file system from which we're serving files.
        /// </summary>
        private readonly DirectoryInfo rootDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileRequestHandlerFactory"/> class.
        /// </summary>
        /// <param name="rootDirectoryName">
        /// Root directory name in server's file system from which files should be served.
        /// The directory must exist at the time of the call.
        /// </param>
        internal FileRequestHandlerFactory(string rootDirectoryName)
        {
            if (!Directory.Exists(rootDirectoryName))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, @"The specified directory '{0}' does not exist", rootDirectoryName), "rootDirectoryName");
            }

            this.rootDirectory = new DirectoryInfo(rootDirectoryName);
        }

        public IHttpRequestHandler CreateHandler()
        {
            return new FileRequestHandler(this.rootDirectory);
        }
    }
}
