using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace PixelFlut.Infrastructure
{

    public unsafe class PixelFlutLookupTableUnsafeRenderService : IRenderService, IDisposable
    {
        private readonly List<GCHandle> _gcHandles = new List<GCHandle>();

        public PixelFlutLookupTableUnsafeRenderService()
        {
            var pxHandle = GCHandle.Alloc(Encoding.ASCII.GetBytes("PX "), GCHandleType.Pinned);
            px = (byte*)pxHandle.AddrOfPinnedObject();
            _gcHandles.Add(pxHandle);

            newline = Encoding.ASCII.GetBytes("\n")[0];
            space = Encoding.ASCII.GetBytes(" ")[0];
            var decNumbers = Enumerable.Range(0, 5000)
                .Select(x =>
                    Encoding.ASCII.GetBytes(x.ToString(CultureInfo.InvariantCulture) + " ").Concat(new[] { (byte)0 }).ToArray())
                .ToArray();

            var decNumbersPtrs = decNumbers.Select(x =>
            {
                var handle = GCHandle.Alloc(x, GCHandleType.Pinned);
                _gcHandles.Add(handle);
                return handle.AddrOfPinnedObject();
            }).ToArray();

            var decNumbersHandle = GCHandle.Alloc(decNumbersPtrs, GCHandleType.Pinned);
            numbersWithSpace = (byte**)decNumbersHandle.AddrOfPinnedObject();
            _gcHandles.Add(decNumbersHandle);

            var hexNumbersHandle = GCHandle.Alloc(Enumerable.Range(0, 256).SelectMany(x => Encoding.ASCII.GetBytes(x.ToString("X2"))).ToArray(), GCHandleType.Pinned);
            hexNumbers = (byte*)hexNumbersHandle.AddrOfPinnedObject();
            _gcHandles.Add(hexNumbersHandle);
        }

        public IList<KeyValuePair<string, string>> GetDiagnostics()
        {
            var toReturn = new List<KeyValuePair<string, string>>();
            return toReturn;
        }

        private readonly byte* px;
        private readonly byte newline;
        private readonly byte space;
        private readonly byte** numbersWithSpace;
        private readonly byte* hexNumbers;

        public ArraySegment<byte> PreRender(OutputPixel[] pixels)
        {
            using (var ms = new UnsafeMemoryBuffer(pixels.Length * 22))
            {
                var len = pixels.Length;

                for (int i = 0; i < len; i++)
                {
                    var pixel = pixels[i];
                    ms.Write(px, 3);
                    var xNum = numbersWithSpace[pixel.X];
                    ms.WriteNullTerminated(xNum);

                    var yNum = numbersWithSpace[pixel.Y];
                    ms.WriteNullTerminated(yNum);

                    var argbColor = pixel.Color;

                    ms.Write(hexNumbers + (argbColor >> 16 & 0xFF) * 2, 2);
                    ms.Write(hexNumbers + (argbColor >> 8 & 0xFF) * 2, 2);
                    ms.Write(hexNumbers + (argbColor & 0xFF) * 2, 2);

                    var a = (argbColor >> 24 & 0xFF);
                    if (a != 255)
                    {
                        ms.Write(hexNumbers + a * 2, 2);
                    }
                    ms.WriteByte(newline);
                }
                return ms.ToArray();
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}