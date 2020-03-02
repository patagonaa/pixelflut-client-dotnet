using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace PixelFlut.Infrastructure
{
    public class EffectHost
    {
        private const int EffectQueueLength = 10;
        private const int RenderQueueLength = 20;
        private const int RenderThreadCount = 16;
        private const int OutputThreadCount = 4;

        private IList<Thread> effectThreads = new List<Thread>();
        private Thread renderThread;
        private Thread outputThread;
        private Thread logThread;
        private CancellationTokenSource cancellationTokenSource;
        private readonly IRenderService renderService;
        private readonly EndPoint endPoint;
        private readonly IOutputService outputService;
        private volatile bool effectTooSlow = false;
        private volatile bool renderTooSlow = false;
        private volatile bool outputTooSlow = false;

        private ConcurrentQueue<OutputFrame> effectQueue = new ConcurrentQueue<OutputFrame>();
        private ConcurrentQueue<ArraySegment<byte>> renderedQueue = new ConcurrentQueue<ArraySegment<byte>>();

        public EffectHost(IRenderService renderService, EndPoint endPoint)
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            this.renderService = renderService;
            this.outputService = new PixelFlutOutputService(endPoint);
            this.endPoint = endPoint;
        }

        public void Start()
        {
            this.renderThread = new Thread(Render);
            this.renderThread.Start();

            this.outputThread = new Thread(Output);
            this.outputThread.Priority = ThreadPriority.AboveNormal;
            this.outputThread.Start();

            this.logThread = new Thread(Log);
            this.logThread.Start();
        }

        public void Stop()
        {
            cancellationTokenSource?.Cancel();
            foreach (var effectThread in effectThreads)
            {
                effectThread.Join();
            }
            renderThread?.Join();
            outputThread?.Join();

            this.cancellationTokenSource = new CancellationTokenSource();
        }

        private void Effect(IEffect effect)
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (this.effectQueue.Count < EffectQueueLength)
                {
                    OutputFrame pixels = effect.GetPixels();
                    this.effectQueue.Enqueue(pixels);
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }

        private const int _numRenderDiagSamples = 1000;
        private Queue<Tuple<DateTime, int>> _renderDiagSamples = new Queue<Tuple<DateTime, int>>();
        private void Render()
        {
            var taskQueue = new Queue<Task<ArraySegment<byte>>>();
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (this.renderedQueue.Count < RenderQueueLength)
                {
                    outputTooSlow = false;
                    if (taskQueue.Count < RenderThreadCount)
                    {
                        if (!this.effectQueue.TryDequeue(out var frame))
                        {
                            effectTooSlow = true;
                            continue;
                        }
                        effectTooSlow = false;
                        taskQueue.Enqueue(Task.Run(() =>
                        {
                            ArraySegment<byte> arraySegment = this.renderService.PreRender(frame);
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
                    }
                    else
                    {
                        var task = taskQueue.Dequeue();
                        task.Wait();
                        this.renderedQueue.Enqueue(task.Result);
                    }
                }
                else
                {
                    outputTooSlow = true;
                    Thread.Sleep(1);
                }
            }
        }

        private void Output()
        {
            if (OutputThreadCount > 1)
            {
                var threads = new List<Thread>();
                for (int i = 0; i < OutputThreadCount; i++)
                {
                    var os = new PixelFlutOutputService(this.endPoint);
                    var thr = new Thread(() => DoOutput(os));
                    thr.Priority = ThreadPriority.AboveNormal;
                    threads.Add(thr);
                    thr.Start();
                }
                threads.ForEach(x => x.Join());
            }
            else
            {
                DoOutput(this.outputService);
            }
        }

        private const int _numOutputDiagSamples = 1000;
        private Queue<Tuple<DateTime, int>> _outputDiagSamples = new Queue<Tuple<DateTime, int>>();

        private void DoOutput(IOutputService outputService)
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (this.renderedQueue.TryDequeue(out var rendered))
                {
                    renderTooSlow = false;
                    var sentBytes = outputService.Output(rendered);
                    //var sentBytes = rendered.Count;
                    lock (_outputDiagSamples)
                    {
                        _outputDiagSamples.Enqueue(new Tuple<DateTime, int>(DateTime.UtcNow, sentBytes));

                        if (_outputDiagSamples.Count > _numOutputDiagSamples)
                        {
                            _outputDiagSamples.Dequeue();
                        }
                    }
                }
                else
                {
                    renderTooSlow = true;
                    Thread.Sleep(1);
                }
            }
        }

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
                diagnostics.Add(new KeyValuePair<string, string>("RenderBuf", this.effectQueue.Count.ToString("D3", CultureInfo.InvariantCulture)));
                diagnostics.Add(new KeyValuePair<string, string>("OutBuf", this.renderedQueue.Count.ToString("D3", CultureInfo.InvariantCulture)));
                var bottleneck = "";

                if (effectTooSlow)
                {
                    bottleneck = "effect";
                }
                else if (renderTooSlow)
                {
                    bottleneck = "render";
                }
                else if (outputTooSlow)
                {
                    bottleneck = "output";
                }

                diagnostics.Add(new KeyValuePair<string, string>("Bottleneck", bottleneck));

                diagnostics.AddRange(GetOutputDiagnostics());
                diagnostics.AddRange(GetRenderDiagnostics());

                Console.WriteLine(string.Join(", ", diagnostics.Select(x => $"{x.Key}: {x.Value}")));
                Thread.Sleep(500);
            }
        }

        public void AddEffect(IEffect effect)
        {
            var effectThread = new Thread(() => Effect(effect));
            effectThread.Priority = ThreadPriority.Highest;

            effect.Init(this.outputService.GetSize());
            effectThread.Start();
        }
    }
}