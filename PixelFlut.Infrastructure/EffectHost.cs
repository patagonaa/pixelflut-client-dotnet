using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace PixelFlut.Infrastructure
{
    public class EffectHost<TRendered>
    {
        private IEffect effect;
        private object effectLock = new object();

        private Thread effectThread;
        private Thread renderThread;
        private Thread outputThread;
        private CancellationTokenSource cancellationTokenSource;
        private readonly IRenderOutputService<TRendered> outputService;

        private ConcurrentQueue<IList<OutputPixel>> effectQueue = new ConcurrentQueue<IList<OutputPixel>>();
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
                if (this.effectQueue.Count < 100)
                {
                    IList<OutputPixel> pixels;
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
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                IList<OutputPixel> pixels;
                if (this.renderedQueue.Count < 100)
                {
                    if(!this.effectQueue.TryDequeue(out pixels)){
                        Console.WriteLine("render buffer empty!");
                        continue;
                    }

                    var rendered = this.outputService.PreRender(pixels);
                    this.renderedQueue.Enqueue(rendered);
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

    public interface IEffect
    {
        IList<OutputPixel> GetPixels();
        void Init(Size canvasSize);
    }

    public abstract class EffectBase : IEffect
    {
        public void Init(Size canvasSize)
        {
            CanvasSize = canvasSize;
            this.initialized = true;
        }

        public IList<OutputPixel> GetPixels()
        {
            if (!this.initialized)
                throw new InvalidOperationException("not initialized!");
            TickInternal();
            Counter++;
            var pixels = this.pixels.ToList();
            this.pixels.Clear();
            return pixels;
        }

        protected abstract void TickInternal();

        private List<OutputPixel> pixels = new List<OutputPixel>();

        protected Size CanvasSize { get; private set; }

        private bool initialized;

        protected int Counter { get; private set; }
        protected void DrawPixel(Point point, Color color)
        {
            lock (this.pixels)
            {
                this.pixels.Add(new OutputPixel(point, color));
            }
        }
    }

    public struct OutputPixel
    {
        public OutputPixel(Point point, Color color)
        {
            Point = point;
            Color = color;
        }

        public Point Point { get; }
        public Color Color { get; }
    }

    public class RandomBoxes : EffectBase
    {
        private readonly Size boxSize;
        private readonly Random random;

        public RandomBoxes(Size boxSize)
        {
            this.boxSize = boxSize;
            this.random = new Random();
        }

        protected override void TickInternal()
        {
            var maxOffsetX = CanvasSize.Width - boxSize.Width;
            var maxOffsetY = CanvasSize.Height - boxSize.Height;

            var color = Color.FromArgb((int)(random.Next() | 0xFF000000));
            var offsetX = this.random.Next(0, maxOffsetX);
            var offsetY = this.random.Next(0, maxOffsetY);

            for (int y = 0; y < boxSize.Height; y++)
            {
                for (int x = 0; x < boxSize.Width; x++)
                {
                    var pos = new Point(offsetX + x, offsetY + y);
                    DrawPixel(pos, color);
                }
            }
        }
    }

    public interface IRenderOutputService<TRendered>
    {
        TRendered PreRender(IList<OutputPixel> pixels);
        void Output(TRendered rendered);
        Size GetSize();
    }

    public class PixelFlutRenderOutputService : IRenderOutputService<byte[]>
    {
        private Socket client;
        private readonly IPEndPoint endPoint;

        public PixelFlutRenderOutputService(IPEndPoint endPoint)
        {
            this.client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.endPoint = endPoint;
        }

        public void Output(byte[] rendered)
        {
            if (!this.client.Connected)
            {
                this.client.Connect(this.endPoint);
            }

            try
            {
                this.client.Send(rendered);
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Connection fucked: {ex.SocketErrorCode}\n{ex}");
                if (ex.SocketErrorCode == SocketError.Shutdown ||
                    ex.SocketErrorCode == SocketError.ConnectionAborted ||
                    ex.SocketErrorCode == SocketError.TimedOut)
                {
                    this.client.Dispose();
                    this.client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    this.client.Connect(this.endPoint);
                }
            }
        }

        private byte[] buffer = null;

        public byte[] PreRender(IList<OutputPixel> pixels)
        {
            if ((buffer?.Length ?? 0) < pixels.Count * 20)
            {
                buffer = new byte[pixels.Count * 20];
            }

            using (var ms = new MemoryStream(buffer))
            {
                using (var sw = new StreamWriter(ms))
                {
                    sw.NewLine = "\n";
                    foreach (var pixel in pixels)
                    {
                        if (pixel.Color.A != 255)
                        {
                            sw.WriteLine($"PX {pixel.Point.X} {pixel.Point.Y} {pixel.Color.R:X2}{pixel.Color.G:X2}{pixel.Color.B:X2}{pixel.Color.A:X2}");
                        }
                        else
                        {
                            sw.Write($"PX ");
                            sw.Write(pixel.Point.X);
                            sw.Write(" ");
                            sw.Write(pixel.Point.Y);
                            sw.Write(" ");
                            sw.Write(pixel.Color.R.ToString("X2"));
                            sw.Write(pixel.Color.G.ToString("X2"));
                            sw.Write(pixel.Color.B.ToString("X2"));
                            sw.WriteLine();
                        }
                    }
                    sw.Flush();
                    var toReturn = new byte[ms.Position];
                    Array.Copy(this.buffer, toReturn, ms.Position);
                    return toReturn;
                }
            }
        }

        public Size GetSize()
        {
            while (true)
            {
                var bytes = new byte[1000];
                int receivedBytes;
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect(this.endPoint);
                    socket.Send(Encoding.UTF8.GetBytes("SIZE\n"));

                    receivedBytes = socket.Receive(bytes);

                    socket.Close();
                }
                var str = Encoding.UTF8.GetString(bytes, 0, receivedBytes);
                var split = str.Split(' ');
                try
                {
                    return new Size(int.Parse(split[1]), int.Parse(split[2]));
                }
                catch (IndexOutOfRangeException)
                {
                    continue;
                }
            }
        }
    }
}