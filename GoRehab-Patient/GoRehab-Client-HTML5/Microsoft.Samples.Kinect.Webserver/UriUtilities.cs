// -----------------------------------------------------------------------
// <copyright file="UriUtilities.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Static class that defines uri manipulation utilities.
    /// </summary>
    public static class UriUtilities
    {
        /// <summary>
        /// Separator between URI path segments.
        /// </summary>
        public const string PathSeparator = "/";

        /// <summary>
        /// Concatenate specified path segments at the end of specified URI.
        /// </summary>
        /// <param name="uri">
        /// Absolute URI to serve as starting point of concatenation.
        /// </param>
        /// <param name="pathSegments">
        /// Path segments to concatenate at the end of URI.
        /// </param>
        /// <returns>
        /// URI that represents the combination of the specified uri and path segments.
        /// May be null if uri segments could not be concatenated.
        /// </returns>
        public static Uri ConcatenateSegments(this Uri uri, params string[] pathSegments)
        {
            Uri result = uri;

            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            if (pathSegments == null)
            {
                throw new ArgumentNullException("pathSegments");
            }

            for (int i = 0; i < pathSegments.Length; ++i)
            {
                var segment = pathSegments[i];

                if (segment == null)
                {
                    throw new ArgumentException(@"One or more of the specified path segments is null", "pathSegments");
                }

                if (i < pathSegments.Length - 1)
                {
                    // For each element other than the last element, make sure it ends in the
                    // path separator character so that it's treated as a path segment rather
                    // than an endpoint or resource (see CoInternetCombineIUri documentation
                    // for an explanation of standard URI combination behavior)
                    segment = segment.EndsWith(PathSeparator, StringComparison.OrdinalIgnoreCase) ? segment : (segment + PathSeparator);
                }

                // Now call the standard URI class to take care of canonicalization and other
                // combination functionality
                var previous = result;
                try
                {
                    result = new Uri(previous, new Uri(segment.Trim(), UriKind.Relative));
                }
                catch (UriFormatException)
                {
                    Trace.TraceError("Unable to concatenate uri \"{0}\" with path segment \"{1}\"", previous, segment);
                    result = null;
                    break;
                }
            }

            return result;
        }
    }
}
