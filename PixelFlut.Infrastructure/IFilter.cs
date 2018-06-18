using System;

namespace PixelFlut.Infrastructure
{
    public interface IFilter : IDisposable
    {
        OutputFrame ApplyFilter(OutputFrame frame);
    }
}
