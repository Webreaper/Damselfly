using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

namespace Damselfly.Core.Utils
{
    /// <summary>
    /// Async tasks, by default, use a system threadpool to execute background work. There's
    /// two reasons we want to use our own scheduler when running background process jobs.
    /// 
    /// 1. We want to ensure all of these background tasks run at the lowest thread priority
    ///    to ensure that everything else in the process takes priority if we're low on CPU
    ///    cycles. We don't want a background thumbnail-gen job being priorised over UI
    ///    interaction.
    ///
    /// 2. We want a separate threadpool dedicated to background jobs that we're batch
    ///    processing, so that it has its own set of threads. Since Blazor UI work is
    ///    frequently done using Async methods, if we queue up hundreds of thumbnail gen
    ///    jobs, they'll starve the UI and it won't be responsive.
    ///
    /// So we create our own instance of TaskScheduler with its own threadpool, so that
    /// we can fling tons of work at it and it'll be executed separately to the rest of
    /// the app.
    /// </summary>
    public static class ThreadUtils
    {
        public static async Task ExecuteInParallel<T>(this IEnumerable<T> collection,
                                   Func<T, Task> processor,
                                   int degreeOfParallelism)
        {
            var queue = new ConcurrentQueue<T>(collection);
            var tasks = Enumerable.Range(0, degreeOfParallelism).Select(async _ =>
            {
                while (queue.TryDequeue(out var item))
                {
                    try
                    {
                        await processor(item);
                    }
                    catch( Exception ex )
                    {
                        Logging.LogError($"Exception during ExecuteInParallel: {ex.Message}");
                    }
                }
            });

            Logging.Log("Waiting for Parallel processing to complete...");

            await Task.WhenAll(tasks);

            Logging.Log("Parallel processing completed successfully.");
        }

        /// <summary>
        /// TaskScheduler implementation that allows the specification of the thread
        /// priorities, and which sets the number of threads based on the CPU arch.
        /// </summary>
        private class PriorityScheduler : TaskScheduler
        {
            private Thread[] _threads;
            private readonly int _maximumConcurrencyLevel = 1;
            private readonly BlockingCollection<Task> _tasks = new BlockingCollection<Task>();
            private readonly ThreadPriority _priority;

            public PriorityScheduler(ThreadPriority priority)
            {
                _priority = priority;

                // Default the max concurrency to the number of processors on the host. So if we're
                // on a box with a single processor, we'll just work through the jobs sequentionally.
                //_maximumConcurrencyLevel = Math.Max(1, Environment.ProcessorCount);
            }

            /// <summary>
            /// Set the max concurrency level for the job. 
            /// </summary>
            public override int MaximumConcurrencyLevel
            {
                get { return _maximumConcurrencyLevel; }
            }

            protected override IEnumerable<Task> GetScheduledTasks()
            {
                return _tasks;
            }

            protected override void QueueTask(Task task)
            {
                _tasks.Add(task);

                if (_threads == null)
                {
                    _threads = new Thread[_maximumConcurrencyLevel];
                    for (int i = 0; i < _threads.Length; i++)
                    {
                        int local = i;
                        _threads[i] = new Thread(() =>
                        {
                            foreach (Task t in _tasks.GetConsumingEnumerable())
                                TryExecuteTask(t);
                        })
                        {
                            Name = $"PriorityScheduler: {i}",
                            Priority = _priority,
                            IsBackground = true
                        };
                        Logging.Log($"Starting thread for Priority Scheuler {i}...");
                        _threads[i].Start();
                    }
                }
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                return false; // we might not want to execute task that should schedule as high or low priority inline
            }
        }

        private static readonly PriorityScheduler LowPriorityScheduler = new PriorityScheduler(ThreadPriority.Lowest);
        private const int s_yieldTime = 100;

        /// <summary>
        /// Consumer threadpool implementation. Takes an action and a collection of items, and
        /// processes them on the number of threads specified. 
        /// </summary>
        /// <typeparam name="T">Type of objects being processed</typeparam>
        /// <param name="targets">Collection of objects to process</param>
        /// <param name="process">The action to be processed for each item</param>
        /// <param name="maxThreads">Max number of threads to use in the pool.</param>
        /// <returns></returns>
        public static bool ProcessOnThreadPool<T>( this T[] targets, Action<T> process, int maxThreads)
        {
            var watch = new Stopwatch("ThreadPoolProcess");

            CountdownEvent threadCount = new CountdownEvent(maxThreads);
            var queue = new ConcurrentQueue<T>(targets);
            var tasks = new List<Task>();

            Logging.LogVerbose($"Processing {targets.Length } objects in {maxThreads} threads.");

            CancellationToken token = new CancellationToken();

            for (int i = 0; i < maxThreads; i++)
            {
                var task = Task.Factory.StartNew(() =>
                        {
                            // Straightforward consumer
                            while (queue.TryDequeue(out T target))
                            {
                                process(target);

                                // Yield to give the dispatcher time to react to stuff.
                                Thread.Sleep(s_yieldTime);
                            }
                        },
                        token, TaskCreationOptions.None, LowPriorityScheduler);
                tasks.Add(task);
            }

            try
            {
                Task.WaitAll(tasks.ToArray());
                Logging.LogVerbose($"Processed ({targets.Length} objects)");
                return true;
            }
            catch (Exception e)
            {
                Logging.LogError($"Exception during parallel conversion: {e}.");
            }
            finally
            {
                watch.Stop();
                if (targets.Length > 0)
                {
                    var avgTime = watch.ElapsedTime / targets.Length;
                    Logging.LogTrace($"Threadpool: Time taken for {targets.Length} items: {watch.ElapsedTime.ToHumanReadableString() }ms (avg: {avgTime})ms ");
                }
            }

            return false;
        }

        /// <summary>
        /// Creates a ConcurrentDictionary from a projection
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="source"></param>
        /// <param name="valueSelector"></param>
        /// <returns></returns>
        public static ConcurrentDictionary<TKey, TValue> ToConcurrentDictionary<TKey, TValue>
                            (this IEnumerable<TValue> source, Func<TValue, TKey> valueSelector)
        {
            return new ConcurrentDictionary<TKey, TValue>
                       (source.ToDictionary(valueSelector));
        }
    }
}
