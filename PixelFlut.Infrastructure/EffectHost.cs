using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PixelFlut.Infrastructure
{
    public class EffectHost<TRendered>
    {
        private const int EffectQueueLength = 20;
        private const int RenderQueueLength = 20;
        private const int RenderThreadCount = 2;

        private IEffect effect;
        private object effectLock = new object();

        private Thread effectThread;
        private Thread renderThread;
        private Thread outputThread;
        private Thread logThread;
        private CancellationTokenSource cancellationTokenSource;
        private readonly IRenderOutputService<TRendered> outputService;

        private ConcurrentQueue<IReadOnlyCollection<OutputPixel>> effectQueue = new ConcurrentQueue<IReadOnlyCollection<OutputPixel>>();
        private ConcurrentQueue<TRendered> renderedQueue = new ConcurrentQueue<TRendered>();

        public EffectHost(IRenderOutputService<TRendered> outputService)
        {
            this.outputService = outputService;
        }

        public void Start()
        {
            this.cancellationTokenSource = new CancellationTokenSource();

            this.effectThread = new Thread(Effect);
            this.effectThread.Start();

            this.renderThread = new Thread(Render);
            this.renderThread.Start();

            this.outputThread = new Thread(Output);
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
                    lock (this.effectLock)
                    {
                        pixels = this.effect.GetPixels();
                    }
                    this.effectQueue.Enqueue(pixels);
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }

        private void Render()
        {
            var taskQueue = new Queue<Task<TRendered>>();
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (this.renderedQueue.Count < RenderQueueLength)
                {
                    if (taskQueue.Count < RenderThreadCount)
                    {
                        if (!this.effectQueue.TryDequeue(out var pixels))
                        {
                            continue;
                        }
                        taskQueue.Enqueue(Task.Run(() => this.outputService.PreRender(pixels)));
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
                    Thread.Sleep(10);
                }
            }
        }

        private void Output()
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (this.renderedQueue.TryDequeue(out var rendered))
                {
                    this.outputService.Output(rendered);
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }

        private void Log()
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                var diagnostics = new List<KeyValuePair<string, string>>();
                diagnostics.Add(new KeyValuePair<string, string>("RenderBuf", this.effectQueue.Count.ToString("D3", CultureInfo.InvariantCulture)));
                diagnostics.Add(new KeyValuePair<string, string>("OutBuf", this.renderedQueue.Count.ToString("D3", CultureInfo.InvariantCulture)));
                diagnostics.AddRange(this.outputService.GetDiagnostics());

                Console.WriteLine(string.Join(", ", diagnostics.Select(x => $"{x.Key}: {x.Value}")));
                Thread.Sleep(500);
            }
        }

        public void SetEffect(IEffect effect)
        {
            lock (effectLock)
            {
                effect.Init(this.outputService.GetSize());
                this.effect = effect;
            }
        }
    }
}