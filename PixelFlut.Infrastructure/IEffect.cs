using System;
using System.Drawing;
using System.Threading.Tasks;

namespace PixelFlut.Infrastructure
{
    public interface IEffect : IDisposable
    {
        Task<OutputFrame> GetPixels();
        Task Init(Size canvasSize);
    }
}