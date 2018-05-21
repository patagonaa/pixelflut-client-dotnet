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
        private const int OutputThreadCount = 2;

        private IEffect effect;
        private object effectLock = new object();

        private Thread effectThread;
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

        private ConcurrentQueue<IReadOnlyCollection<OutputPixel>> effectQueue = new ConcurrentQueue<IReadOnlyCollection<OutputPixel>>();
        private ConcurrentQueue<byte[]> renderedQueue = new ConcurrentQueue<byte[]>();

        public EffectHost(IRenderService renderService, EndPoint endPoint)
        {
            this.renderService = renderService;
            this.outputService = new PixelFlutOutputService(endPoint);
            this.endPoint = endPoint;
        }

        public void Start()
        {
            this.cancellationTokenSource = new CancellationTokenSource();

            this.effectThread = new Thread(Effect);
            this.effectThread.Priority = ThreadPriority.Highest;
            this.effectThread.Start();

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
            effectThread?.Join();
            renderThread?.Join();
            outputThread?.Join();
        }

        private void Effect()
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (this.effectQueue.Count < EffectQueueLength)
                {
                    IReadOnlyCollection<OutputPixel> pixels;
                    pixels = this.effect.GetPixels();
                    this.effectQueue.Enqueue(pixels);
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }

        private void Render()
        {
            var taskQueue = new Queue<Task<byte[]>>();
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (this.renderedQueue.Count < RenderQueueLength)
                {
                    outputTooSlow = false;
                    if (taskQueue.Count < RenderThreadCount)
                    {
                        if (!this.effectQueue.TryDequeue(out var pixels))
                        {
                            effectTooSlow = true;
                            continue;
                        }
                        effectTooSlow = false;
                        taskQueue.Enqueue(Task.Run(() => this.renderService.PreRender(pixels)));
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

        private const int _numSamples = 1000;
        private Queue<Tuple<DateTime, int>> _diagSamples = new Queue<Tuple<DateTime, int>>();

        private void DoOutput(IOutputService outputService)
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (this.renderedQueue.TryDequeue(out var rendered))
                {
                    renderTooSlow = false;
                    outputService.Output(rendered);
                    var sentBytes = rendered.Length;
                    lock (_diagSamples)
                    {
                        _diagSamples.Enqueue(new Tuple<DateTime, int>(DateTime.UtcNow, sentBytes));

                        if (_diagSamples.Count > _numSamples)
                        {
                            _diagSamples.Dequeue();
                        }
                    }
                }
                else
                {
                    renderTooSlow = true;
                    //Console.WriteLine("Output buffer Empty!");
                    Thread.Sleep(1);
                }
            }
        }

        public IList<KeyValuePair<string, string>> GetOutputDiagnostics()
        {
            var toReturn = new List<KeyValuePair<string, string>>();

            IList<Tuple<DateTime, int>> diagSamples;
            lock (_diagSamples)
            {
                diagSamples = _diagSamples.ToList();
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

                Console.WriteLine(string.Join(", ", diagnostics.Select(x => $"{x.Key}: {x.Value}")));
                Thread.Sleep(500);
            }
        }

        public void SetEffect(IEffect effect)
        {
            effect.Init(this.outputService.GetSize());
            this.effect = effect;
        }
    }
}