using System.Linq;
using System.Drawing;
using PixelFlut.Infrastructure;
using System.Threading.Tasks;

namespace PixelFlut.Demo.Effects.Image
{
    public class DrawImageStatic : DrawImageBase
    {
        private readonly Bitmap _image;
        private readonly Point _pos;
        private OutputPixel[] _renderedImage;

        public DrawImageStatic(string filePath, Point p)
        {
            _image = GetImageData(filePath);
            _pos = p;
        }

        protected override Task<OutputFrame> TickInternal()
        {
            OutputPixel[] pixels = _renderedImage ?? (_renderedImage = DrawImage(_image, Point.Empty).ToArray());
            return Task.FromResult(new OutputFrame(_pos.X, _pos.Y, pixels, 0, true));
        }
    }
}