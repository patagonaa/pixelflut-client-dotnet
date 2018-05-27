using System.Drawing;

namespace PixelFlut.Infrastructure
{
    public interface IEffect
    {
        OutputFrame GetPixels();
        void Init(Size canvasSize);
    }
}