using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Damselfly.Core.Utils;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

namespace Damselfly.ML.AzureFace
{
    /// <summary>
    /// A transaction queue for Azure. We maintain a rolling queue of executed
    /// Azure transactions, and when the next transaction is executed we see if
    /// there's more than maxTransactionsPerMin been executed in the last minute.
    /// If we've exceeded the trans/min, we calculate the amount of time we need
    /// to sleep until we can execute another one. So for example:
    ///    - We've executed the first in the queue 45 seconds ago.
    ///    - Since then, we've executed another 19 transactions
    ///    - We're at max capacity, so work out how long we have to sleep
    ///    - 1 minute minus 45 seconds means we sleep for 15 seconds.
    ///    - We discard the oldest transaction from the queue, as it's no
    ///      longer relevant in the context of our one-minute timeframe.
    ///    - At the point we can then execute another transaction, and add it
    ///      to the queue.
    /// This way, we execute as many transactions as possible, as quickly as
    /// possible, but without exceeding the rate limit.
    ///
    /// For this to work, *every* Azure API call has to have a call to
    /// WaitAfterTransaction as the very next call.
    /// </summary>
    public class TransThrottle
    {
        private class AzureTransaction
        {
            public DateTime timestamp { get; set; }
            public string description { get; set; }
            public override string ToString()
            {
                return $"{timestamp:HH:mm:ss} {description}";
            }
        }

        private volatile int _totalTransactions;
        private readonly int maxTransactionsPerMin;
        private ConcurrentQueue<AzureTransaction> transQueue = new ConcurrentQueue<AzureTransaction>();

        public TransThrottle( int maxTransPerMin = 20 )
        {
            maxTransactionsPerMin = maxTransPerMin;
        }

        private async Task WaitAfterTransaction(string desc)
        {
            // Now add our transaction
            transQueue.Enqueue( new AzureTransaction { timestamp = DateTime.UtcNow, description = desc } );
            Logging.LogTrace($"Executed {desc}; there are now {transQueue.Count} Azure transactions in current batch");
            _totalTransactions++;

            if (transQueue.Count == (maxTransactionsPerMin - 1))
            {
                // We've got 20 or more in the last minute.
                transQueue.TryDequeue(out var oldestTrans);

                var nextTransHappensAt = oldestTrans.timestamp.AddSeconds(61);
                var sleepTime = (int)(nextTransHappensAt - DateTime.UtcNow).TotalSeconds;

                if (sleepTime > 0)
                {
                    if (sleepTime > 10)
                    {
                        // Log for long delays.
                        Logging.Log($"Sleeping for {sleepTime}s to avoid Azure transaction throttle.");
                    }

                    await Task.Delay(sleepTime * 1000);
                }
            }
            else
            {
                // Always sleep one second, so that every trans is distinguised by a second timespan
                //await Task.Delay(1000);
            }
        }

        /// <summary>
        /// Wrapper for Face Service calls to manage throttling and retries
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="desc"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public async Task<T> Call<T>(string desc, Task<T> method)
        {
            T t = default(T);
            int retries = 3;

            while (retries-- > 0)
            {
                try
                {
                    t = await method;
                    await WaitAfterTransaction(desc);
                    retries = 0;
                }
                catch (APIErrorException ex)
                {
                    await HandleThrottleException(ex, retries);
                }
            }

            return t;
        }

        /// <summary>
        /// Wrapper for Face Service calls to manage throttling and retries
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="desc"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public async Task Run(string desc, Task method)
        {
            int retries = 3;

            while (retries-- > 0)
            {
                try
                {
                    await method;
                    await WaitAfterTransaction(desc);
                    retries = 0;
                }
                catch (APIErrorException ex)
                {
                    await HandleThrottleException(ex, retries);
                }
            }
        }

        private async Task HandleThrottleException( APIErrorException ex, int retriesRemaining )
        {
            if (ex.Response.Content.Contains("exceeded rate limit") && retriesRemaining > 0 )
            {
                Logging.LogWarning($"Azure throttle error: {ex.Response.Content}. Retrying {retriesRemaining} more times.");
                DumpQueue();
                await Task.Delay(3 * 1000);
            }
            else
                throw ex;
        }

        public int TotalTransactions { get { return _totalTransactions; } }
        public int ResetTotalTransactions() { int total = _totalTransactions;  _totalTransactions = 0; return total;  }

        internal void DumpQueue()
        {
            Logging.Log($"Azure Trans Queue ({transQueue.Count} items):");
            foreach( var x in transQueue )
            {
                Logging.Log($" {x}");
            }
        }
    }
}
