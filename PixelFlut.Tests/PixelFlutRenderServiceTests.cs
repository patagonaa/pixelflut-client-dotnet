using NUnit.Framework;
using PixelFlut.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace PixelFlut.Tests
{
    public class PixelFlutRenderServiceTests
    {
        [Test]
        public void SimpleRenderService_SimpleFrame_Correct()
        {
            var pixels = new[] {
                new OutputPixel(0, 0, unchecked((int)0xFFFF00FF)),
                new OutputPixel(0, 1, unchecked((int)0xFF00FF00)),
                new OutputPixel(1, 0, unchecked((int)0xFF0000FF)),
                new OutputPixel(0, 0, unchecked((int)0xFFFFFF00)),
            };

            var frame = new OutputFrame(0, 0, pixels);
            var sut = new SimpleRenderService();
            var actual = Encoding.ASCII.GetString(sut.PreRender(frame));

            Assert.AreEqual("PX 0 0 FF00FF\nPX 0 1 00FF00\nPX 1 0 0000FF\nPX 0 0 FFFF00\n", actual);
        }

        [Test]
        public void UnsafeRenderService_SimpleFrame_Correct()
        {
            var pixels = new[] {
                new OutputPixel(0, 0, unchecked((int)0xFFFF00FF)),
                new OutputPixel(0, 1, unchecked((int)0xFF00FF00)),
                new OutputPixel(1, 0, unchecked((int)0xFF0000FF)),
                new OutputPixel(0, 0, unchecked((int)0xFFFFFF00)),
            };

            var frame = new OutputFrame(0, 0, pixels);
            var sut = new PixelFlutLookupTableUnsafeRenderService(ServerCapabilities.None);
            var actual = Encoding.ASCII.GetString(sut.PreRender(frame));

            Assert.AreEqual("PX 0 0 FF00FF\nPX 0 1 00FF00\nPX 1 0 0000FF\nPX 0 0 FFFF00\n", actual);
        }

        [Test]
        public void UnsafeRenderService_TryToFuckItUpFrame_Correct()
        {
            var r = new Random();
            for (int i = 0; i < 10; i++)
            {
                var pixels = new List<OutputPixel>();
                for (int pxNum = 0; pxNum < 100000; pxNum++)
                {
                    pixels.Add(new OutputPixel(r.Next(2000), r.Next(2000), unchecked((int)(r.Next(0xFFFFFF) | 0xFF000000))));
                }

                var frame = new OutputFrame(0, 0, pixels.ToArray());

                var simpleSut = new SimpleRenderService();
                var unsafeSut = new PixelFlutLookupTableUnsafeRenderService(ServerCapabilities.None);

                var simpleFrame = Encoding.ASCII.GetString(simpleSut.PreRender(frame));
                var unsafeFrame = Encoding.ASCII.GetString(unsafeSut.PreRender(frame));

                Assert.AreEqual(simpleFrame, unsafeFrame);
            }
        }
    }
}