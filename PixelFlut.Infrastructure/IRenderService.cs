using System;
using System.Collections.Generic;

namespace PixelFlut.Infrastructure
{

    public interface IRenderService
    {
        ArraySegment<byte> PreRender(OutputFrame frame);
        IList<KeyValuePair<string, string>> GetDiagnostics();
    }
}