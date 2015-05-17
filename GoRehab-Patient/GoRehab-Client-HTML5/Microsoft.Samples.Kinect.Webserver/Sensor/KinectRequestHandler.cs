// -----------------------------------------------------------------------
// <copyright file="KinectRequestHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver.Sensor
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Net;
    using System.Net.WebSockets;
    using System.Runtime.Serialization;
    using System.Threading.Tasks;

    using Microsoft.Kinect;
    using Microsoft.Kinect.Toolkit;
    using Microsoft.Samples.Kinect.Webserver;
    using Microsoft.Samples.Kinect.Webserver.Sensor.Serialization;

    /// <summary>
    /// Implementation of IHttpRequestHandler used to handle communication to/from
    /// a Kinect sensor represented by a specified KinectSensorChooser.
    /// </summary>
    public class KinectRequestHandler : IHttpRequestHandler
    {
        /// <summary>
        /// Property name used to return success status to client.
        /// </summary>
        public const string SuccessPropertyName = "success";

        /// <summary>
        /// Property name used to return a set of property names that encountered errors to client.
        /// </summary>
        public const string ErrorsPropertyName = "errors";

        /// <summary>
        /// Property name used to represent "enabled" property of a stream.
        /// </summary>
        public const string EnabledPropertyName = "enabled";

        /// <summary>
        /// Sub path for REST endpoint owned by this handler.
        /// </summary>
        public const string StateUriSubpath = "STATE";

        /// <summary>
        /// Sub path for stream data web-socket endpoint owned by this handler.
        /// </summary>
        public const string StreamUriSubpath = "STREAM";

        /// <summary>
        /// Sub path for stream data web-socket endpoint owned by this handler.
        /// </summary>
        public const string EventsUriSubpath = "EVENTS";

        /// <summary>
        /// MIME type name for JSON data.
        /// </summary>
        internal const string JsonContentType = "application/json";

        /// <summary>
        /// Reserved names that streams can't have.
        /// </summary>
        private static readonly HashSet<string> ReservedNames = new HashSet<string>
                                                                {
                                                                    SuccessPropertyName.ToUpperInvariant(),
                                                                    ErrorsPropertyName.ToUpperInvariant(),
                                                                    StreamUriSubpath.ToUpperInvariant(),
                                                                    EventsUriSubpath.ToUpperInvariant(),
                                                                    StateUriSubpath.ToUpperInvariant()
                                                                };

        /// <summary>
        /// Sensor chooser used to obtain a KinectSensor.
        /// </summary>
        private readonly KinectSensorChooser sensorChooser;

        /// <summary>
        /// Array of sensor stream handlers used to process kinect data and deliver
        /// a data streams ready for web consumption.
        /// </summary>
        private readonly ISensorStreamHandler[] streamHandlers;

        /// <summary>
        /// Map of stream handler names to sensor stream handler objects
        /// </summary>
        private readonly Dictionary<string, ISensorStreamHandler> streamHandlerMap = new Dictionary<string, ISensorStreamHandler>();

        /// <summary>
        /// Map of uri names (expected to be case-insensitive) corresponding to a stream, to
        /// their case-sensitive names used in JSON communications.
        /// </summary>
        private readonly Dictionary<string, string> uriName2StreamNameMap = new Dictionary<string, string>();

        /// <summary>
        /// Channel used to send event messages that are part of main data stream.
        /// </summary>
        private readonly List<WebSocketEventChannel> streamChannels = new List<WebSocketEventChannel>();

        /// <summary>
        /// Channel used to send event messages that are part of an events stream.
        /// </summary>
        private readonly List<WebSocketEventChannel> eventsChannels = new List<WebSocketEventChannel>();

        /// <summary>
        /// Intermediate storage for the skeleton data received from the Kinect sensor.
        /// </summary>
        private Skeleton[] skeletons;

        /// <summary>
        /// Kinect sensor currently associated with this request handler.
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Initializes a new instance of the KinectRequestHandler class.
        /// </summary>
        /// <param name="sensorChooser">
        /// Sensor chooser that will be used to obtain a KinectSensor.
        /// </param>
        /// <param name="streamHandlerFactories">
        /// Collection of stream handler factories to be used to process kinect data and deliver
        /// data streams ready for web consumption.
        /// </param>
        internal KinectRequestHandler(KinectSensorChooser sensorChooser, Collection<ISensorStreamHandlerFactory> streamHandlerFactories)
        {
            this.sensorChooser = sensorChooser;
            this.streamHandlers = new ISensorStreamHandler[streamHandlerFactories.Count];
            var streamHandlerContext = new SensorStreamHandlerContext(this.SendStreamMessageAsync, this.SendEventMessageAsync);

            var normalizedNameSet = new HashSet<string>();

            // Associate each of the supported stream names with the corresponding handlers 
            for (int i = 0; i < streamHandlerFactories.Count; ++i)
            {
                var handler = streamHandlerFactories[i].CreateHandler(streamHandlerContext);
                this.streamHandlers[i] = handler;
                var names = handler.GetSupportedStreamNames();

                foreach (var name in names)
                {
                    if (string.IsNullOrEmpty(name))
                    {
                        throw new InvalidOperationException(@"Empty stream names are not supported");
                    }

                    if (name.IndexOfAny(SharedConstants.UriPathComponentDelimiters) >= 0)
                    {
                        throw new InvalidOperationException(@"Stream names can't contain '/' character");
                    }

                    var normalizedName = name.ToUpperInvariant();

                    if (ReservedNames.Contains(normalizedName))
                    {
                        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "'{0}' is a reserved stream name", normalizedName));
                    }

                    if (normalizedNameSet.Contains(normalizedName))
                    {
                        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "'{0}' is a duplicate stream name", normalizedName));
                    }

                    normalizedNameSet.Add(normalizedName);

                    this.uriName2StreamNameMap.Add(normalizedName, name);
                    this.streamHandlerMap.Add(name, handler);
                }
            }
        }

        /// <summary>
        /// Prepares handler to start receiving HTTP requests.
        /// </summary>
        /// <returns>
        /// Await-able task.
        /// </returns>
        public Task InitializeAsync()
        {
            this.OnKinectChanged(this.sensorChooser.Kinect);
            this.sensorChooser.KinectChanged += this.SensorChooserKinectChanged;
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

            var splitPath = SplitUriSubpath(subpath);

            if (splitPath == null)
            {
                CloseResponse(requestContext, HttpStatusCode.NotFound);
                return;
            }

            var pathComponent = splitPath.Item1;

            try
            {
                switch (pathComponent)
                {
                case StateUriSubpath:
                    await this.HandleStateRequest(requestContext);
                    break;

                case StreamUriSubpath:
                    this.HandleStreamRequest(requestContext);
                    break;

                case EventsUriSubpath:
                    this.HandleEventRequest(requestContext);
                    break;

                default:
                    var remainingSubpath = splitPath.Item2;
                    if (remainingSubpath == null)
                    {
                        CloseResponse(requestContext, HttpStatusCode.NotFound);
                        return;
                    }

                    string streamName;
                    if (!this.uriName2StreamNameMap.TryGetValue(pathComponent, out streamName))
                    {
                        CloseResponse(requestContext, HttpStatusCode.NotFound);
                        return;
                    }

                    var streamHandler = this.streamHandlerMap[streamName];

                    await streamHandler.HandleRequestAsync(streamName, requestContext, remainingSubpath);
                    break;
                }
            }
            catch (Exception e)
            {
                // If there's any exception while handling a request, return appropriate
                // error status code rather than propagating exception further.
                Trace.TraceError("Exception encountered while handling Kinect sensor request:\n{0}", e);
                CloseResponse(requestContext, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Cancel all pending operations.
        /// </summary>
        public void Cancel()
        {
            foreach (var channel in this.streamChannels.SafeCopy())
            {
                channel.Cancel();
            }

            foreach (var channel in this.eventsChannels.SafeCopy())
            {
                channel.Cancel();
            }

            foreach (var streamHandler in this.streamHandlers)
            {
                streamHandler.Cancel();
            }
        }

        /// <summary>
        /// Lets handler know that no more HTTP requests will be received, so that it can
        /// clean up resources associated with request handling.
        /// </summary>
        /// <returns>
        /// Await-able task.
        /// </returns>
        public async Task UninitializeAsync()
        {
            foreach (var channel in this.streamChannels.SafeCopy())
            {
                await channel.CloseAsync();
            }

            foreach (var channel in this.eventsChannels.SafeCopy())
            {
                await channel.CloseAsync();
            }

            this.OnKinectChanged(null);

            foreach (var handler in this.streamHandlers)
            {
                await handler.UninitializeAsync();
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
        internal static void CloseResponse(HttpListenerContext context, HttpStatusCode statusCode)
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
        /// Splits a URI sub-path into "first path component" and "rest of sub-path".
        /// </summary>
        /// <param name="subpath">
        /// Uri sub-path. Expected to start with "/" character.
        /// </param>
        /// <returns>
        /// <para>
        /// A tuple containing two elements:
        /// 1) first sub-path component
        /// 2) rest of sub-path string
        /// </para>
        /// <para>
        /// May be null if sub-path is badly formed.
        /// </para>
        /// </returns>
        /// <remarks>
        /// The returned path components will have been normalized to uppercase.
        /// </remarks>
        internal static Tuple<string, string> SplitUriSubpath(string subpath)
        {
            if (!subpath.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            subpath = subpath.Substring(1).ToUpperInvariant();
            int delimiterIndex = subpath.IndexOfAny(SharedConstants.UriPathComponentDelimiters);
            var firstComponent = (delimiterIndex < 0) ? subpath : subpath.Substring(0, delimiterIndex);
            var remainingSubpath = (delimiterIndex < 0) ? null : subpath.Substring(delimiterIndex);

            return new Tuple<string, string>(firstComponent, remainingSubpath);
        }

        /// <summary>
        /// Add response headers that mean that response should not be cached.
        /// </summary>
        /// <param name="response">
        /// Http response object.
        /// </param>
        /// <remarks>
        /// This method needs to be called before starting to write the response output stream,
        /// or the headers meant to be added will be silently ignored, since they can't be sent
        /// after content is sent.
        /// </remarks>
        private static void AddNoCacheHeaders(HttpListenerResponse response)
        {
            response.Headers.Add("Cache-Control", "no-cache");
            response.Headers.Add("Cache-Control", "no-store");
            response.Headers.Add("Pragma", "no-cache");
            response.Headers.Add("Expires", "Mon, 1 Jan 1990 00:00:00 GMT");
        }

        /// <summary>
        /// Handle Http GET requests for state endpoint.
        /// </summary>
        /// <param name="requestContext">
        /// Context containing HTTP GET request data, and which will also contain associated
        /// response upon return.
        /// </param>
        /// <returns>
        /// Await-able task.
        /// </returns>
        private async Task HandleGetStateRequest(HttpListenerContext requestContext)
        {
            // Don't cache results of any endpoint requests
            AddNoCacheHeaders(requestContext.Response);
            
            var responseProperties = new Dictionary<string, object>();
            foreach (var mapEntry in this.streamHandlerMap)
            {
                var handlerStatus = mapEntry.Value.GetState(mapEntry.Key);
                responseProperties.Add(mapEntry.Key, handlerStatus);
            }

            requestContext.Response.ContentType = JsonContentType;
            await responseProperties.DictionaryToJsonAsync(requestContext.Response.OutputStream);
            CloseResponse(requestContext, HttpStatusCode.OK);
        }

        /// <summary>
        /// Handle Http POST requests for state endpoint.
        /// </summary>
        /// <param name="requestContext">
        /// Context containing HTTP POST request data, and which will also contain associated
        /// response upon return.
        /// </param>
        /// <returns>
        /// Await-able task.
        /// </returns>
        private async Task HandlePostStateRequest(HttpListenerContext requestContext)
        {
            // Don't cache results of any endpoint requests
            AddNoCacheHeaders(requestContext.Response);
            
            Dictionary<string, object> requestProperties;

            try
            {
                requestProperties = await requestContext.Request.InputStream.DictionaryFromJsonAsync();
            }
            catch (SerializationException)
            {
                requestProperties = null;
            }

            if (requestProperties == null)
            {
                CloseResponse(requestContext, HttpStatusCode.BadRequest);
                return;
            }

            var responseProperties = new Dictionary<string, object>();
            var errorStreamNames = new List<string>();

            foreach (var requestEntry in requestProperties)
            {
                ISensorStreamHandler handler;
                if (!this.streamHandlerMap.TryGetValue(requestEntry.Key, out handler))
                {
                    // Don't process unrecognized handlers
                    responseProperties.Add(requestEntry.Key, Properties.Resources.StreamNameUnrecognized);
                    errorStreamNames.Add(requestEntry.Key);
                    continue;
                }

                var propertiesToSet = requestEntry.Value as IDictionary<string, object>;
                if (propertiesToSet == null)
                {
                    continue;
                }

                var propertyErrors = new Dictionary<string, object>();
                var success = handler.SetState(requestEntry.Key, new ReadOnlyDictionary<string, object>(propertiesToSet), propertyErrors);
                if (!success)
                {
                    responseProperties.Add(requestEntry.Key, propertyErrors);
                    errorStreamNames.Add(requestEntry.Key);
                }
            }

            if (errorStreamNames.Count == 0)
            {
                responseProperties.Add(SuccessPropertyName, true);
            }
            else
            {
                // The only properties returned other than the "success" property are to indicate error,
                // so if there are other properties present it means that we've encountered at least
                // one error while trying to change state of the streams.
                responseProperties.Add(SuccessPropertyName, false);
                responseProperties.Add(ErrorsPropertyName, errorStreamNames.ToArray());
            }

            requestContext.Response.ContentType = JsonContentType;
            await responseProperties.DictionaryToJsonAsync(requestContext.Response.OutputStream);
            CloseResponse(requestContext, HttpStatusCode.OK);
        }

        /// <summary>
        /// Handle Http requests for state endpoint.
        /// </summary>
        /// <param name="requestContext">
        /// Context containing HTTP request data, and which will also contain associated
        /// response upon return.
        /// </param>
        /// <returns>
        /// Await-able task.
        /// </returns>
        private async Task HandleStateRequest(HttpListenerContext requestContext)
        {
            const string AllowHeader = "Allow";
            const string AllowedMethods = "GET, POST, OPTIONS";

            switch (requestContext.Request.HttpMethod)
            {
                case "GET":
                    await this.HandleGetStateRequest(requestContext);
                    break;

                case "POST":
                    await this.HandlePostStateRequest(requestContext);
                    break;

                case "OPTIONS":
                    requestContext.Response.Headers.Set(AllowHeader, AllowedMethods);
                    CloseResponse(requestContext, HttpStatusCode.OK);
                    break;

                default:
                    requestContext.Response.Headers.Set(AllowHeader, AllowedMethods);
                    CloseResponse(requestContext, HttpStatusCode.MethodNotAllowed);
                    break;
            }
        }

        /// <summary>
        /// Handle Http requests for stream endpoint.
        /// </summary>
        /// <param name="requestContext">
        /// Context containing HTTP request data, and which will also contain associated
        /// response upon return.
        /// </param>
        private void HandleStreamRequest(HttpListenerContext requestContext)
        {
            WebSocketEventChannel.TryOpenAsync(
                requestContext,
                channel => this.streamChannels.Add(channel),
                channel => this.streamChannels.Remove(channel));
        }

        /// <summary>
        /// Handle Http requests for event endpoint.
        /// </summary>
        /// <param name="requestContext">
        /// Context containing HTTP request data, and which will also contain associated
        /// response upon return.
        /// </param>
        private void HandleEventRequest(HttpListenerContext requestContext)
        {
            WebSocketEventChannel.TryOpenAsync(
                requestContext,
                channel => this.eventsChannels.Add(channel),
                channel => this.eventsChannels.Remove(channel));
        }

        /// <summary>
        /// Asynchronously send stream message to client(s) of stream handler.
        /// </summary>
        /// <param name="message">
        /// Stream message to send.
        /// </param>
        /// <param name="binaryPayload">
        /// Binary payload of stream message. May be null if message does not require a binary
        /// payload.
        /// </param>
        /// <returns>
        /// Await-able task.
        /// </returns>
        private async Task SendStreamMessageAsync(StreamMessage message, byte[] binaryPayload)
        {
            var webSocketMessage = message.ToTextMessage();

            foreach (var channel in this.streamChannels.SafeCopy())
            {
                if (channel == null)
                {
                    break;
                }

                if (binaryPayload != null)
                {
                    // If binary payload is non-null, send two-part stream message, with the first
                    // part acting as a message header.
                    await
                            channel.SendMessagesAsync(
                            webSocketMessage, new WebSocketMessage(new ArraySegment<byte>(binaryPayload), WebSocketMessageType.Binary));
                    continue;
                }

                await channel.SendMessagesAsync(webSocketMessage);
            }
        }

        /// <summary>
        /// Asynchronously send event message to client(s) of stream handler.
        /// </summary>
        /// <param name="message">
        /// Event message to send.
        /// </param>
        /// <returns>
        /// Await-able task.
        /// </returns>
        private async Task SendEventMessageAsync(EventMessage message)
        {
            foreach (var channel in this.eventsChannels.SafeCopy())
            {
                await channel.SendMessagesAsync(message.ToTextMessage());
            }
        }

        /// <summary>
        /// Responds to KinectSensor changes.
        /// </summary>
        /// <param name="newSensor">
        /// New sensor associated with this KinectRequestHandler.
        /// </param>
        private void OnKinectChanged(KinectSensor newSensor)
        {
            if (this.sensor != null)
            {
                try
                {
                    this.sensor.ColorFrameReady -= this.SensorColorFrameReady;
                    this.sensor.DepthFrameReady -= this.SensorDepthFrameReady;
                    this.sensor.SkeletonFrameReady -= this.SensorSkeletonFrameReady;

                    this.sensor.DepthStream.Range = DepthRange.Default;
                    this.sensor.SkeletonStream.EnableTrackingInNearRange = false;
                    this.sensor.DepthStream.Disable();
                    this.sensor.SkeletonStream.Disable();
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }

                this.sensor = null;
                this.skeletons = null;
            }

            if (newSensor != null)
            {
                // Allocate space to put the skeleton and interaction data we'll receive
                this.sensor = newSensor;

                try
                {
                    newSensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                    newSensor.SkeletonStream.Enable();

                    try
                    {
                        newSensor.DepthStream.Range = DepthRange.Near;
                        newSensor.SkeletonStream.EnableTrackingInNearRange = true;
                    }
                    catch (InvalidOperationException)
                    {
                        // Non Kinect for Windows devices do not support Near mode, so reset back to default mode.
                        newSensor.DepthStream.Range = DepthRange.Default;
                        newSensor.SkeletonStream.EnableTrackingInNearRange = false;
                    }

                    this.skeletons = new Skeleton[newSensor.SkeletonStream.FrameSkeletonArrayLength];

                    newSensor.ColorFrameReady += this.SensorColorFrameReady;
                    newSensor.DepthFrameReady += this.SensorDepthFrameReady;
                    newSensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }

            foreach (var handler in this.streamHandlers)
            {
                handler.OnSensorChanged(newSensor);
            }
        }

        /// <summary>
        /// Handler for KinectSensorChooser's KinectChanged event.
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="args">event arguments</param>
        private void SensorChooserKinectChanged(object sender, KinectChangedEventArgs args)
        {
            this.OnKinectChanged(args.NewSensor);
        }
        
        /// <summary>
        /// Handler for the Kinect sensor's ColorFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="colorImageFrameReadyEventArgs">event arguments</param>
        private void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs colorImageFrameReadyEventArgs)
        {
            // Even though we un-register all our event handlers when the sensor
            // changes, there may still be an event for the old sensor in the queue
            // due to the way the KinectSensor delivers events.  So check again here.
            if (this.sensor != sender)
            {
                return;
            }

            using (var colorFrame = colorImageFrameReadyEventArgs.OpenColorImageFrame())
            {
                if (null != colorFrame)
                {
                    try
                    {
                        // Hand data to each handler to be processed
                        foreach (var handler in this.streamHandlers)
                        {
                            handler.ProcessColor(colorFrame.GetRawPixelData(), colorFrame);
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // ColorFrame functions may throw when the sensor gets
                        // into a bad state.  Ignore the frame in that case.
                    }
                }
            }
        }

        /// <summary>
        /// Handler for the Kinect sensor's DepthFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="depthImageFrameReadyEventArgs">event arguments</param>
        private void SensorDepthFrameReady(object sender, DepthImageFrameReadyEventArgs depthImageFrameReadyEventArgs)
        {
            // Even though we un-register all our event handlers when the sensor
            // changes, there may still be an event for the old sensor in the queue
            // due to the way the KinectSensor delivers events.  So check again here.
            if (this.sensor != sender)
            {
                return;
            }

            using (var depthFrame = depthImageFrameReadyEventArgs.OpenDepthImageFrame())
            {
                if (null != depthFrame)
                {
                    var depthBuffer = depthFrame.GetRawPixelData();

                    try
                    {
                        // Hand data to each handler to be processed
                        foreach (var handler in this.streamHandlers)
                        {
                            handler.ProcessDepth(depthBuffer, depthFrame);
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // DepthFrame functions may throw when the sensor gets
                        // into a bad state.  Ignore the frame in that case.
                    }
                }
            }
        }

        /// <summary>
        /// Handler for the Kinect sensor's SkeletonFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="skeletonFrameReadyEventArgs">event arguments</param>
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs skeletonFrameReadyEventArgs)
        {
            // Even though we un-register all our event handlers when the sensor
            // changes, there may still be an event for the old sensor in the queue
            // due to the way the KinectSensor delivers events.  So check again here.
            if (this.sensor != sender)
            {
                return;
            }

            using (SkeletonFrame skeletonFrame = skeletonFrameReadyEventArgs.OpenSkeletonFrame())
            {
                if (null != skeletonFrame)
                {
                    try
                    {
                        // Copy the skeleton data from the frame to an array used for temporary storage
                        skeletonFrame.CopySkeletonDataTo(this.skeletons);

                        foreach (var handler in this.streamHandlers)
                        {
                            handler.ProcessSkeleton(this.skeletons, skeletonFrame);
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // SkeletonFrame functions may throw when the sensor gets
                        // into a bad state.  Ignore the frame in that case.
                    }
                }
            }
        }
    }

    /// <summary>
    /// Helper class that adds a SafeCopy extension method to a List of WebSocketEventChannel.
    /// </summary>
    internal static class ChannelListHelper
    {
        /// <summary>
        /// Safely makes a copy of the list of stream channels.
        /// </summary>
        /// <returns>Array containing the stream channels.</returns>
        public static IEnumerable<WebSocketEventChannel> SafeCopy(this List<WebSocketEventChannel> list)
        {
            var channels = new WebSocketEventChannel[list.Count];
            list.CopyTo(channels);
            return channels;
        }
    }
}
