using System;
using System.Collections.Generic;
using Damselfly.Core.Utils;

namespace Damselfly.Core.Models
{
    /// <summary>
    /// Class representing a Task that'll be executed by the scheduler.
    /// </summary>
    public class ScheduledTask
    {
        public enum TaskType
        {
            FullIndex,
            IncrementalIndex,
            FullThumbnailGen,
            IncrementalThumbnailGen,
            CleanupDownloads,
            CleanupKeywordOps,
            ThumbnailCleanup,
            MetadataScan,
            FlushDBWriteCache,
            DumpPerformance,
            CleanupThumbs,
            FreeTextIndex
        }

        public TaskType Type { get; set; }
        public DateTime LastStarted { get; set; } = DateTime.UtcNow;
        public DateTime LastCompleted { get; set; } = DateTime.MinValue;
        public DateTime NextRun { get; set; } = DateTime.MinValue;
        public TimeSpan ExecutionFrequency { get; set; }
        public bool ImmediateStart { get; set; } = false;

        public List<TaskType> ExclusiveToTasks { get; } = new List<TaskType>();
        public Action WorkMethod { get; set; }

        public override string ToString()
        {
            string lastRun = LastStarted == DateTime.MinValue ? "never" : LastStarted.ToString();
            return $"{Type} (Freq: {ExecutionFrequency.ToHumanReadableString()} Last run: {lastRun})";
        }
    }
}
