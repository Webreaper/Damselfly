using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Damselfly.Core.Utils
{
    public class ConcurrentPriorityQueue<T> where T : class
    {
        SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        private PriorityQueue<T, int> _queue = new PriorityQueue<T, int>();

        public void Enqueue( T obj, int priority )
        {
            lock( _queue )
            { 
                _queue.Enqueue(obj, priority);
            }
        }

        public T TryDequeue()
        {
            lock( _queue )
            {
                if (_queue.TryDequeue(out T obj, out var _))
                    return obj;
            }

            return default(T);
        }
    }
}

