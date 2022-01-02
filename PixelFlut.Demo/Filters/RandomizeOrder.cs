using PixelFlut.Infrastructure;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PixelFlut.Demo.Filters
{
    class RandomizeOrder : IFilter
    {
        private readonly ThreadLocal<Random> _random = new ThreadLocal<Random>(() => new Random());

        public RandomizeOrder()
        {
        }

        public Task<OutputFrame> ApplyFilter(OutputFrame frame)
        {
            var random = _random.Value;
            var copy = frame.Pixels.ToArray();
            Shuffle(random, copy);
            return Task.FromResult(new OutputFrame(frame.OffsetX, frame.OffsetY, copy, frame.CacheId, frame.OffsetStatic));
        }

        private static void Shuffle<T>(Random rng, T[] array)
        {
            int n = array.Length;
            while (n > 1)
            {
                int k = rng.Next(n--);
                T temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
        }

        public void Dispose()
        {
            _random.Dispose();
        }
    }
}
