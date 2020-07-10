using PixelFlut.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PixelFlut.Demo.Filters
{
    class FramePaceFilter : IFilter
    {
        private const int NumSamples = 10;
        private readonly int targetFps;
        private Queue<DateTime> _samples = new Queue<DateTime>();

        public FramePaceFilter(int targetFps)
        {
            this.targetFps = targetFps;
        }

        public OutputFrame ApplyFilter(OutputFrame frame)
        {
            lock (_samples)
            {
                _samples.Enqueue(DateTime.UtcNow);

                if (_samples.Count > NumSamples)
                {
                    _samples.Dequeue();
                }
            }
            if (GetFps() > this.targetFps)
                Thread.Sleep(1000 / targetFps);

            return frame;
        }

        private double GetFps()
        {
            if (_samples.Count < 2)
                return this.targetFps;

            List<DateTime> samples;
            lock (_samples)
            {
                samples = _samples.ToList();
            }

            var minDate = samples.Min();
            var maxDate = samples.Max();

            var diff = maxDate - minDate;

            return samples.Count / diff.TotalSeconds;
        }

        public void Dispose()
        {
        }
    }
}
