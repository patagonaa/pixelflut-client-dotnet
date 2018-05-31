#if NET461
using PixelFlut.Infrastructure;
using Accord.Video.FFMPEG;
using System.Drawing;
using PixelFlut.Demo.Effects.Image;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System;

namespace PixelFlut.Demo.Effects
{
    class VideoPlayback : DrawImageBase
    {
        private const int KeyFrameAfterFrames = 20;

        private VideoFileReader reader;
        private OutputPixel[] lastFrame;
        private int i;

        public VideoPlayback(string filePath)
        {
            this.reader = new VideoFileReader();
            this.reader.Open(filePath);

            this.i = 0;
        }

        protected override OutputFrame TickInternal()
        {
            Bitmap frame;
            while ((frame = this.reader.ReadVideoFrame(i++)) == null)
            {
                i = 0;
            }

            OutputFrame toReturn;
            using (frame)
            {
                var outputPixels = DrawImage(frame, Point.Empty);

                toReturn = new OutputFrame(0, 0, OptimizeBandwidth(outputPixels, ref lastFrame), -1, false);
            }
            //Thread.Sleep(1000);
            return toReturn;
        }

        private OutputPixel[] OptimizeBandwidth(OutputPixel[] outputPixels, ref OutputPixel[] lastFrame)
        {
            if (lastFrame == null)
            {
                lastFrame = outputPixels;
                return outputPixels;
            }

            var toReturn = new List<OutputPixel>(100000);

            var pixelLength = outputPixels.Length;
            for (int i = 0; i < pixelLength; i++)
            {
                var oldPixel = lastFrame[i];
                var newPixel = outputPixels[i];

                if (!(this.i % KeyFrameAfterFrames == 0) && ColorsEqual(oldPixel, newPixel))
                    continue;
                lastFrame[i] = newPixel;
                toReturn.Add(newPixel);
            }

            return toReturn.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ColorsEqual(OutputPixel oldPixel, OutputPixel newPixel)
        {
            //return (oldPixel.Color == newPixel.Color);

            var c1 = oldPixel.Color;
            var c2 = newPixel.Color;

            var diff = (c1 & 0xFF) - (c2 & 0xFF);
            if (diff > 10 || diff < -10)
                return false;
            c1 >>= 8;
            c2 >>= 8;

            diff = (c1 & 0xFF) - (c2 & 0xFF);
            if (diff > 10 || diff < -10)
                return false;
            c1 >>= 8;
            c2 >>= 8;

            diff = (c1 & 0xFF) - (c2 & 0xFF);
            if (diff > 10 || diff < -10)
                return false;

            return true;
        }

        public override void Dispose()
        {
            this.reader?.Close();
            this.reader?.Dispose();
            base.Dispose();
        }
    }
}
#endif