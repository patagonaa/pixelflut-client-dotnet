using PixelFlut.Infrastructure;
using System.Drawing;
using System.Threading.Tasks;

namespace PixelFlut.Demo.Effects
{
    public class Void : EffectBase
    {
        protected override Task<OutputFrame> TickInternal()
        {
            var height = CanvasSize.Height;
            var width = CanvasSize.Width;
            var color = unchecked((uint)Color.Black.ToArgb());

            var toReturn = new OutputPixel[width * height];
            var i = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    toReturn[i++] = new OutputPixel(x, y, color);
                }
            }

            return Task.FromResult(new OutputFrame(0, 0, toReturn, 0, true));
        }
    }
}