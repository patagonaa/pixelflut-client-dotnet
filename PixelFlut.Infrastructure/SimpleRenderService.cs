using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace PixelFlut.Infrastructure
{
    public class SimpleRenderService : IRenderService, IDisposable
    {
        public void Dispose()
        {
        }

        public IList<KeyValuePair<string, string>> GetDiagnostics()
        {
            throw new NotImplementedException();
        }

        public ArraySegment<byte> PreRender(OutputFrame frame)
        {
            using (var ms = new MemoryStream())
            {
                using (var sw = new StreamWriter(ms))
                {
                    sw.NewLine = "\n";
                    foreach (var pixel in frame.Pixels)
                    {
                        var color = Color.FromArgb(pixel.Color);
                        sw.WriteLine($"PX {pixel.X + frame.OffsetX} {pixel.Y + frame.OffsetY} {color.R:X2}{color.G:X2}{color.B:X2}");
                    }
                    sw.Flush();
                    return new ArraySegment<byte>(ms.GetBuffer(), 0, (int)ms.Length);
                }
            }
        }
    }
}