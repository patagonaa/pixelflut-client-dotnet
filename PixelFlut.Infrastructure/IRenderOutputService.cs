using System.Collections.Generic;
using System.Drawing;

namespace PixelFlut.Infrastructure
{
    public interface IRenderOutputService<TRendered>
    {
        TRendered PreRender(IReadOnlyCollection<OutputPixel> pixels);
        void Output(TRendered rendered);
        Size GetSize();
        IList<KeyValuePair<string, string>> GetDiagnostics();
    }
}