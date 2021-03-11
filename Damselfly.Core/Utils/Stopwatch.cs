using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Damselfly.Core.Utils
{
    /// <summary>
    /// Timer class for tracking performance. Keeps a running stat of the max times
    /// for each named operation, and the average times for all instances of a named
    /// operation.
    /// </summary>
    public class Stopwatch
    {
        private class Totals
        {
            public long count;
            public long totalTime;

            public long AverageTime { get { return (long)(((double)totalTime) / count);  } }
        }

        private static IDictionary<string, Totals> averages = new ConcurrentDictionary<string, Totals>(StringComparer.OrdinalIgnoreCase);
        private static IDictionary<string, long> maximums = new ConcurrentDictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        private int taskThresholdMS = -1;
        private string timername;
        private long start;
        private long end;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Name of the work being timed</param>
        /// <param name="thresholdMS">Threshold, in milliseconds, ovr which we should
        /// log a message stating that the work took an unexpectedly long time to complete.</param>
        public Stopwatch( string name, int thresholdMS = 1000)
        {
            this.taskThresholdMS = thresholdMS;

            timername = name;
            start = Environment.TickCount;
        }

        /// <summary>
        /// Terminate the stopwatch, add the average/max to the collection of timers,
        /// and log if the job took longer than the expected threshold.
        /// </summary>
        public void Stop()
        {
            end = Environment.TickCount;

            long time = end - start;
            Logging.LogTrace("Time taken for {0}: {1}ms", timername, time);

            if( taskThresholdMS > 0 && time > taskThresholdMS )
            {
                Logging.LogVerbose($"Stopwatch: task {timername} took {time}ms (threshold {taskThresholdMS}ms).");
            }

            Totals total;
            if (!averages.TryGetValue(timername, out total))
            {
                total = new Totals { count = 1, totalTime = time };
                averages[timername] = total;
            }
            else
            {
                total.count++;
                total.totalTime += time;
            }

            if (! maximums.ContainsKey(timername) || maximums[timername] < time)
                maximums[timername] = time;
        }

        public long ElapsedTime { get { return end - start; } }
        public string HumanElapsedTime { get { return (end - start).ToHumanReadableString(); } }
        public override string ToString() => $"{ElapsedTime}";

        public static void WriteTotals()
        {
            try
            {
                Logging.LogVerbose("Performance Summary:");
                foreach (var kvp in averages.OrderBy(x => x.Key))
                    Logging.LogVerbose("  Avg {0}: {1}ms", kvp.Key, kvp.Value.AverageTime);

                foreach (var kvp in maximums.OrderBy(x => x.Key))
                    Logging.LogVerbose("  Max {0}: {1}ms", kvp.Key, kvp.Value);
            }
            catch (Exception)
            {
                Logging.LogVerbose("Unable to write stopwatch totals.");
            }
        }
    }
}
