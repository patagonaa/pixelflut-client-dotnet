using System;
using System.Collections.Generic;
using System.Drawing;

namespace PixelFlut.Infrastructure
{

    public interface IRenderService
    {
        void Init(Size canvasSize);
        ArraySegment<byte> PreRender(OutputFrame frame);
        IList<KeyValuePair<string, string>> GetDiagnostics();
    }
}