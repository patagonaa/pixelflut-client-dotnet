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
        private readonly Size? _overrideSize;
        private readonly CancellationTokenSource _cts;
        private Size _videoSize;
        private byte[] _frame;
        private OutputPixel[] _pixels;

        public VideoPlayback(string filePath, Size? overrideSize = null)
        {
            _filePath = filePath;
            _overrideSize = overrideSize;
            _cts = new CancellationTokenSource();
        }

        public override async Task Init(Size canvasSize)
        {
            await base.Init(canvasSize);

            var result = await FFProbe.AnalyseAsync(_filePath);

            _videoSize = _overrideSize ?? new Size(result.PrimaryVideoStream.Width, result.PrimaryVideoStream.Height);

            _ = Task.Factory.StartNew(() => GenerateFrames(result), TaskCreationOptions.LongRunning);
        }

        private async Task GenerateFrames(IMediaAnalysis result)
        {
            while (!_cts.IsCancellationRequested)
            {
                var sink = new RawImagePipeSink(_videoSize, 3, OnFrame);
                var args = FFMpegArguments
                    .FromFileInput(_filePath).OutputToPipe(sink, options =>
                     {
                         options.DisableChannel(Channel.Audio)
                            .UsingMultithreading(true)
                            .ForceFormat("rawvideo")
                            .ForcePixelFormat("bgr24");
                         if (_overrideSize.HasValue)
                         {
                             options.Resize(_videoSize);
                         }
                     })
                    .CancellableThrough(out var cancelCallback);
                _cts.Token.Register(cancelCallback);

                //fire and forget
                await args.ProcessAsynchronously(true);
            }
        }

        private async Task OnFrame(byte[] frame)
        {
            while (_frame != null)
            {
                await Task.Yield();
            }

            _frame = frame;
        }

        protected override async Task<OutputFrame> TickInternal()
        {
            while (_frame == null)
            {
                await Task.Yield();
            }

            if (_frame != null)
            {
                var frame = _frame;
                _frame = null;
                _pixels = new OutputPixel[_videoSize.Width * _videoSize.Height];
                int i = 0;
                for (int y = 0; y < _videoSize.Height; y++)
                {
                    for (int x = 0; x < _videoSize.Width; x++)
                    {
                        var argb = unchecked((uint)(frame[i * 3] | frame[i * 3 + 1] << 8 | frame[i * 3 + 2] << 16 | 0xFF << 24));

                        _pixels[i] = new OutputPixel(x, y, argb);

                        i++;
                    }
                }
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
            private readonly Func<byte[], Task> _onFrame;

            public RawImagePipeSink(Size size, int bytesPerPixel, Func<byte[], Task> onFrame)
            {
                _frameSize = size.Width * size.Height * bytesPerPixel;
                _onFrame = onFrame;
            }

            public string GetFormat()
            {
                return string.Empty;
            }

            public async Task ReadAsync(Stream inputStream, CancellationToken cancellationToken)
            {
                var buffer = new byte[_frameSize];
                var buffer2 = new byte[_frameSize];

                try
                {
                    await ReadFrame(inputStream, buffer, cancellationToken);

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        await Task.WhenAll(_onFrame(buffer), ReadFrame(inputStream, buffer2, cancellationToken));
                        await Task.WhenAll(_onFrame(buffer2), ReadFrame(inputStream, buffer, cancellationToken));
                    }
                }
                catch (TaskCanceledException)
                {
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Error in Video Playback: " + ex);
                }
            }

            private async Task ReadFrame(Stream stream, byte[] buffer, CancellationToken token)
            {
                var bufferPos = 0;

                while (bufferPos < _frameSize)
                {
                    var readBytes = await stream.ReadAsync(buffer, bufferPos, _frameSize - bufferPos, token);
                    if (readBytes == 0 || token.IsCancellationRequested) // stream end
                        throw new TaskCanceledException();
                    bufferPos += readBytes;
                }
                if (bufferPos != _frameSize)
                    throw new InvalidOperationException("invalid BufferPos");
            }
        }
    }
}