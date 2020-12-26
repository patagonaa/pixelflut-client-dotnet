using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace PixelFlut.Infrastructure
{
    public class SimpleRenderService : IRenderService, IDisposable
    {
        private Size _canvasSize;

        public void Dispose()
        {
        }

        public IList<KeyValuePair<string, string>> GetDiagnostics()
        {
            throw new NotImplementedException();
        }

        public void Init(Size canvasSize)
        {
            _canvasSize = canvasSize;
        }

        public ArraySegment<byte> PreRender(OutputFrame frame)
        {
            const int pxLen = 22;

            var canvasWidth = _canvasSize.Width;
            var canvasHeight = _canvasSize.Height;

            using (var ms = new MemoryStream(pxLen * frame.Pixels.Length))
            {
                using (var sw = new StreamWriter(ms))
                {
                    sw.NewLine = "\n";
                    foreach (var pixel in frame.Pixels)
                    {
                        uint color = pixel.Color;
                        var renderX = pixel.X + frame.OffsetX;
                        var renderY = pixel.Y + frame.OffsetY;
                        if (renderX < 0 || renderY < 0 || renderX >= canvasWidth || renderY >= canvasHeight)
                            continue;

                        if (color < 0xFF000000)
                        {
                            sw.WriteLine($"PX {pixel.X + frame.OffsetX} {pixel.Y + frame.OffsetY} {color & 0x00FFFFFF:X6}{color >> 24:X2}");
                        }
                        else
                        {
                            sw.Write("PX ");
                            sw.Write((pixel.X + frame.OffsetX).ToString());
                            sw.Write(" ");
                            sw.Write((pixel.Y + frame.OffsetY).ToString());
                            sw.Write(" ");
                            sw.WriteLine((color & 0x00FFFFFF).ToString("X6"));
                        }
                    }
                    sw.Flush();
                    return new ArraySegment<byte>(ms.GetBuffer(), 0, (int)ms.Length);
                }
            }
        }
    }
}