﻿using System;
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

        public OutputFrame GetPixels()
        {
            if (!this.initialized)
                throw new InvalidOperationException("not initialized!");
            return TickInternal();
        }

        protected abstract OutputFrame TickInternal();

        public virtual void Dispose()
        {
        }

        protected Size CanvasSize { get; private set; }

        private bool initialized;

        protected int Counter { get; private set; }
    }
}