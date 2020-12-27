using PixelFlut.Infrastructure;
using System.Drawing;
using System.Threading.Tasks;
using FFMpegCore;
using System.IO;
using FFMpegCore.Pipes;
using FFMpegCore.Enums;
using System.Threading;
using System;

namespace PixelFlut.Demo.Effects
{
    class VideoPlayback : EffectBase
    {
        private readonly string _filePath;
        private readonly CancellationTokenSource _cts;
        private Size _videoSize;
        private byte[] _frame;
        private OutputPixel[] _pixels;

        public VideoPlayback(string filePath)
        {
            _filePath = filePath;
            _cts = new CancellationTokenSource();
        }

        public override void Init(Size canvasSize)
        {
            base.Init(canvasSize);
            InitAsync().Wait();
        }

        private async Task InitAsync()
        {
            var result = await FFProbe.AnalyseAsync(_filePath);

            var width = result.PrimaryVideoStream.Width;
            var height = result.PrimaryVideoStream.Height;
            _videoSize = new Size(width, height);

            _ = Task.Factory.StartNew(() => GenerateFrames(result), TaskCreationOptions.LongRunning);
        }

        private async Task GenerateFrames(IMediaAnalysis result)
        {
            while (!_cts.IsCancellationRequested)
            {
                var sink = new RawImagePipeSink(result.PrimaryVideoStream, 3, OnFrame, true);
                var args = FFMpegArguments
                    .FromFileInput(_filePath).OutputToPipe(sink, options =>
                     options.DisableChannel(Channel.Audio)
                     .UsingMultithreading(true)
                     .ForceFormat("rawvideo")
                     .ForcePixelFormat("bgr24"))
                    .CancellableThrough(out var cancelCallback);
                _cts.Token.Register(cancelCallback);

                //fire and forget
                await args.ProcessAsynchronously(true);
            }
        }

        private async Task OnFrame(byte[] frame)
        {
            _frame = frame;
        }

        protected override OutputFrame TickInternal()
        {
            while (_frame == null)
            {
                Thread.Sleep(10);
            }

            if (_frame != null)
            {
                var frame = _frame;
                _pixels = new OutputPixel[_videoSize.Width * _videoSize.Height];
                int i = 0;
                for (int y = 0; y < _videoSize.Height; y++)
                {
                    for (int x = 0; x < _videoSize.Width; x++)
                    {
                        var argb = unchecked((uint)(frame[i * 3] | frame[i * 3 + 1] << 8 | frame[i * 3 + 2] << 16));

                        _pixels[i] = new OutputPixel(x, y, argb);

                        i++;
                    }
                }
                _frame = null;
            }

            var outputPixels = _pixels;
            return new OutputFrame(0, 0, outputPixels, -1, false);
        }

        public override void Dispose()
        {
            _cts.Cancel();
            base.Dispose();
        }

        class RawImagePipeSink : IPipeSink
        {
            private readonly int _frameSize;
            private readonly double _frameRate;
            private readonly Func<byte[], Task> _onFrame;
            private readonly bool _limitFrameRate;

            public RawImagePipeSink(VideoStream primaryVideoStream, int bytesPerPixel, Func<byte[], Task> onFrame, bool limitFrameRate = false)
            {
                _frameSize = primaryVideoStream.Width * primaryVideoStream.Height * bytesPerPixel;
                _frameRate = primaryVideoStream.FrameRate;
                _onFrame = onFrame;
                _limitFrameRate = limitFrameRate;
            }

            public string GetFormat()
            {
                return string.Empty;
            }

            public async Task ReadAsync(Stream inputStream, CancellationToken cancellationToken)
            {
                var buffer = new byte[_frameSize];
                int bufferPos = 0;
                var lastFrameTime = DateTime.MinValue;
                var frameDuration = TimeSpan.FromSeconds(1 / _frameRate);

                while (!cancellationToken.IsCancellationRequested)
                {
                    while (bufferPos < _frameSize)
                    {
                        var readBytes = await inputStream.ReadAsync(buffer, bufferPos, _frameSize - bufferPos);
                        if (readBytes == 0) // stream end
                            return;
                        bufferPos += readBytes;
                    }
                    if (bufferPos != _frameSize)
                        throw new InvalidOperationException("invalid BufferPos");
                    await _onFrame(buffer);
                    if (_limitFrameRate)
                    {
                        var waitTime = frameDuration - (DateTime.UtcNow - lastFrameTime);
                        if (waitTime > TimeSpan.Zero)
                            await Task.Delay(waitTime);

                        lastFrameTime = DateTime.UtcNow;
                    }
                    bufferPos = 0;
                }
            }
        }
    }
}