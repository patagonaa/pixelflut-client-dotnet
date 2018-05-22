using System;
using System.Collections.Generic;

namespace PixelFlut.Infrastructure
{

    public interface IRenderService
    {
        ArraySegment<byte> PreRender(OutputPixel[] pixels);
        IList<KeyValuePair<string, string>> GetDiagnostics();
    }
}