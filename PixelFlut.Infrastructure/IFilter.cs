using System;
using System.Threading.Tasks;

namespace PixelFlut.Infrastructure
{
    public interface IFilter : IDisposable
    {
        Task<OutputFrame> ApplyFilter(OutputFrame frame);
    }
}
