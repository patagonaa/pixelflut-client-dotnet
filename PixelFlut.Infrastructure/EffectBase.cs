using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace PixelFlut.Infrastructure
{
    public abstract class EffectBase : IEffect
    {
        public virtual void Init(Size canvasSize)
        {
            CanvasSize = canvasSize;
            this.initialized = true;
        }

        public IReadOnlyCollection<OutputPixel> GetPixels()
        {
            if (!this.initialized)
                throw new InvalidOperationException("not initialized!");
            return TickInternal().ToList();
        }

        protected abstract IEnumerable<OutputPixel> TickInternal();

        protected Size CanvasSize { get; private set; }

        private bool initialized;

        protected int Counter { get; private set; }
    }
}