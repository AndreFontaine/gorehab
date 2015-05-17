//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.WebserverBasics
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Windows;

    using Microsoft.Kinect.Toolkit;
    using Microsoft.Samples.Kinect.Webserver;

    /// <summary>
    /// Interaction logic for MainWindow.
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly DependencyProperty IsStartedProperty = DependencyProperty.Register(
            "IsStarted", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public static readonly DependencyProperty ErrorTextProperty = DependencyProperty.Register(
            "ErrorText", typeof(string), typeof(MainWindow), new PropertyMetadata(string.Empty));

        /// <summary>
        /// Name (URI sub-path) to be used by clients to refer to the default kinect sensor.
        /// </summary>
        private const string DefaultSensorChooserName = "default";

        /// <summary>
        /// Name of main page for web sample.
        /// </summary>
        private const string SamplePageName = "GoRehab.html";

        /// <summary>
        /// Character used to separate origins in list of origins allowed to access this server.
        /// </summary>
        private const char AllowedOriginSeparator = ',';

        /// <summary>
        /// Component that manages finding a Kinect sensor
        /// </summary>
        private readonly KinectSensorChooser sensorChooser = new KinectSensorChooser();

        /// <summary>
        /// Webserver that handles requests from web clients.
        /// </summary>
        private readonly KinectWebserver webserver = new KinectWebserver();

        /// <summary>
        /// Error buffer listener used to update error text in UI.
        /// </summary>
        private readonly RecentEventBufferTraceListener errorBufferListener;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();

            if (!IsSupportedOsVersion(Environment.OSVersion))
            {
                MessageBox.Show(
                    Properties.Resources.SystemVersionNotSupportedMessage,
                    Properties.Resources.SystemVersionNotSupportedCaption,
                    MessageBoxButton.OK);
                Application.Current.Shutdown();
            }

            this.DataContext = this;

            this.errorBufferListener = Trace.Listeners["errorBufferListener"] as RecentEventBufferTraceListener;
            if (this.errorBufferListener != null)
            {
                this.errorBufferListener.RecentEventBufferChanged += this.ErrorBufferListenerOnChanged;
            }

            Uri originUri;
            try
            {
                originUri = new Uri(Properties.Settings.Default.OriginUri.Trim());
            }
            catch (UriFormatException)
            {
                Trace.TraceError("Invalid format for listening origin: \"{0}\"", Properties.Settings.Default.OriginUri);

                // If we can't listen on origin, we can't serve any data whatsover, so there is no sense in continuing
                // setup
                return;
            }

            this.webserver.SensorChooserMap.Add(DefaultSensorChooserName, this.sensorChooser);
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.FileRootDirectory))
            {
                this.webserver.FileServerRootDirectory = Properties.Settings.Default.FileRootDirectory;

                try
                {
                    this.RootDirectoryTextRun.Text = Path.GetFullPath(Properties.Settings.Default.FileRootDirectory);
                }
                catch (ArgumentException e)
                {
                    this.ShowNotServingFilesUi();
                    Trace.TraceError("Exception encountered while parsing root directory for serving files:\n{0}", e);
                }
            }
            else
            {
                this.ShowNotServingFilesUi();
            }

            this.webserver.OriginUri = originUri;
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.AccessControlAllowedOrigins))
            {
                foreach (var origin in Properties.Settings.Default.AccessControlAllowedOrigins.Split(AllowedOriginSeparator))
                {
                    try
                    {
                        originUri = new Uri(origin.Trim());
                    }
                    catch (UriFormatException)
                    {
                        Trace.TraceError("Invalid format for access control allowed origin: {0}", origin);
                        continue;
                    }

                    this.webserver.AccessControlAllowedOrigins.Add(originUri);
                }
            }

            //// TODO: Optionally add factories here for custom handlers:
            ////       this.webserver.SensorStreamHandlerFactories.Add(new MyCustomSensorStreamHandlerFactory());
            //// Your custom factory would implement ISensorStreamHandlerFactory, in which the
            //// CreateHandler method would return a class derived from SensorStreamHandlerBase
            //// which overrides one or more of its virtual methods.

            this.webserver.Started += (server, args) => this.IsStarted = true;
            this.webserver.Stopped += (server, args) => this.IsStarted = false;
        }

        /// <summary>
        /// Gets the KinectSensorChooser component
        /// </summary>
        public KinectSensorChooser KinectSensorChooser
        {
            get { return this.sensorChooser; }
        }

        /// <summary>
        /// true if server has started listening, false otherwise. 
        /// </summary>
        public bool IsStarted
        {
            get
            {
                return (bool)this.GetValue(IsStartedProperty);
            }

            set
            {
                this.SetValue(IsStartedProperty, value);
            }
        }

        /// <summary>
        /// Error text to display in UI.
        /// </summary>
        public string ErrorText
        {
            get
            {
                return (string)this.GetValue(ErrorTextProperty);
            }

            set
            {
                this.SetValue(ErrorTextProperty, value);
            }
        }

        /// <summary>
        /// Determine if running this application is supported in the specified operating
        /// system version.
        /// </summary>
        /// <param name="systemInfo">
        /// Operating system version information.
        /// </param>
        /// <returns>
        /// true if running this application is supported in the specified operating system.
        /// false otherwise.
        /// </returns>
        private static bool IsSupportedOsVersion(OperatingSystem systemInfo)
        {
            const int MinimumMajorVersion = 6;
            const int MinimumMinorVersion = 2;

            return (systemInfo.Version.Major > MinimumMajorVersion)
                   || ((systemInfo.Version.Major == MinimumMajorVersion) && (systemInfo.Version.Minor >= MinimumMinorVersion));
        }

        /// <summary>
        /// Start web server, reporting an error to trace listeners if exception is encountered.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Server application should never crash. Errors are reported in web server UI instead.")]
        private void StartWebserver()
        {
            try
            {
                this.webserver.Start();
            }
            catch (Exception e)
            {
                Trace.TraceError("Unable to start server due to exception:\n{0}", e);
            }
        }

        /// <summary>
        /// Stop web server, reporting an error to trace listeners if exception is encountered.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Server application should never crash. Errors are reported in web server UI instead.")]
        private void StopWebserver()
        {
            try
            {
                this.webserver.Stop();
            }
            catch (Exception e)
            {
                Trace.TraceError("Unable to stop server due to exception:\n{0}", e);
            }
        }

        /// <summary>
        /// Show UI appropriate for when we're not serving static files.
        /// </summary>
        private void ShowNotServingFilesUi()
        {
            this.ServingFilesText.Visibility = Visibility.Collapsed;
            this.OpenBrowserText.Visibility = Visibility.Collapsed;
            this.NotServingFilesText.Visibility = Visibility.Visible;
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            this.sensorChooser.Start();

            this.StartWebserver();
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.StopWebserver();
        }

        private void StartButtonOnClick(object sender, RoutedEventArgs e)
        {
            this.StartWebserver();
        }

        private void StopButtonOnClick(object sender, RoutedEventArgs e)
        {
            this.StopWebserver();
        }

        private void ErrorBufferListenerOnChanged(object sender, EventArgs e)
        {
            this.Dispatcher.BeginInvoke((Action)(() => this.ErrorText = this.errorBufferListener.RecentEventBuffer));
        }

        /// <summary>
        /// Event handler for UriLink's Click event.
        /// </summary>
        /// <param name="sender">
        /// Object triggering the event.
        /// </param>
        /// <param name="e">
        /// Event arguments.
        /// </param>
        private void UriLinkOnClick(object sender, RoutedEventArgs e)
        {
            // Shell execute the URI of sample page to open in default browser
            var samplePageUri = this.webserver.OriginUri.ConcatenateSegments(this.webserver.FileServerBasePath, SamplePageName);

            if (samplePageUri == null)
            {
                return;
            }

            var startInfo = new ProcessStartInfo(samplePageUri.ToString()) { UseShellExecute = true };
            using (var process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
            }
        }
    }
}