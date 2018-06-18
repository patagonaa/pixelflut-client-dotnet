#if NET461
using PixelFlut.Infrastructure;
using Accord.Video.FFMPEG;
using System.Drawing;
using PixelFlut.Demo.Effects.Image;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PixelFlut.Demo.Effects
{
    class VideoPlayback : DrawImageBase
    {
        private readonly VideoFileReader reader;
        private int i;
        private const int QueueLength = 4;
        private readonly Queue<Task<OutputPixel[]>> frameTaskQueue;

        public VideoPlayback(string filePath)
        {
            this.reader = new VideoFileReader();
            this.reader.Open(filePath);

            this.i = 0;
            this.frameTaskQueue = new Queue<Task<OutputPixel[]>>();
        }

        protected override OutputFrame TickInternal()
        {
            while (this.frameTaskQueue.Count < QueueLength)
            {
                Bitmap frame = GetFrame();
                this.frameTaskQueue.Enqueue(Task.Run(() => DrawImage(frame, Point.Empty)));
            }

            var outputPixels = frameTaskQueue.Dequeue().Result;
            return new OutputFrame(0, 0, outputPixels, -1, false);
        }

        private Bitmap GetFrame()
        {
            Bitmap frame;
            while ((frame = this.reader.ReadVideoFrame(i++)) == null)
            {
                i = 0;
            }

            return frame;
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