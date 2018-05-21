using System.Collections.Generic;

namespace PixelFlut.Infrastructure
{

    public interface IRenderService
    {
        byte[] PreRender(IReadOnlyCollection<OutputPixel> pixels);
        IList<KeyValuePair<string, string>> GetDiagnostics();
    }
}