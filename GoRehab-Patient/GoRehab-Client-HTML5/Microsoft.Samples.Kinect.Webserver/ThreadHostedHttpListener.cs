// -----------------------------------------------------------------------
// <copyright file="ThreadHostedHttpListener.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using System.Threading;
    using System.Windows.Threading;

    using Microsoft.Kinect.Toolkit;

    /// <summary>
    /// HTTP request/response handler that listens for and processes HTTP requests in
    /// a thread dedicated to that single purpose.
    /// </summary>
    public sealed class ThreadHostedHttpListener
    {
        /// <summary>
        /// URI origins for which server is expected to be listening.
        /// </summary>
        private readonly List<Uri> ownedOriginUris = new List<Uri>();

        /// <summary>
        /// Origin Uris that are allowed to access data served by this listener.
        /// </summary>
        private readonly HashSet<Uri> allowedOriginUris = new HashSet<Uri>();

        /// <summary>
        /// Mapping between URI paths and factories of request handlers that can correspond
        /// to them.
        /// </summary>
        private readonly Dictionary<string, IHttpRequestHandlerFactory> requestHandlerFactoryMap;

        /// <summary>
        /// SynchronizationContext wrapper used to track event handlers for Started event.
        /// </summary>
        private readonly ContextEventWrapper<EventArgs> startedContextWrapper =
            new ContextEventWrapper<EventArgs>(ContextSynchronizationMethod.Post);

        /// <summary>
        /// SynchronizationContext wrapper used to track event handlers for Stopped event.
        /// </summary>
        private readonly ContextEventWrapper<EventArgs> stoppedContextWrapper =
            new ContextEventWrapper<EventArgs>(ContextSynchronizationMethod.Post);

        /// <summary>
        /// Object used to synchronize access to data shared between client calling thread(s)
        /// and listener thread.
        /// </summary>
        private readonly object lockObject = new object();

        /// <summary>
        /// Data shared between client calling thread(s) and listener thread.
        /// </summary>
        private SharedThreadData threadData;

        /// <summary>
        /// Initializes a new instance of the ThreadHostedHttpListener class.
        /// </summary>
        /// <param name="ownedOrigins">
        /// URI origins for which server is expected to be listening.
        /// </param>
        /// <param name="allowedOrigins">
        /// Origin Uris that are allowed to access data served by this listener, in addition
        /// to owned origin Uris. May be empty or null if only owned origins are allowed to
        /// access server data.
        /// </param>
        /// <param name="requestHandlerFactoryMap">
        /// Mapping between URI paths and factories of request handlers that can correspond
        /// to them.
        /// </param>
        public ThreadHostedHttpListener(IEnumerable<Uri> ownedOrigins, IEnumerable<Uri> allowedOrigins, Dictionary<string, IHttpRequestHandlerFactory> requestHandlerFactoryMap)
        {
            if (ownedOrigins == null)
            {
                throw new ArgumentNullException("ownedOrigins");
            }

            if (requestHandlerFactoryMap == null)
            {
                throw new ArgumentNullException("requestHandlerFactoryMap");
            }

            this.requestHandlerFactoryMap = requestHandlerFactoryMap;

            foreach (var origin in ownedOrigins)
            {
                this.ownedOriginUris.Add(origin);

                this.allowedOriginUris.Add(origin);
            }

            if (allowedOrigins != null)
            {
                foreach (var origin in allowedOrigins)
                {
                    this.allowedOriginUris.Add(origin);
                }
            }
        }

        /// <summary>
        /// Event used to signal that the server has started listening for connections.
        /// </summary>
        public event EventHandler<EventArgs> Started
        {
            add { this.startedContextWrapper.AddHandler(value); }

            remove { this.startedContextWrapper.RemoveHandler(value); }
        }

        /// <summary>
        /// Event used to signal that the server has stopped listening for connections.
        /// </summary>
        public event EventHandler<EventArgs> Stopped
        {
            add { this.stoppedContextWrapper.AddHandler(value); }

            remove { this.stoppedContextWrapper.RemoveHandler(value); }
        }

        /// <summary>
        /// True if listener has a thread actively listening for HTTP requests.
        /// </summary>
        public bool IsListening
        {
            get
            {
                return (this.threadData != null) && this.threadData.Thread.IsAlive;
            }
        }

        /// <summary>
        /// Start listening for requests.
        /// </summary>
        public void Start()
        {
            lock (this.lockObject)
            {
                Thread oldThread = null;

                // If thread is currently running
                if (this.IsListening)
                {
                    if (!this.threadData.StopRequestSent)
                    {
                        // Thread is already running and ready to handle requests, so there
                        // is no need to start up a new one.
                        return;
                    }

                    // If thread is running, but still in the process of winding down,
                    // dissociate server from currently running thread without waiting for
                    // thread to finish.
                    // New thread will wait for previous thread to finish so that there is
                    // no conflict between two different threads listening on the same URI
                    // prefixes.
                    oldThread = this.threadData.Thread;
                    this.threadData = null;
                }

                this.threadData = new SharedThreadData
                {
                    Thread = new Thread(this.ListenerThread),
                    PreviousThread = oldThread
                };
                this.threadData.Thread.Start(this.threadData);
            }
        }

        /// <summary>
        /// Stop listening for requests, optionally waiting for thread to finish.
        /// </summary>
        /// <param name="wait">
        /// True if caller wants to wait until listener thread has terminated before
        /// returning.
        /// False otherwise.
        /// </param>
        public void Stop(bool wait)
        {
            Thread listenerThread = null;

            lock (this.lockObject)
            {
                if (this.IsListening)
                {
                    if (!this.threadData.StopRequestSent)
                    {
                        // Request the thread to end, but keep remembering the old thread
                        // data in case listener gets re-started immediately and new thread
                        // has to wait for old thread to finish before getting started.
                        SendStopMessage(this.threadData);
                    }

                    if (wait)
                    {
                        listenerThread = this.threadData.Thread;
                    }
                }
            }

            if (listenerThread != null)
            {
                listenerThread.Join();
            }
        }

        /// <summary>
        /// Stop listening for requests but don't wait for thread to finish.
        /// </summary>
        public void Stop()
        {
            this.Stop(false);
        }

        /// <summary>
        /// Let listener thread know that it should wind down and stop listening for incoming
        /// HTTP requests.
        /// </summary>
        /// <param name="threadData">
        /// Object containing shared data used to communicate with listener thread.
        /// </param>
        private static void SendStopMessage(SharedThreadData threadData)
        {
            if (threadData.StateManager != null)
            {
                // If there's already a state manager associated with the server thread,
                // tell it to stop listening.
                threadData.StateManager.Stop();
            }
            
            // Even if there's no dispatcher associated with the server thread yet,
            // let thread know that it should bail out immediately after starting
            // up, and never push a dispatcher frame at all.
            threadData.StopRequestSent = true;
        }

        /// <summary>
        /// Thread procedure for HTTP listener thread.
        /// </summary>
        /// <param name="data">
        /// Object containing shared data used to communicate between client thread
        /// and listener thread.
        /// </param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Main listener thread should never crash. Errors are captured in web server trace intead.")]
        private void ListenerThread(object data)
        {
            var sharedData = (SharedThreadData)data;
            
            lock (this.lockObject)
            {
                if (sharedData.StopRequestSent)
                {
                    return;
                }

                sharedData.StateManager = new ListenerThreadStateManager(this.ownedOriginUris, this.allowedOriginUris, this.requestHandlerFactoryMap);
            }

            try
            {
                if (sharedData.PreviousThread != null)
                {
                    // Wait for the previous thread to finish so that only one thread can be listening
                    // on the specified prefixes at any one time.
                    sharedData.PreviousThread.Join();
                    sharedData.PreviousThread = null;
                }

                //// After this point, it is expected that the only mutation of shared data triggered by
                //// another thread will be to signal that this thread should stop listening.

                this.startedContextWrapper.Invoke(this, EventArgs.Empty);

                sharedData.StateManager.Listen();
            }
            catch (Exception e)
            {
                Trace.TraceError("Exception encountered while listening for connections:\n{0}", e);
            }
            finally
            {
                sharedData.StateManager.Dispose();

                this.stoppedContextWrapper.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Represents state that needs to be shared between listener thread and client calling thread.
        /// </summary>
        private sealed class SharedThreadData
        {
            /// <summary>
            /// Listener thread.
            /// </summary>
            public Thread Thread { get; set; }

            /// <summary>
            /// Object used to manage state and safely communicate with listener thread.
            /// </summary>
            public ListenerThreadStateManager StateManager { get; set; }

            /// <summary>
            /// True if the specific listener thread associated with this shared data
            /// has previously been requested to stop.
            /// False otherwise.
            /// </summary>
            public bool StopRequestSent { get; set; }
            
            /// <summary>
            /// Previous listener thread, which had not fully finished processing before
            /// a new thread was requested. Most of the time this will be null.
            /// </summary>
            public Thread PreviousThread { get; set; }
        }

        /// <summary>
        /// Manages state used by a single listener thread.
        /// </summary>
        private sealed class ListenerThreadStateManager : IDisposable
        {
            /// <summary>
            /// URI origins for which server is expected to be listening.
            /// </summary>
            private readonly List<Uri> ownedOriginUris;

            /// <summary>
            /// Origin Uris that are allowed to access data served by this listener.
            /// </summary>
            private readonly HashSet<Uri> allowedOriginUris;

            /// <summary>
            /// HttpListener used to wait for incoming HTTP requests.
            /// </summary>
            private readonly HttpListener listener = new HttpListener();

            /// <summary>
            /// Mapping between URI paths and factories of request handlers that can correspond
            /// to them.
            /// </summary>
            private readonly Dictionary<string, IHttpRequestHandlerFactory> requestHandlerFactoryMap = new Dictionary<string, IHttpRequestHandlerFactory>();

            /// <summary>
            /// Mapping between URI paths and corresponding request handlers.
            /// </summary>
            private readonly Dictionary<string, IHttpRequestHandler> requestHandlerMap = new Dictionary<string, IHttpRequestHandler>();

            /// <summary>
            /// Dispatcher used to manage the queue of work done in listener thread. 
            /// </summary>
            private readonly Dispatcher dispatcher;

            /// <summary>
            /// Represents main execution loop in listener thread.
            /// </summary>
            private readonly DispatcherFrame frame;

            /// <summary>
            /// Asynchronous result indicating that we're currently waiting for some HTTP
            /// client to initiate a request.
            /// </summary>
            private IAsyncResult getContextResult;

            /// <summary>
            /// Initializes a new instance of the ListenerThreadStateManager class.
            /// </summary>
            /// <param name="ownedOriginUris">
            /// URI origins for which server is expected to be listening.
            /// </param>
            /// <param name="allowedOriginUris">
            /// URI origins that are allowed to access data served by this listener.
            /// </param>
            /// <param name="requestHandlerFactoryMap">
            /// Mapping between URI paths and factories of request handlers that can correspond
            /// to them.
            /// </param>
            internal ListenerThreadStateManager(List<Uri> ownedOriginUris, HashSet<Uri> allowedOriginUris, Dictionary<string, IHttpRequestHandlerFactory> requestHandlerFactoryMap)
            {
                this.ownedOriginUris = ownedOriginUris;
                this.allowedOriginUris = allowedOriginUris;
                this.dispatcher = Dispatcher.CurrentDispatcher;
                this.frame = new DispatcherFrame { Continue = true };
                this.requestHandlerFactoryMap = requestHandlerFactoryMap;
            }

            /// <summary>
            /// Releases resources used while listening for HTTP requests.
            /// </summary>
            public void Dispose()
            {
                this.listener.Close();
            }

            internal void Stop()
            {
                this.dispatcher.BeginInvoke((Action)(() =>
                {
                    foreach (var handler in this.requestHandlerMap)
                    {
                        handler.Value.Cancel();
                    }

                    this.frame.Continue = false;
                }));
            }
            
            /// <summary>
            /// Initializes request handlers, listens for incoming HTTP requests until client
            /// requests us to stop and then uninitializes request handlers.
            /// </summary>
            internal void Listen()
            {
                foreach (var entry in this.requestHandlerFactoryMap)
                {
                    var path = entry.Key;

                    // To simplify lookup against "PathAndQuery" property of Uri objects,
                    // we ensure that this has the starting forward slash that PathAndQuery
                    // property values have.
                    if (!path.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                    {
                        path = "/" + path;
                    }

                    // Listen for each handler path under each origin
                    foreach (var originUri in this.ownedOriginUris)
                    {
                        // HttpListener only listens to URIs that end in "/", but also remember
                        // path exactly as requested by client associated with request handler,
                        // to match subpath expressions expected by handler
                        var uriBuilder = new UriBuilder(originUri) { Path = path };
                        var prefix = uriBuilder.ToString();
                        if (!prefix.EndsWith("/", StringComparison.OrdinalIgnoreCase))
                        {
                            prefix = prefix + "/";
                        }

                        listener.Prefixes.Add(prefix);
                    }

                    requestHandlerMap.Add(path, entry.Value.CreateHandler());
                }
            
                this.listener.Start();
                try
                {
                    var initialize = (Action)(async () =>
                        {
                            foreach (var handler in requestHandlerMap.Values)
                            {
                                await handler.InitializeAsync();
                            }

                            this.getContextResult = this.listener.BeginGetContext(this.GetContextCallback, null);
                        });
                    this.dispatcher.BeginInvoke(DispatcherPriority.Normal, initialize);
                    Dispatcher.PushFrame(this.frame);

                    var uninitializeFrame = new DispatcherFrame { Continue = true };
                    var uninitialize = (Action)(async () =>
                        {
                            foreach (var handler in this.requestHandlerMap.Values)
                            {
                                await handler.UninitializeAsync();
                            }

                            uninitializeFrame.Continue = false;
                        });
                    this.dispatcher.BeginInvoke(DispatcherPriority.Normal, uninitialize);
                    Dispatcher.PushFrame(uninitializeFrame);
                }
                finally
                {
                    this.listener.Stop();
                }
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
            private static void CloseResponse(HttpListenerContext context, HttpStatusCode statusCode)
            {
                try
                {
                    context.Response.StatusCode = (int)statusCode;
                    context.Response.Close();
                }
                catch (HttpListenerException e)
                {
                    Trace.TraceWarning(
                        "Problem encountered while sending response for kinect sensor request. Client might have aborted request. Cause: \"{0}\"", e.Message);
                }
            }

            /// <summary>
            /// Checks if this is corresponds to a cross-origin request and, if so, prepares
            /// the response with the appropriate headers or even body, if necessary.
            /// </summary>
            /// <param name="context">
            /// Listener context containing the request and associated response object.
            /// </param>
            /// <returns>
            /// True if request was initiated by an explicitly allowed origin URI.
            /// False if origin URI for request is not allowed.
            /// </returns>
            private bool HandleCrossOrigin(HttpListenerContext context)
            {
                const string OriginHeader = "Origin";
                const string AllowOriginHeader = "Access-Control-Allow-Origin";
                const string RequestHeadersHeader = "Access-Control-Request-Headers";
                const string AllowHeadersHeader = "Access-Control-Allow-Headers";
                const string RequestMethodHeader = "Access-Control-Request-Method";
                const string AllowMethodHeader = "Access-Control-Allow-Methods";

                // Origin header is not required, since it is up to browser to
                // detect when cross-origin security checks are needed.
                var originValue = context.Request.Headers[OriginHeader];
                if (originValue != null)
                {
                    // If origin header is present, check if it's in allowed list
                    Uri originUri;
                    try
                    {
                        originUri = new Uri(originValue);
                    }
                    catch (UriFormatException)
                    {
                        return false;
                    }

                    if (!this.allowedOriginUris.Contains(originUri))
                    {
                        return false;
                    }

                    // We allow all origins to access this server's data
                    context.Response.Headers.Add(AllowOriginHeader, originValue);
                }

                var requestHeaders = context.Request.Headers[RequestHeadersHeader];
                if (requestHeaders != null)
                {
                    // We allow all headers in cross-origin server requests
                    context.Response.Headers.Add(AllowHeadersHeader, requestHeaders);
                }

                var requestMethod = context.Request.Headers[RequestMethodHeader];
                if (requestMethod != null)
                {
                    // We allow all methods in cross-origin server requests
                    context.Response.Headers.Add(AllowMethodHeader, requestMethod);
                }

                return true;
            }

            /// <summary>
            /// Callback used by listener to let us know when we have received an HTTP
            /// context corresponding to an earlier call to BeginGetContext.
            /// </summary>
            /// <param name="result">
            /// Status of asynchronous operation.
            /// </param>
            private void GetContextCallback(IAsyncResult result)
            {
                this.dispatcher.BeginInvoke((Action)(() =>
                    {
                        if (!this.listener.IsListening)
                        {
                            return;
                        }

                        Debug.Assert(result == this.getContextResult, "remembered GetContext result should match result handed to callback.");

                        var httpListenerContext = this.listener.EndGetContext(result);
                        this.getContextResult = null;

                        this.HandleRequestAsync(httpListenerContext);

                        if (this.frame.Continue)
                        {
                            this.getContextResult = this.listener.BeginGetContext(this.GetContextCallback, null);
                        }
                    }));
            }

            /// <summary>
            /// Handle an HTTP request asynchronously
            /// </summary>
            /// <param name="httpListenerContext">
            /// Context containing HTTP request and response information
            /// </param>
            private async void HandleRequestAsync(HttpListenerContext httpListenerContext)
            {
                var uri = httpListenerContext.Request.Url;
                var clientAddress = httpListenerContext.Request.RemoteEndPoint != null
                                        ? httpListenerContext.Request.RemoteEndPoint.Address
                                        : IPAddress.None;
                var requestOverview = string.Format("URI=\"{0}\", client=\"{1}\"", uri, clientAddress);

                try
                {
                    bool foundHandler = false;

                    if (!this.HandleCrossOrigin(httpListenerContext))
                    {
                        CloseResponse(httpListenerContext, HttpStatusCode.Forbidden);
                    }
                    else
                    {
                        foreach (var entry in this.requestHandlerMap)
                        {
                            if (uri.PathAndQuery.StartsWith(entry.Key, StringComparison.InvariantCultureIgnoreCase))
                            {
                                foundHandler = true;
                                var subPath = uri.PathAndQuery.Substring(entry.Key.Length);
                                await entry.Value.HandleRequestAsync(httpListenerContext, subPath);

                                break;
                            }
                        }

                        if (!foundHandler)
                        {
                            CloseResponse(httpListenerContext, HttpStatusCode.NotFound);
                        }
                    }

                    Trace.TraceInformation("Request for {0} completed with result: {1}", requestOverview, httpListenerContext.Response.StatusCode);
                }
                catch (Exception e)
                {
                    Trace.TraceError("Exception encountered while handling request for {0}:\n{1}", requestOverview, e);
                    CloseResponse(httpListenerContext, HttpStatusCode.InternalServerError);
                }
            }
        }
    }
}
