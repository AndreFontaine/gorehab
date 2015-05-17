// -----------------------------------------------------------------------
// <copyright file="BackgroundRemovalStreamMessage.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Webserver.Sensor.Serialization
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    using Microsoft.Kinect.Toolkit.BackgroundRemoval;

    /// <summary>
    /// Serializable representation of a background removed color stream message to send to client.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter",
        Justification = "Lower case names allowed for JSON serialization.")]
    public class BackgroundRemovalStreamMessage : ImageHeaderStreamMessage
    {
        /// <summary>
        /// Bytes per pixel const.
        /// </summary>
        private const int BytesPerPixel = 4;

        /// <summary>
        /// Tracking ID of the player currently being tracked. Pixels that do not belong
        /// to this player are removed.
        /// </summary>
        /// <remarks>
        /// This value will be 0 if no player is found in the corresponding color frame.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "tracked", Justification = "Lower case names allowed for JSON serialization.")]
        public int trackedPlayerId { get; set; }

        /// <summary>
        /// The average depth of the pixels corresponding to the foreground player.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "average", Justification = "Lower case names allowed for JSON serialization.")]
        public short averageDepth { get; set; }

        /// <summary>
        /// Buffer that holds background removed color image.
        /// </summary>
        internal byte[] Buffer { get; private set; }

        /// <summary>
        /// Update background removed color frame.
        /// </summary>
        /// <param name="frame">The input frame.</param>
        public void UpdateBackgroundRemovedColorFrame(BackgroundRemovedColorFrame frame)
        {
            if (frame == null)
            {
                throw new ArgumentNullException("frame");
            }

            this.timestamp = frame.Timestamp;
            this.width = frame.Width;
            this.height = frame.Height;
            this.bufferLength = frame.PixelDataLength;
            this.trackedPlayerId = frame.TrackedPlayerId;
            this.averageDepth = frame.AverageDepth;

            if ((this.Buffer == null) || (this.Buffer.Length != this.bufferLength))
            {
                this.Buffer = new byte[this.bufferLength];
            }

            unsafe
            {
                fixed (byte* messageDataPtr = this.Buffer)
                {
                    fixed (byte* frameDataPtr = frame.GetRawPixelData())
                    {
                        byte* messageDataPixelPtr = messageDataPtr;
                        byte* frameDataPixelPtr = frameDataPtr;

                        byte* messageDataPixelPtrEnd = messageDataPixelPtr + this.bufferLength;

                        while (messageDataPixelPtr != messageDataPixelPtrEnd)
                        {
                            // Convert from BGRA to RGBA format
                            *(messageDataPixelPtr++) = *(frameDataPixelPtr + 2);
                            *(messageDataPixelPtr++) = *(frameDataPixelPtr + 1);
                            *(messageDataPixelPtr++) = *frameDataPixelPtr;
                            *(messageDataPixelPtr++) = *(frameDataPixelPtr + 3);

                            frameDataPixelPtr += BytesPerPixel;
                        }
                    }
                }
            }
        }
    }
}
