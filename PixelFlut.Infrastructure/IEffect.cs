using System.Drawing;

namespace PixelFlut.Infrastructure
{
    public interface IEffect
    {
        OutputPixel[] GetPixels();
        void Init(Size canvasSize);
    }
}