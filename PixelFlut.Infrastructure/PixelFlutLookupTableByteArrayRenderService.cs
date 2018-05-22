using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PixelFlut.Infrastructure
{

    public class PixelFlutLookupTableByteArrayRenderService : IRenderService
    {
        public IList<KeyValuePair<string, string>> GetDiagnostics()
        {
            var toReturn = new List<KeyValuePair<string, string>>();
            return toReturn;
        }

        private readonly byte[] px = Encoding.ASCII.GetBytes("PX ");
        private readonly byte newline = Encoding.ASCII.GetBytes("\n")[0];
        private readonly byte space = Encoding.ASCII.GetBytes(" ")[0];
        private readonly byte[][] numbersWithSpace = Enumerable.Range(0, 5000).Select(x => Encoding.ASCII.GetBytes(x.ToString(CultureInfo.InvariantCulture) + " ")).ToArray();
        private readonly byte[][] hexNumbers = Enumerable.Range(0, 256).Select(x => Encoding.ASCII.GetBytes(x.ToString("X2"))).ToArray();

        public ArraySegment<byte> PreRender(OutputPixel[] pixels)
        {
            using (var ms = new UnsafeMemoryBuffer(pixels.Length * 24))
            {
                var len = pixels.Length;

                for(int i = 0; i < len; i++)
                {
                    var pixel = pixels[i];
                    ms.Write(px, 0, 3);
                    var xNum = numbersWithSpace[pixel.X];
                    ms.Write(xNum, 0, xNum.Length);

                    var yNum = numbersWithSpace[pixel.Y];
                    ms.Write(yNum, 0, yNum.Length);

                    var argbColor = pixel.Color;

                    ms.Write(hexNumbers[argbColor >> 16 & 0xFF], 0, 2);
                    ms.Write(hexNumbers[argbColor >> 8 & 0xFF], 0, 2);
                    ms.Write(hexNumbers[argbColor & 0xFF], 0, 2);

                    var a = (argbColor >> 24 & 0xFF);
                    if (a != 255)
                    {
                        ms.Write(hexNumbers[a], 0, 2);
                    }
                    ms.WriteByte(newline);
                }
                return ms.ToArray();
            }
        }
    }
}