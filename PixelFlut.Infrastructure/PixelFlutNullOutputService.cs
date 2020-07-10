using System;
using System.Drawing;

namespace PixelFlut.Infrastructure
{
    public class PixelFlutNullOutputService : IOutputService
    {
        private readonly Size size;

        public PixelFlutNullOutputService(Size size)
        {
            this.size = size;
        }

        public Size GetSize()
        {
            return this.size;
        }

        public int Output(ArraySegment<byte> rendered)
        {
            return rendered.Count;
        }
    }
}