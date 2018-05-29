﻿using System;
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
        private readonly byte* hexColors;
        private readonly byte* hexNumbers;
        private readonly ServerCapabilities serverCapabilities;
        private readonly List<GCHandle> _gcHandles = new List<GCHandle>();

        private readonly IDictionary<int, byte[]> _cache = new Dictionary<int, byte[]>();

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

            byte[] hexColorsBytes = Enumerable.Range(0, 0xFFFFFF).SelectMany(x => Encoding.ASCII.GetBytes(x.ToString("X6"))).ToArray();
            var hexColorsHandle = GCHandle.Alloc(hexColorsBytes, GCHandleType.Pinned);
            hexColors = (byte*)hexColorsHandle.AddrOfPinnedObject();
            _gcHandles.Add(hexColorsHandle);
            Console.Write(".");

            byte[] hexNumbersBytes = Enumerable.Range(0, 0xFF).SelectMany(x => Encoding.ASCII.GetBytes(x.ToString("X2"))).ToArray();
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

            var cachingPossible = cacheId != -1 && (offsetSupported || offsetStatic);

            if (cachingPossible)
            {
                byte[] cachedFrame;
                if (!_cache.TryGetValue(cacheId, out cachedFrame))
                {
                    Console.WriteLine($"Frame {cacheId} not rendered! rendering...");
                    using (var cachems = new UnsafeMemoryBuffer(pixels.Length * 22))
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
                    using (var ms = new UnsafeMemoryBuffer(pixels.Length * 22 + offsetLen))
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
                using (var ms = new UnsafeMemoryBuffer(pixels.Length * 22 + (offsetSupported ? offsetLen : 0)))
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

        private bool IsGreyScale(int argbColor, out byte grey)
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
    }
}