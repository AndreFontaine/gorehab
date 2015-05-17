// -----------------------------------------------------------------------
// <copyright file="KinectWebserver.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;

    using Microsoft.Kinect.Toolkit;
    using Microsoft.Samples.Kinect.Webserver.Sensor;

    /// <summary>
    /// Server used to expose Kinect state and events to web clients.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Instances of this class are not thread-safe, and are expected to be called from
    /// a single thread, e.g.: a UI thread.
    /// </para>
    /// <para>
    /// By default listens for connections at origin http://localhost:8181 and does not
    /// serve files until owner configures FileServerRootDirectory property.
    /// </para>
    /// </remarks>
    public sealed class KinectWebserver
    {
        /// <summary>
        /// Base URI path (within expected origin) for Kinect endpoints.
        /// </summary>
        private const string KinectEndpointBasePath = "/Kinect";

        /// <summary>
        /// Base URI path (within expected origin) for file server endpoints.
        /// </summary>
        private const string DefaultFileEndpointBasePath = "/files";

        /// <summary>
        /// Default HTTP URI origin to listen on.
        /// </summary>
        private static readonly Uri DefaultUriOrigin = new Uri("http://localhost:8181");
        
        /// <summary>
        /// Mapping between URI base path names to be used by clients to refer to data from a
        /// specific Kinect sensor and their corresponding KinectSensorChooser objects.
        /// </summary>
        private readonly Dictionary<string, KinectSensorChooser> sensorChooserMap = new Dictionary<string, KinectSensorChooser>();

        /// <summary>
        /// Origin Uris that are allowed to access data owned by this server.
        /// </summary>
        private readonly List<Uri> allowedOrigins = new List<Uri>();

        /// <summary>
        /// Collection of stream handler factories to be used to process kinect data and deliver
        /// data streams ready for web consumption.
        /// </summary>
        private readonly Collection<ISensorStreamHandlerFactory> streamHandlerFactories;

        /// <summary>
        /// Snapshot of mapping between URI base path names to be used by clients to refer to
        /// data from a specific Kinect sensor and their corresponding KinectSensorChooser
        /// objects, taken every time we create a new listener.
        /// </summary>
        private Dictionary<string, KinectSensorChooser> sensorChooserMapSnapshot = new Dictionary<string, KinectSensorChooser>();

        /// <summary>
        /// Snapshot of origin Uris that are allowed to access data owned by this server,
        /// taken every time we create a new listener.
        /// </summary>
        private List<Uri> allowedOriginsSnapshot = new List<Uri>();

        /// <summary>
        /// Snapshot of collection of stream handler factories to be used to process kinect data and deliver
        /// data streams ready for web consumption, taken every time we create a new listener.
        /// </summary>
        private Collection<ISensorStreamHandlerFactory> streamHandlerFactoriesSnapshot = new Collection<ISensorStreamHandlerFactory>(); 

        /// <summary>
        /// Uri specifying origin associated with web server.
        /// </summary>
        private Uri originUri = DefaultUriOrigin;

        /// <summary>
        /// True if origin URI has changed since last time we started listening for
        /// connections.
        /// </summary>
        private bool hasOriginChanged;

        /// <summary>
        /// URI base path name to be used by clients to refer to HTTP endpoint that serves
        /// static files.
        /// </summary>
        private string fileServerBasePath = DefaultFileEndpointBasePath;

        /// <summary>
        /// Root directory in local file system from which static files are served.
        /// </summary>
        /// <remarks>
        /// If null, static files will not be served.
        /// </remarks>
        private string fileServerRootDirectory;

        /// <summary>
        /// True if static file server settings have changed since last time we started
        /// listening for connections.
        /// </summary>
        private bool hasFileServerChanged;
        
        /// <summary>
        /// Listener that handles HTTP requests in a dedicated thread.
        /// </summary>
        private ThreadHostedHttpListener listener;

        /// <summary>
        /// Initializes a new instance of the <see cref="KinectWebserver"/> class.
        /// </summary>
        public KinectWebserver()
        {
            this.streamHandlerFactories = KinectRequestHandlerFactory.CreateDefaultStreamHandlerFactories();
        }

        /// <summary>
        /// Event used to signal that the server has started listening for connections.
        /// </summary>
        public event EventHandler<EventArgs> Started;

        /// <summary>
        /// Event used to signal that the server has stopped listening for connections.
        /// </summary>
        public event EventHandler<EventArgs> Stopped;

        /// <summary>
        /// Mapping between URI base path names to be used by clients to refer to data from a
        /// specific Kinect sensor and their corresponding KinectSensorChooser objects.
        /// </summary>
        /// <remarks>
        /// Changes made to this map are only reflected in the server request-handling
        /// behavior after a call to <see cref="Start"/> method after the modification
        /// has taken place.
        /// </remarks>
        public Dictionary<string, KinectSensorChooser> SensorChooserMap
        {
            get
            {
                return this.sensorChooserMap;
            }
        }

        /// <summary>
        /// Origin Uris that are allowed to access data served by this listener, in addition
        /// to owned origin Uri.
        /// </summary>
        /// <remarks>
        /// Changes made to this list are only reflected in the server request-handling
        /// behavior after a call to <see cref="Start"/> method after the modification
        /// has taken place.
        /// </remarks>
        public ICollection<Uri> AccessControlAllowedOrigins
        {
            get
            {
                return this.allowedOrigins;
            }
        }

        /// <summary>
        /// Set of stream handler factories to be used to process kinect data and deliver
        /// data streams ready for web consumption.
        /// </summary>
        public ICollection<ISensorStreamHandlerFactory> SensorStreamHandlerFactories
        {
            get
            {
                return this.streamHandlerFactories;
            }
        }

        /// <summary>
        /// URI base path name to be used by clients to refer to HTTP endpoint that serves
        /// static files.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Must not be a null or empty string.
        /// </para>
        /// <para>
        /// Valid examples: "file", "files", "content", "resources", etc.
        /// </para>
        /// <para>
        /// Changes made to this map are only reflected in the server request-handling
        /// behavior after a call to <see cref="Start"/> method after the modification
        /// has taken place.
        /// </para>
        /// </remarks>
        public string FileServerBasePath
        {
            get
            {
                return this.fileServerBasePath;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException(@"FileServerBasePath value must not be null or empty");
                }
                
                this.fileServerBasePath = value;
                this.hasFileServerChanged = true;
            }
        }

        /// <summary>
        /// Origin URI where web server is listening for requests.
        /// </summary>
        public Uri OriginUri
        {
            get
            {
                return this.originUri;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                this.originUri = value;
                this.hasOriginChanged = true;
            }
        }

        /// <summary>
        /// Root directory in local file system from which static files are served.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If null, static files will not be served.
        /// </para>
        /// <para>
        /// Changes made to this map are only reflected in the server request-handling
        /// behavior after a call to <see cref="Start"/> method after the modification
        /// has taken place.
        /// </para>
        /// </remarks>
        public string FileServerRootDirectory
        {
            get
            {
                return this.fileServerRootDirectory;
            }

            set
            {
                this.fileServerRootDirectory = value;
                this.hasFileServerChanged = true;
            }
        }

        /// <summary>
        /// Start listening for requests.
        /// </summary>
        public void Start()
        {
            if (this.SensorChooserMap.Count <= 0)
            {
                if (this.listener != null)
                {
                    this.listener.Stop();
                }

                return;
            }

            bool snapshotMatch = true;

            // Check if the sensor chooser map, the allowed origins list or the collection
            // of stream handler factories have changed since last snapshot was taken.
            // If any of them has changed we need to create a new ThreadHostedHttpListener,
            // since the listener handler factory map is immutable data per instance.
            if (!SensorMapEquals(this.sensorChooserMap, this.sensorChooserMapSnapshot))
            {
                snapshotMatch = false;
                this.sensorChooserMapSnapshot = new Dictionary<string, KinectSensorChooser>(this.sensorChooserMap);
            }

            if (!this.allowedOrigins.SequenceEqual(this.allowedOriginsSnapshot))
            {
                snapshotMatch = false;
                this.allowedOriginsSnapshot = new List<Uri>(this.allowedOrigins);
            }

            if (!this.streamHandlerFactoriesSnapshot.SequenceEqual(this.streamHandlerFactories))
            {
                snapshotMatch = false;
                this.streamHandlerFactoriesSnapshot = new Collection<ISensorStreamHandlerFactory>(this.streamHandlerFactories);
            }

            if (!snapshotMatch || this.hasFileServerChanged || this.hasOriginChanged || (this.listener == null))
            {
                if (this.listener != null)
                {
                    // we need to stop the current listener before creating a new one with
                    // diferent parameters
                    this.listener.Stop(true);
                    this.listener.Started -= this.ListenerOnStarted;
                    this.listener.Stopped -= this.ListenerOnStopped;
                    this.listener = null;
                }

                var factoryMap = new Dictionary<string, IHttpRequestHandlerFactory>();
                foreach (var entry in this.sensorChooserMapSnapshot)
                {
                    factoryMap.Add(
                        string.Format(CultureInfo.InvariantCulture, "{0}/{1}", KinectEndpointBasePath, entry.Key),
                        new KinectRequestHandlerFactory(entry.Value, this.streamHandlerFactoriesSnapshot));
                }

                if (!string.IsNullOrEmpty(this.fileServerRootDirectory))
                {
                    factoryMap.Add(
                        this.fileServerBasePath,
                        new FileRequestHandlerFactory(this.fileServerRootDirectory));
                }

                var origins = new[] { this.OriginUri };
                this.listener = new ThreadHostedHttpListener(origins, this.allowedOriginsSnapshot, factoryMap);
                this.listener.Started += this.ListenerOnStarted;
                this.listener.Stopped += this.ListenerOnStopped;
                this.listener.Start();

                this.hasFileServerChanged = false;
                this.hasOriginChanged = false;
            }
            else if (!this.listener.IsListening)
            {
                this.listener.Start();
            }
        }

        /// <summary>
        /// Stop listening for requests.
        /// </summary>
        public void Stop()
        {
            if (this.listener == null)
            {
                return;
            }

            this.listener.Stop();
        }

        /// <summary>
        /// Determine if two sensor chooser maps are equivalent.
        /// </summary>
        /// <param name="left">
        /// Reference dictionary in comparison.
        /// </param>
        /// <param name="right">
        /// Other dictionary to compare against
        /// </param>
        /// <returns>
        /// True if left and right dictionaries are equivalent. False otherwise.
        /// </returns>
        private static bool SensorMapEquals(Dictionary<string, KinectSensorChooser> left, Dictionary<string, KinectSensorChooser> right)
        {
            if (left == right)
            {
                return true;
            }

            if ((left == null) || (right == null))
            {
                return false;
            }

            if (left.Count != right.Count)
            {
                return false;
            }

            foreach (var entry in left)
            {
                KinectSensorChooser rightValue;
                if (!right.TryGetValue(entry.Key, out rightValue))
                {
                    return false;
                }

                if (entry.Value == null)
                {
                    if (rightValue != null)
                    {
                        return false;
                    }

                    continue;
                }

                if (!entry.Value.Equals(rightValue))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Handler for ThreadHostedHttpListener.Started event.
        /// </summary>
        /// <param name="sender">
        /// Object that sent the event.
        /// </param>
        /// <param name="args">
        /// Event arguments.
        /// </param>
        private void ListenerOnStarted(object sender, EventArgs args)
        {
            if (this.Started != null)
            {
                this.Started(this, args);
            }
        }

        /// <summary>
        /// Handler for ThreadHostedHttpListener.Stopped event.
        /// </summary>
        /// <param name="sender">
        /// Object that sent the event.
        /// </param>
        /// <param name="args">
        /// Event arguments.
        /// </param>
        private void ListenerOnStopped(object sender, EventArgs args)
        {
            if (this.Stopped != null)
            {
                this.Stopped(this, args);
            }
        }
    }
}
