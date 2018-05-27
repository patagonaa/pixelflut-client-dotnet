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
        private readonly byte* px;
        private readonly byte* offset;
        private readonly byte newline;
        private readonly byte space;
        private readonly byte** numbers;
        private readonly byte* hexNumbers;
        private readonly ServerCapabilities serverCapabilities;
        private readonly List<GCHandle> _gcHandles = new List<GCHandle>();

        public PixelFlutLookupTableUnsafeRenderService(ServerCapabilities serverCapabilities)
        {
            this.serverCapabilities = serverCapabilities;

            var pxHandle = GCHandle.Alloc(Encoding.ASCII.GetBytes("PX "), GCHandleType.Pinned);
            px = (byte*)pxHandle.AddrOfPinnedObject();
            _gcHandles.Add(pxHandle);

            var offsetHandle = GCHandle.Alloc(Encoding.ASCII.GetBytes("OFFSET "), GCHandleType.Pinned);
            offset = (byte*)offsetHandle.AddrOfPinnedObject();
            _gcHandles.Add(offsetHandle);

            newline = Encoding.ASCII.GetBytes("\n")[0];
            space = Encoding.ASCII.GetBytes(" ")[0];
            var decNumbers = Enumerable.Range(0, 5000)
                .Select(x =>
                    Encoding.ASCII.GetBytes(x.ToString(CultureInfo.InvariantCulture)).Concat(new[] { (byte)0 }).ToArray())
                .ToArray();

            var decNumbersPtrs = decNumbers.Select(x =>
            {
                var handle = GCHandle.Alloc(x, GCHandleType.Pinned);
                _gcHandles.Add(handle);
                return handle.AddrOfPinnedObject();
            }).ToArray();

            var decNumbersHandle = GCHandle.Alloc(decNumbersPtrs, GCHandleType.Pinned);
            numbers = (byte**)decNumbersHandle.AddrOfPinnedObject();
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

        public ArraySegment<byte> PreRender(OutputFrame frame)
        {
            var pixels = frame.Pixels;
            var offsetX = frame.OffsetX;
            var offsetY = frame.OffsetY;

            bool offsetSupported = ((serverCapabilities & ServerCapabilities.Offset) != 0);
            bool greyscaleSupported = ((serverCapabilities & ServerCapabilities.GreyScale) != 0);

            const int offsetLen = 7 + 4 + 1 + 4 + 1;

            using (var ms = new UnsafeMemoryBuffer(pixels.Length * 22 + (offsetSupported ? offsetLen : 0)))
            {
                var len = pixels.Length;

                if (offsetSupported)
                {
                    ms.Write(offset, 7);
                    var xNum = numbers[offsetX];
                    ms.WriteNullTerminated(xNum);
                    ms.WriteByte(space);
                    var yNum = numbers[offsetY];
                    ms.WriteNullTerminated(yNum);
                    ms.WriteByte(newline);
                }

                for (int i = 0; i < len; i++)
                {
                    var pixel = pixels[i];

                    int pixelX;
                    int pixelY;

                    if (offsetSupported)
                    {
                        pixelX = pixel.X;
                        pixelY = pixel.Y;
                    }
                    else
                    {
                        pixelX = pixel.X + offsetX;
                        pixelY = pixel.Y + offsetY;
                    }

                    ms.Write(px, 3);
                    var xNum = numbers[pixelX];
                    ms.WriteNullTerminated(xNum);
                    ms.WriteByte(space);

                    var yNum = numbers[pixelY];
                    ms.WriteNullTerminated(yNum);
                    ms.WriteByte(space);

                    var argbColor = pixel.Color;

                    var a = (argbColor >> 24 & 0xFF);
                    var r = (argbColor >> 16 & 0xFF);
                    var g = (argbColor >> 8 & 0xFF);
                    var b = (argbColor & 0xFF);

                    if (greyscaleSupported && r == b && b == g && a == 255)
                    {
                        ms.Write(hexNumbers + (r << 1), 2);
                    }
                    else
                    {
                        ms.Write(hexNumbers + (r << 1), 2);
                        ms.Write(hexNumbers + (g << 1), 2);
                        ms.Write(hexNumbers + (b << 1), 2);

                        if (a != 255)
                        {
                            ms.Write(hexNumbers + (a << 1), 2);
                        }
                    }
                    ms.WriteByte(newline);
                }
                return ms.ToArray();
            }
        }

        public void Dispose()
        {
            foreach (var gcHandle in _gcHandles)
            {
                gcHandle.Free();
            }
        }
    }
}