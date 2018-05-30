using PixelFlut.Infrastructure;
using PixelFlut.Infrastructure.Effects.Image;
using Accord.Video.FFMPEG;
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace PixelFlut.Demo.Effects
{
    class VideoPlayback : DrawImageBase
    {
        private VideoFileReader reader;
        private int i;

        public VideoPlayback(string filePath)
        {
            this.reader = new VideoFileReader();
            this.reader.Open(filePath);
            this.i = 0;
        }

        protected override OutputFrame TickInternal()
        {
            if (i == this.reader.FrameCount)
            {
                i = 0;
            }

            using (var frame = this.reader.ReadVideoFrame(i++))
            {
                return new OutputFrame(0, 0, DrawImage(frame, Point.Empty), -1, true);
            }
        }

        public override void Dispose()
        {
            this.reader?.Close();
            this.reader?.Dispose();
            base.Dispose();
        }
    }
}
