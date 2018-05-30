#if NET461
using PixelFlut.Infrastructure;
using Accord.Video.FFMPEG;
using System.Drawing;
using PixelFlut.Demo.Effects.Image;

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
            Bitmap frame;
            while ((frame = this.reader.ReadVideoFrame(i++)) == null)
            {
                i = 0;
            }

            using (frame)
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
#endif