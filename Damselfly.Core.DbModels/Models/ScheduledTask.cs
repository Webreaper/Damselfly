using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Damselfly.Core.Constants;
using Damselfly.Shared.Utils;

namespace Damselfly.Core.Models;

/// <summary>
///     Class representing a Task that'll be executed by the scheduler.
/// </summary>
public class ScheduledTask
{
    public TaskType Type { get; set; }
    public DateTime LastStarted { get; set; } = DateTime.UtcNow;
    public DateTime LastCompleted { get; set; } = DateTime.MinValue;
    public DateTime NextRun { get; set; } = DateTime.MinValue;
    public TimeSpan ExecutionFrequency { get; set; }
    public bool ImmediateStart { get; set; } = false;

    public List<TaskType> ExclusiveToTasks { get; } = new();

    [JsonIgnore] public Action WorkMethod { get; set; }

    public override string ToString()
    {
        var lastRun = LastStarted == DateTime.MinValue ? "never" : LastStarted.ToString();
        return $"{Type} (Freq: {ExecutionFrequency.ToHumanReadableString()} Last run: {lastRun})";
    }
}