using System;
using System.Drawing;

namespace PixelFlut.Infrastructure
{
    public interface IEffect : IDisposable
    {
        OutputFrame GetPixels();
        void Init(Size canvasSize);
    }
}