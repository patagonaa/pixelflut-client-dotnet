using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PixelFlut.Infrastructure
{
    public class EffectHost<TRendered>
    {
        private const int EffectQueueLength = 10;
        private const int RenderQueueLength = 10;

        private IEffect effect;
        private object effectLock = new object();

        private Thread effectThread;
        private Thread renderThread;
        private Thread outputThread;
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
                    Thread.Sleep(100);
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
                    if (!this.effectQueue.TryDequeue(out var pixels))
                    {
                        Console.WriteLine("render buffer empty!");
                        continue;
                    }

                    if (taskQueue.Count < 4)
                    {
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
                    Thread.Sleep(100);
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
                    Console.WriteLine("output buffer empty!");
                    Thread.Sleep(100);
                }
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