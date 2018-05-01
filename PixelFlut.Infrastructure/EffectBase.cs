using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace PixelFlut.Infrastructure
{
    public abstract class EffectBase : IEffect
    {
        public void Init(Size canvasSize)
        {
            CanvasSize = canvasSize;
            this.initialized = true;
        }

        public IReadOnlyCollection<OutputPixel> GetPixels()
        {
            if (!this.initialized)
                throw new InvalidOperationException("not initialized!");
            TickInternal();
            Counter++;
            var pixels = this.pixels;
            this.pixels = new List<OutputPixel>();
            return pixels;
        }

        protected abstract void TickInternal();

        private List<OutputPixel> pixels = new List<OutputPixel>();

        protected Size CanvasSize { get; private set; }

        private bool initialized;

        protected int Counter { get; private set; }
        protected void DrawPixel(Point point, Color color)
        {
            this.pixels.Add(new OutputPixel(point, color));
        }
    }
}