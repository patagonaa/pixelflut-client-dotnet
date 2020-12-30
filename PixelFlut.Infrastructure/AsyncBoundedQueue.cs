using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PixelFlut.Infrastructure
{
    class AsyncBoundedQueue<T>
    {
        private readonly SemaphoreSlim _semaphoreAvailable;
        private readonly SemaphoreSlim _semaphoreOccupied;
        private readonly CancellationTokenSource _cts;
        private readonly ConcurrentQueue<T> _collection;

        public AsyncBoundedQueue(int capacity)
        {
            _collection = new ConcurrentQueue<T>();
            _semaphoreAvailable = new SemaphoreSlim(capacity);
            _semaphoreOccupied = new SemaphoreSlim(0);
            _cts = new CancellationTokenSource();
        }

        public bool IsCompleted { get; private set; }

        public bool IsAddingCompleted { get; private set; }

        public int Count => _collection.Count;

        public void CompleteAdding()
        {
            IsAddingCompleted = true;
            while (_semaphoreOccupied.CurrentCount == 0)
            {
                _semaphoreOccupied.Release();
            }
        }

        public async Task<T> DequeueOrDefault()
        {
            try
            {
                await _semaphoreOccupied.WaitAsync(_cts.Token).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                return default;
            }

            if (IsCompleted || !_collection.TryDequeue(out T item))
            {
                if (IsAddingCompleted)
                {
                    IsCompleted = true;
                    _cts.Cancel();
                    return default;
                }

                throw new Exception("something went wrong");
            }

            _semaphoreAvailable.Release();
            return item;
        }

        public async IAsyncEnumerable<T> GetConsumingEnumerable()
        {
            while (!IsCompleted)
            {
                var toReturn = await DequeueOrDefault();
                if (object.Equals(toReturn, default(T)))
                    yield break;
                yield return toReturn;
            }
        }

        public async Task Enqueue(T item)
        {
            await _semaphoreAvailable.WaitAsync();
            _collection.Enqueue(item);
            _semaphoreOccupied.Release();
        }
    }
}
