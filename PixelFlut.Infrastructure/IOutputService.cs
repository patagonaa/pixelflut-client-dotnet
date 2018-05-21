using System.Collections.Generic;
using System.Drawing;

namespace PixelFlut.Infrastructure
{
    public interface IOutputService
    {
        int Output(byte[] rendered);
        Size GetSize();
    }
}