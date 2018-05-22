using System;
using System.Collections.Generic;
using System.Drawing;

namespace PixelFlut.Infrastructure
{
    public interface IOutputService
    {
        int Output(ArraySegment<byte> rendered);
        Size GetSize();
    }
}