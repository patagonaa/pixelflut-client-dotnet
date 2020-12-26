using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Drawing;

namespace PixelFlut.Infrastructure
{

    public unsafe class PixelFlutLookupTableUnsafeRenderService : IRenderService, IDisposable
    {
        private readonly byte* px;
        private readonly byte* offset;
        private readonly byte newline;
        private readonly byte space;
        private readonly byte** numbers;
        private readonly byte* hexColors;
        private readonly byte* hexNumbers;
        private readonly ServerCapabilities serverCapabilities;
        private readonly List<GCHandle> _gcHandles = new List<GCHandle>();

        private readonly IDictionary<int, byte[]> _cache = new ConcurrentDictionary<int, byte[]>();

        public PixelFlutLookupTableUnsafeRenderService(ServerCapabilities serverCapabilities)
        {
            this.serverCapabilities = serverCapabilities;

            Console.Write("Precalculating Strings");
            var pxHandle = GCHandle.Alloc(Encoding.ASCII.GetBytes("PX "), GCHandleType.Pinned);
            px = (byte*)pxHandle.AddrOfPinnedObject();
            _gcHandles.Add(pxHandle);
            Console.Write(".");

            var offsetHandle = GCHandle.Alloc(Encoding.ASCII.GetBytes("OFFSET "), GCHandleType.Pinned);
            offset = (byte*)offsetHandle.AddrOfPinnedObject();
            _gcHandles.Add(offsetHandle);
            Console.Write(".");

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
            Console.Write(".");

            using (var ms = new MemoryStream(6 * (0xFFFFFF + 1)))
            {
                using (var sw = new StreamWriter(ms, Encoding.ASCII))
                {
                    for (int i = 0; i <= 0xFFFFFF; i++)
                    {
                        sw.Write(i.ToString("X6"));
                        if (i % 50000 == 0)
                        {
                            Console.Write(".");
                        }
                    }
                    sw.Flush();

                    var hexColorsHandle = GCHandle.Alloc(ms.ToArray(), GCHandleType.Pinned);
                    hexColors = (byte*)hexColorsHandle.AddrOfPinnedObject();
                    _gcHandles.Add(hexColorsHandle);
                    Console.Write(".");
                }
            }

            byte[] hexNumbersBytes = Enumerable.Range(0, 0xFF + 1).SelectMany(x => Encoding.ASCII.GetBytes(x.ToString("X2"))).ToArray();
            var hexNumbersHandle = GCHandle.Alloc(hexNumbersBytes, GCHandleType.Pinned);
            hexNumbers = (byte*)hexNumbersHandle.AddrOfPinnedObject();
            _gcHandles.Add(hexNumbersHandle);
            Console.Write(".");
            Console.WriteLine();
            Console.WriteLine("done!");
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
            var cacheId = frame.CacheId;
            var offsetStatic = frame.OffsetStatic;

            bool offsetSupported = ((serverCapabilities & ServerCapabilities.Offset) != 0);
            bool greyscaleSupported = ((serverCapabilities & ServerCapabilities.GreyScale) != 0);

            const int offsetLen = 7 + 4 + 1 + 4 + 1;
            const int pxLen = 22;

            var cachingPossible = cacheId != -1 && (offsetSupported || offsetStatic);

            if (cachingPossible)
            {
                byte[] cachedFrame;

                if (!_cache.TryGetValue(cacheId, out cachedFrame))
                {

                    Console.WriteLine($"Frame {cacheId} not rendered! rendering...");
                    using (var cachems = GetBuffer(pixels.Length * pxLen))
                    {
                        RenderPixels(pixels, offsetX, offsetY, offsetSupported, greyscaleSupported, cachems);
                        var rendered = cachems.ToArraySegment();
                        byte[] renderedArray = rendered.ToArray();
                        _cache[cacheId] = renderedArray;
                        cachedFrame = renderedArray;
                    }
                }

                if (!offsetStatic)
                {
                    using (var ms = GetBuffer(pixels.Length * pxLen + offsetLen))
                    {
                        WriteOffset(offsetX, offsetY, ms);
                        ms.Write(cachedFrame, cachedFrame.Length);
                        return ms.ToArraySegment();
                    }
                }
                else
                {
                    return new ArraySegment<byte>(cachedFrame);
                }
            }
            else
            {
                using (var ms = GetBuffer(pixels.Length * pxLen + (offsetSupported ? offsetLen : 0)))
                {
                    if (offsetSupported)
                    {
                        WriteOffset(offsetX, offsetY, ms);
                    }
                    RenderPixels(pixels, offsetX, offsetY, offsetSupported, greyscaleSupported, ms);
                    return ms.ToArraySegment();
                }
            }
        }

        private void WriteOffset(int offsetX, int offsetY, UnsafeMemoryBuffer ms)
        {
            ms.Write(offset, 7);
            var xNum = numbers[offsetX];
            ms.WriteNullTerminated(xNum);
            ms.WriteByte(space);
            var yNum = numbers[offsetY];
            ms.WriteNullTerminated(yNum);
            ms.WriteByte(newline);
        }

        private void RenderPixels(OutputPixel[] pixels, int offsetX, int offsetY, bool omitOffset, bool greyscaleSupported, UnsafeMemoryBuffer ms)
        {
            var len = pixels.Length;
            for (int i = 0; i < len; i++)
            {
                var pixel = pixels[i];

                int pixelX;
                int pixelY;

                if (omitOffset)
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

                var a = (byte)(argbColor >> 24 & 0xFF);

                if (greyscaleSupported && IsGreyScale(argbColor, out var grey) && a == 0xFF)
                {
                    ms.Write(hexNumbers + (grey << 1), 2);
                }
                else
                {
                    ms.Write(hexColors + (argbColor & 0x00FFFFFF) * 6, 6);

                    if (a != 0xFF)
                    {
                        ms.Write(hexNumbers + (a << 1), 2);
                    }
                }
                ms.WriteByte(newline);
            }
        }

        private UnsafeMemoryBuffer GetBuffer(int length)
        {
            return new UnsafeMemoryBuffer(length);
        }

        private bool IsGreyScale(uint argbColor, out byte grey)
        {
            var r = (byte)(argbColor >> 16 & 0xFF);
            var g = (byte)(argbColor >> 8 & 0xFF);
            var b = (byte)(argbColor & 0xFF);
            grey = r;

            return r == b && r == g;
        }

        public void Dispose()
        {
            foreach (var gcHandle in _gcHandles)
            {
                gcHandle.Free();
            }
        }

        public void Init(Size canvasSize)
        {
            throw new NotImplementedException();
        }
    }
}