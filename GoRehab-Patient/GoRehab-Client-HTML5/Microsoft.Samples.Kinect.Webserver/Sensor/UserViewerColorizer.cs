// -----------------------------------------------------------------------
// <copyright file="UserViewerColorizer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver.Sensor
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;

    using Microsoft.Kinect;
    using Microsoft.Kinect.Toolkit.Interaction;

    /// <summary>
    /// Colorizes a depth image according to desired size and color mapping.
    /// </summary>
    internal class UserViewerColorizer
    {
        /// <summary>
        /// Number of bytes per pixel in colorized image.
        /// </summary>
        private const int BytesPerPixel = 4;

        /// <summary>
        /// Background color is always transparent.
        /// </summary>
        private const int BackgroundColor = 0x00000000;

        /// <summary>
        /// Lookup table mapping player indexes (observable in depth image pixels) to 32-bit
        /// ARGB color.
        /// </summary>
        private readonly int[] playerColorLookupTable;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserViewerColorizer"/> class.
        /// </summary>
        /// <param name="width">
        /// Desired width of colorized image.
        /// </param>
        /// <param name="height">
        /// Desired height of colorized image.
        /// </param>
        public UserViewerColorizer(int width, int height)
        {
            this.playerColorLookupTable = new int[SharedConstants.MaxUsersTracked + 1];

            this.SetResolution(width, height);
        }

        /// <summary>
        /// Desired width of colorized image.
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// Desired height of colorized image.
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// Buffer that holds colorized image.
        /// </summary>
        public byte[] Buffer { get; private set; }

        /// <summary>
        /// Updates the desired resolution for user viewer image.
        /// </summary>
        /// <param name="width">
        /// Desired width of colorized image.
        /// </param>
        /// <param name="height">
        /// Desired height of colorized image.
        /// </param>
        public void SetResolution(int width, int height)
        {
            if ((this.Buffer == null) || (this.Width != width) || (this.Height != height))
            {
                this.Width = width;
                this.Height = height;

                this.Buffer = new byte[width * height * BytesPerPixel];
            }
        }

        /// <summary>
        /// Color the user viewer image pixels appropriately given the specified depth image.
        /// </summary>
        /// <param name="depthImagePixels">
        /// Depth image from which we will colorize user viewer image.
        /// </param>
        /// <param name="depthWidth">
        /// Width of depth image, in pixels.
        /// </param>
        /// <param name="depthHeight">
        /// Height of depth image, in pixels.
        /// </param>
        public void ColorizeDepthPixels(DepthImagePixel[] depthImagePixels, int depthWidth, int depthHeight)
        {
            if (depthImagePixels == null)
            {
                throw new ArgumentNullException("depthImagePixels");
            }

            if (depthWidth <= 0)
            {
                throw new ArgumentException(@"Width of depth image must be greater than zero", "depthWidth");
            }

            if (depthWidth % this.Width != 0)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Depth image width '{0}' is not a multiple of the desired user viewer image width '{1}'",
                        depthWidth,
                        this.Width),
                    "depthWidth");
            }

            if (depthHeight <= 0)
            {
                throw new ArgumentException(@"Height of depth image must be greater than zero", "depthHeight");
            }

            if (depthHeight % this.Height != 0)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Depth image height '{0}' is not a multiple of the desired user viewer image height '{1}'",
                        depthHeight,
                        this.Height),
                    "depthHeight");
            }

            int downscaleFactor = depthWidth / this.Width;
            Debug.Assert(depthHeight / this.Height == downscaleFactor, "Downscale factor in x and y dimensions should be exactly the same.");

            int pixelDisplacementBetweenRows = depthWidth * downscaleFactor;

            unsafe
            {
                fixed (byte* colorBufferPtr = this.Buffer)
                {
                    fixed (DepthImagePixel* depthImagePixelPtr = depthImagePixels)
                    {
                        fixed (int* playerColorLookupPtr = this.playerColorLookupTable)
                        {
                            // Write color values using int pointers instead of byte pointers,
                            // since each color pixel is 32-bits wide.
                            int* colorBufferIntPtr = (int*)colorBufferPtr;
                            DepthImagePixel* currentPixelRowPtr = depthImagePixelPtr;

                            for (int row = 0; row < depthHeight; row += downscaleFactor)
                            {
                                DepthImagePixel* currentPixelPtr = currentPixelRowPtr;
                                for (int column = 0; column < depthWidth; column += downscaleFactor)
                                {
                                    *colorBufferIntPtr++ = playerColorLookupPtr[currentPixelPtr->PlayerIndex];
                                    currentPixelPtr += downscaleFactor;
                                }

                                currentPixelRowPtr += pixelDisplacementBetweenRows;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Reset the lookup table mapping user indexes (observable in depth image pixels)
        /// to 32-bit ARGB color to map all indexes to background color.
        /// </summary>
        public void ResetColorLookupTable()
        {
            // Initialize all player indexes to background color
            for (int entryIndex = 0; entryIndex < this.playerColorLookupTable.Length; ++entryIndex)
            {
                this.playerColorLookupTable[entryIndex] = BackgroundColor;
            }
        }

        /// <summary>
        /// Update the lookup table mapping user indexes (observable in depth image pixels)
        /// to 32-bit ARGB color.
        /// </summary>
        /// <param name="userInfos">
        /// User information obtained from an <see cref="InteractionFrame"/>.
        /// </param>
        /// <param name="defaultUserColor">
        /// 32-bit ARGB representation of default color assigned to users.
        /// </param>
        /// <param name="userStates">
        /// Mapping between user tracking IDs and user states (obtained from
        /// <see cref="IUserStateManager"/>).
        /// </param>
        /// <param name="userColors">
        /// Mapping between user states and 32-bit ARGB color.
        /// </param>
        public void UpdateColorLookupTable(UserInfo[] userInfos, int defaultUserColor, IDictionary<int, string> userStates, IDictionary<string, int> userColors)
        {
            if ((userInfos == null) || (userStates == null) || (userColors == null))
            {
                this.ResetColorLookupTable();
                return;
            }

            // Reset lookup table to have all player indexes map to default user color
            for (int i = 1; i < this.playerColorLookupTable.Length; i++)
            {
                this.playerColorLookupTable[i] = defaultUserColor;
            }

            // Iterate through user tracking Ids to populate color table.
            for (int i = 0; i < userInfos.Length; ++i)
            {
                // Player indexes in depth image are shifted by one in order to be able to
                // use zero as a marker to mean "pixel does not correspond to any player".
                int depthPlayerIndex = i + 1;
                var trackingId = userInfos[i].SkeletonTrackingId;

                string state;
                if (userStates.TryGetValue(trackingId, out state))
                {
                    int color;
                    if (userColors.TryGetValue(state, out color))
                    {
                        this.playerColorLookupTable[depthPlayerIndex] = color;
                    }
                }
            }
        }
    }
}
