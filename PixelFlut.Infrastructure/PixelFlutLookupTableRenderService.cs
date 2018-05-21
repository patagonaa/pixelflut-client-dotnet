using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace PixelFlut.Infrastructure
{

    public class PixelFlutLookupTableRenderService : IRenderService
    {
        public IList<KeyValuePair<string, string>> GetDiagnostics()
        {
            var toReturn = new List<KeyValuePair<string, string>>();
            return toReturn;
        }

        private readonly byte[] px = Encoding.ASCII.GetBytes("PX ");
        private readonly byte newline = Encoding.ASCII.GetBytes("\n")[0];
        private readonly byte space = Encoding.ASCII.GetBytes(" ")[0];
        private readonly byte[][] numbers = Enumerable.Range(0, 5000).Select(x => Encoding.ASCII.GetBytes(x.ToString(CultureInfo.InvariantCulture))).ToArray();
        private readonly byte[][] hexNumbers = Enumerable.Range(0, 256).Select(x => Encoding.ASCII.GetBytes(x.ToString("X2"))).ToArray();

        public byte[] PreRender(IReadOnlyCollection<OutputPixel> pixels)
        {
            using (var ms = new MemoryStream(pixels.Count * 20))
            {
                foreach (var pixel in pixels)
                {
                    ms.Write(px, 0, 3);
                    var xNum = numbers[pixel.X];
                    ms.Write(xNum, 0, xNum.Length);

                    ms.WriteByte(space);

                    var yNum = numbers[pixel.Y];
                    ms.Write(yNum, 0, yNum.Length);

                    ms.WriteByte(space);

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