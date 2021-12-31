using PixelFlut.Infrastructure;
using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace PixelFlut.Demo
{
    class EffectHost
    {
        private readonly Size _outputSize;
        private readonly IRenderService _renderService;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private readonly TransformBlock<OutputFrame, ArraySegment<byte>> _renderTransformBlock;

        private ulong _pixelsHandled = 0;
        private ulong _bytesHandled = 0;

        public EffectHost(Size outputSize, IRenderService renderService)
        {
            _outputSize = outputSize;
            renderService.Init(outputSize);
            _renderService = renderService;
            _renderTransformBlock = new TransformBlock<OutputFrame, ArraySegment<byte>>(Render, new ExecutionDataflowBlockOptions { BoundedCapacity = 20, MaxDegreeOfParallelism = 16 });
        }

        public void Start()
        {
            _ = Task.Factory.StartNew(DiagnosticWorker, TaskCreationOptions.LongRunning);
        }

        private async void DiagnosticWorker()
        {
            var lastTime = DateTime.UtcNow;
            var lastPixels = 0UL;
            var lastBytes = 0UL;

            while (!_cts.IsCancellationRequested)
            {
                var time = DateTime.UtcNow;
                var pixels = _pixelsHandled;
                var bytes = _bytesHandled;

                var timeSpan = time - lastTime;
                var pps = (pixels - lastPixels) / timeSpan.TotalSeconds;
                var bps = (bytes - lastBytes) / timeSpan.TotalSeconds;

                Console.WriteLine($"Render in: {_renderTransformBlock.InputCount}, Render out: {_renderTransformBlock.OutputCount}, Mpx/s: {pps / 1000 / 1000:F2}, Mb/s: {bps / 1000 / 1000 * 8:F2}");
                await Task.Delay(10000);
                lastTime = time;
                lastPixels = pixels;
                lastBytes = bytes;
            }

        }

        public void Stop()
        {
            _cts.Cancel();
        }

        public async Task AddEffect(IEffect effect, params IFilter[] filters)
        {
            await effect.Init(_outputSize);
            var transformOptions = new ExecutionDataflowBlockOptions { BoundedCapacity = 20, MaxDegreeOfParallelism = 8 };
            var filterBlocks = filters.Select(x => new TransformBlock<OutputFrame, OutputFrame>(x.ApplyFilter, transformOptions)).ToList();
            ITargetBlock<OutputFrame> firstBlock;
            if (filterBlocks.Any())
            {
                var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

                for (int i = 0; i < filterBlocks.Count; i++)
                {
                    var thisBlock = (ISourceBlock<OutputFrame>)filterBlocks[i];
                    var nextBlock = (i+1) >= filterBlocks.Count ? (ITargetBlock<OutputFrame>)_renderTransformBlock : filterBlocks[i + 1];
                    thisBlock.LinkTo(nextBlock, linkOptions);
                }
                firstBlock = filterBlocks[0];
            }
            else
            {
                firstBlock = _renderTransformBlock;
            }

            _ = Task.Run(async () =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    var frame = await effect.GetPixels();
                    await firstBlock.SendAsync(frame);
                }
                firstBlock.Complete();
            });
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task AddOutput(IOutputService outputService)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var outputBlock = new ActionBlock<ArraySegment<byte>>(x => outputService.Output(x), new ExecutionDataflowBlockOptions { BoundedCapacity = 20 });
            _renderTransformBlock.LinkTo(outputBlock);
        }

        private ArraySegment<byte> Render(OutputFrame pixels)
        {
            ArraySegment<byte> bytes = _renderService.PreRender(pixels);
            Interlocked.Add(ref _bytesHandled, (ulong)bytes.Count);
            Interlocked.Add(ref _pixelsHandled, (ulong)pixels.Pixels.Length);
            return bytes;
        }
    }
}
