using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PixelFlut.Infrastructure
{
    public class EffectHost
    {
        private const int EffectFilterBufferLength = 20;
        private const int FilterRenderBufferLength = 20;
        private const int RenderOutputBufferLength = 20;

        private CancellationTokenSource cancellationTokenSource;
        private readonly IRenderService renderService;
        private readonly IOutputService outputService;

        private List<Task> _tasks = new List<Task>();
        private TaskFactory _tfLongRunning = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);
        private TaskFactory _tfShort = new TaskFactory(TaskCreationOptions.PreferFairness, TaskContinuationOptions.None);
        private BlockingCollection<OutputFrame> _effectRenderBuffer = new BlockingCollection<OutputFrame>(FilterRenderBufferLength);
        private BlockingCollection<ArraySegment<byte>> _renderOutputBuffer = new BlockingCollection<ArraySegment<byte>>(RenderOutputBufferLength);

        public EffectHost(IRenderService renderService, IOutputService outputService)
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            this.renderService = renderService;
            this.outputService = outputService;
        }

        public void Start()
        {
            var renderTask = _tfLongRunning.StartNew(() => { Render(this.renderService, this._effectRenderBuffer, this._renderOutputBuffer); });
            var outputTask = _tfLongRunning.StartNew(() => { Output(this.outputService, this._renderOutputBuffer); });
            var logTask = _tfLongRunning.StartNew(() => Log());
            _tasks.AddRange(new[] { renderTask, outputTask, logTask });
        }

        public async Task Stop()
        {
            cancellationTokenSource.Cancel();
            await Task.WhenAll(_tasks);
        }

        private void Effect(IEffect effect, BlockingCollection<OutputFrame> outputFrames)
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                OutputFrame frame = effect.GetPixels();
                if (frame.Pixels == null)
                {
                    outputFrames.CompleteAdding();
                    break;
                }
                outputFrames.Add(frame);
            }
        }

        private void Filter(IFilter filter, BlockingCollection<OutputFrame> inputFrames, BlockingCollection<OutputFrame> outputFrames)
        {
            foreach (var inputFrame in inputFrames.GetConsumingEnumerable())
            {
                var frame = inputFrame;
                frame = filter.ApplyFilter(frame);
                outputFrames.Add(frame);
            }
            outputFrames.CompleteAdding();
        }

        private const int _numRenderDiagSamples = 100;
        private Queue<Tuple<DateTime, int>> _renderDiagSamples = new Queue<Tuple<DateTime, int>>();
        private async void Render(IRenderService renderService, BlockingCollection<OutputFrame> inputFrames, BlockingCollection<ArraySegment<byte>> outputBytes)
        {
            var tasksQueue = new Queue<Task<ArraySegment<byte>>>();
            foreach (var frame in inputFrames.GetConsumingEnumerable())
            {
                tasksQueue.Enqueue(_tfShort.StartNew(() =>
                {
                    ArraySegment<byte> arraySegment = renderService.PreRender(frame);
                    lock (_renderDiagSamples)
                    {
                        _renderDiagSamples.Enqueue(new Tuple<DateTime, int>(DateTime.UtcNow, frame.Pixels.Length));

                        if (_renderDiagSamples.Count > _numRenderDiagSamples)
                        {
                            _renderDiagSamples.Dequeue();
                        }
                    }
                    return arraySegment;
                }));

                if (tasksQueue.Count > 16)
                {
                    outputBytes.Add(await tasksQueue.Dequeue());
                }
            }
            outputBytes.CompleteAdding();
        }

        private void Output(IOutputService outputService, BlockingCollection<ArraySegment<byte>> inputBytes)
        {
            foreach (var bytes in inputBytes.GetConsumingEnumerable())
            {
                var sentBytes = outputService.Output(bytes);
                lock (_outputDiagSamples)
                {
                    _outputDiagSamples.Enqueue(new Tuple<DateTime, int>(DateTime.UtcNow, sentBytes));

                    if (_outputDiagSamples.Count > _numOutputDiagSamples)
                    {
                        _outputDiagSamples.Dequeue();
                    }
                }
            }
        }

        private const int _numOutputDiagSamples = 100;
        private Queue<Tuple<DateTime, int>> _outputDiagSamples = new Queue<Tuple<DateTime, int>>();

        private IList<KeyValuePair<string, string>> GetRenderDiagnostics()
        {
            var toReturn = new List<KeyValuePair<string, string>>();

            IList<Tuple<DateTime, int>> diagSamples;
            lock (_renderDiagSamples)
            {
                diagSamples = _renderDiagSamples.ToList();
            }
            if (diagSamples.Count > 2)
            {
                var span = diagSamples.Max(x => x.Item1) - diagSamples.Min(x => x.Item1);
                var totalPixels = diagSamples.Sum(x => (long)x.Item2);

                var mpixelsPerSecond = (double)totalPixels / span.TotalSeconds / 1_000_000;
                toReturn.Add(new KeyValuePair<string, string>("Output (MPixels/s)", mpixelsPerSecond.ToString("F2", CultureInfo.InvariantCulture)));
                var fps = diagSamples.Count / span.TotalSeconds;
                toReturn.Add(new KeyValuePair<string, string>("Output (Frames/s)", fps.ToString("F2", CultureInfo.InvariantCulture)));
            }

            return toReturn;
        }

        private IList<KeyValuePair<string, string>> GetOutputDiagnostics()
        {
            var toReturn = new List<KeyValuePair<string, string>>();

            IList<Tuple<DateTime, int>> diagSamples;
            lock (_outputDiagSamples)
            {
                diagSamples = _outputDiagSamples.ToList();
            }
            if (diagSamples.Count > 2)
            {
                var span = diagSamples.Max(x => x.Item1) - diagSamples.Min(x => x.Item1);
                var totalBytes = diagSamples.Sum(x => (long)x.Item2);

                var mbit = totalBytes / 1000d / 1000d * 8d;
                var bandwidthMBits = mbit / span.TotalSeconds;
                toReturn.Add(new KeyValuePair<string, string>("Bandwidth (Mbit/s)", bandwidthMBits.ToString("F2", CultureInfo.InvariantCulture)));
            }

            return toReturn;
        }

        private void Log()
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                var diagnostics = new List<KeyValuePair<string, string>>();
                diagnostics.Add(new KeyValuePair<string, string>("RenderBuf", this._effectRenderBuffer.Count.ToString("D3", CultureInfo.InvariantCulture)));
                diagnostics.Add(new KeyValuePair<string, string>("OutBuf", this._renderOutputBuffer.Count.ToString("D3", CultureInfo.InvariantCulture)));

                diagnostics.AddRange(GetOutputDiagnostics());
                diagnostics.AddRange(GetRenderDiagnostics());

                Console.WriteLine(string.Join(", ", diagnostics.Select(x => $"{x.Key}: {x.Value}")));
                Thread.Sleep(500);
            }
        }

        public void AddEffect(IEffect effect, params IFilter[] filters)
        {
            var effectBuffer = filters.Length == 0 ? this._effectRenderBuffer : new BlockingCollection<OutputFrame>(EffectFilterBufferLength);

            effect.Init(this.outputService.GetSize());
            var effectTask = _tfLongRunning.StartNew(() => { Effect(effect, effectBuffer); });
            this._tasks.Add(effectTask);

            BlockingCollection<OutputFrame> filterInBuffer = effectBuffer;
            for (int i = 0; i < filters.Length; i++)
            {
                var filter = filters[i];
                var filterOutBuffer = i == filters.Length - 1 ? this._effectRenderBuffer : new BlockingCollection<OutputFrame>(EffectFilterBufferLength);
                var thisFilterInBuffer = filterInBuffer;
                var filterTask = _tfLongRunning.StartNew(() => { Filter(filter, thisFilterInBuffer, filterOutBuffer); });
                this._tasks.Add(filterTask);
                filterInBuffer = filterOutBuffer;
            }
        }
    }
}