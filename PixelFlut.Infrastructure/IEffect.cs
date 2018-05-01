using System.Collections.Generic;
using System.Drawing;

namespace PixelFlut.Infrastructure
{
    public interface IEffect
    {
        IReadOnlyCollection<OutputPixel> GetPixels();
        void Init(Size canvasSize);
    }
}