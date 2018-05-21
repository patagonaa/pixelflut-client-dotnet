using System.Collections.Generic;
using System.Drawing;

namespace PixelFlut.Infrastructure
{
    public interface IOutputService
    {
        void Output(byte[] rendered);
        Size GetSize();
    }
}