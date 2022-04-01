using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Damselfly.Core.Utils;
using Damselfly.Core.Models;
using System.Threading.Tasks;

namespace Damselfly.Core.Services;

/// <summary>
/// Service to manage scheduled tasks. Each task has a nextRun property, and the
/// service periodically triggers, checks if there are any tasks scheduled to run
/// and if so, runs them. We support the concept of task exclusivity so that
/// certain tasks can be made mutually exclusive. For example, we don't want to
/// run an incremental indexing task while a full indexing task is in progress.
/// </summary>
public class TaskService
{
    private Timer timer;
    private readonly TimeSpan timerFreq = new TimeSpan(0, 1, 0); // Every minute
    private readonly List<ScheduledTask> taskDefinitions = new List<ScheduledTask>();
    private readonly Queue<ScheduledTask> taskQueue = new Queue<ScheduledTask>();
    private readonly IDictionary<ScheduledTask.TaskType, Thread> runningTasks = new Dictionary<ScheduledTask.TaskType, Thread>();

    private readonly object runningTaskLock = new object();

    public TaskService()
    {
        Logging.Log("Task scheduler started.");
    }

    /// <summary>
    /// Create the base timer that will be used to cycle checking the scheduled tasks.
    /// We'll check for task statuses every minute.
    /// </summary>
    public void Start()
    {
        // Don't trigger an immediate process on startup - give everything a chance
        // to initialise and settle down. Wait a cycle and then start checking/working
        timer = new Timer(ProcessPendingTasks, null, timerFreq, timerFreq);
    }

    /// <summary>
    /// Get a list of tasks and statuses (so we can display some status
    /// in the UI).
    /// </summary>
    /// <returns></returns>
    public Task<ScheduledTask[]> GetTasksAsync()
    {
        lock( runningTaskLock )
        {
            var tasks = taskDefinitions.ToArray();
            return Task.FromResult(tasks);
        }
    }

    /// <summary>
    /// Adds a scheduled task to the queue of tasks that is pending being
    /// executed. This is basically the method that says "time to run this,
    /// so add it to the queue". Note that tasks may not always get executed
    /// immediately, they just go into the queue and get picked up when we
    /// have cycles.
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    public Task<bool> EnqueueTaskAsync(ScheduledTask task)
    {
        bool result = EnqueueTask(task);

        if (result)
        {
            // Now process the tasks
            ProcessTaskQueue();
        }

        return Task.FromResult(result);
    }

    /// <summary>
    /// Adds a task definition to the list of tasks that could be executed.
    /// Some basic checks for duplicates, and we also see if the task is
    /// scheduled for immediate start. If so, we set its NextRun to now.
    /// Otherwise, we just tee it up to get executed in the future.
    /// </summary>
    /// <param name="task"></param>
    public void AddTaskDefinition(ScheduledTask task)
    {
        Logging.Log("Adding scheduled task: {0} every {1}", task.Type, task.ExecutionFrequency.ToHumanReadableString());

        if (taskDefinitions.Any(x => x.Type == task.Type))
        {
            Logging.LogError("Duplicate task {0} added! Logic error.", task.Type);
            throw new Exception("Logic exception - duplicate scheduled task");
        }

        if (task.ImmediateStart)
            task.NextRun = DateTime.UtcNow; 
        else
            task.NextRun = DateTime.UtcNow + task.ExecutionFrequency;

        taskDefinitions.Add(task);
    }

    /// <summary>
    /// Our task scheduler execution.
    /// </summary>
    /// <param name="state"></param>
    private void ProcessPendingTasks(object state)
    {
        // See if we have any tasks that need to be scheduled
        QueuePendingTasks();

        // Now process the tasks
        ProcessTaskQueue();

        foreach( var job in runningTasks )
        {
            if (job.Value.IsAlive)
                Logging.LogTrace("Status: Task {0} is still running.", job.Key);
        }
    }

    /// <summary>
    /// Checks what tasks are due to be scheduled and adds them to the queue
    /// to be processed. 
    /// </summary>
    private void QueuePendingTasks()
    {
        foreach (var task in taskDefinitions)
        {
            Logging.LogTrace($"Checking NextRun for task: {task}");

            // Have we passed the due time? Do it! 
            if ( task.NextRun <= DateTime.UtcNow )
            {
                EnqueueTask(task);
            }
        }
    }

    /// <summary>
    /// Add the task to the queue - but only if it's not currently running.
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    private bool EnqueueTask(ScheduledTask task)
    {
        bool enqueued = false;

        // If the task isn't already running, and hasn't been queued to run, enqueue it
        if (!TaskIsRunning(task.Type) && !taskQueue.Where(x => x.Type == task.Type).Any())
        {
            taskQueue.Enqueue(task);
            Logging.LogVerbose("Task {0} enqueued", task.Type);
            enqueued = true;
        }
        else
        {
            // Todo - skipped one because it's running. Do we schedule again straight after?
            Logging.LogTrace("Task {0} already running or enqueued to run", task.Type);
        }

        return enqueued;
    }

    /// <summary>
    /// Works through the list of queued tasks, and spins up a new named thread
    /// to execute each one.
    /// </summary>
    private void ProcessTaskQueue()
    {
        while (taskQueue.Any())
        {
            var task = taskQueue.Dequeue();
            var thread = new Thread(new ThreadStart( () => { ExecuteMethod(task); } ) );
            thread.Name = $"{task.Type}Thread";
            thread.IsBackground = true;
            thread.Priority = ThreadPriority.Lowest;

            lock (runningTaskLock)
            {
                if (runningTasks.Keys.Intersect(task.ExclusiveToTasks).Any())
                {
                    string avoid = string.Join(", ", task.ExclusiveToTasks);
                    Logging.LogVerbose($"One or more exclusive tasks is running ({avoid}). Skipping execution of {task.Type}");
                }
                else
                {
                    task.LastStarted = DateTime.UtcNow;

                    // TODO: Should the next run be now + freq, or should it be
                    // from the time the task completes?
                    task.NextRun = DateTime.UtcNow + task.ExecutionFrequency;

                    runningTasks.Add(task.Type, thread);
                    Logging.LogVerbose($"Starting thread for task \"{task.Type}\"");
                    thread.Start();
                }
            }
        }
    }

    /// <summary>
    /// When a task completes, this callback is executed.
    /// </summary>
    /// <param name="task"></param>
    private void TaskCompleted( ScheduledTask task )
    {
        lock (runningTaskLock)
        {
            // Remove the task from the collection
            runningTasks.Remove(task.Type);

            task.LastCompleted = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Helper to access a task status and see if it's running.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private bool TaskIsRunning( ScheduledTask.TaskType type )
    {
        lock (runningTaskLock)
        {
            return runningTasks.ContainsKey(type);
        }
    }

    /// <summary>
    /// Executes the actual work method. This is always run on a
    /// named background thread that's been allocated for this
    /// task type.
    /// </summary>
    /// <param name="task"></param>
    private void ExecuteMethod(ScheduledTask task)
    {
        try
        {
            if (task.WorkMethod != null)
            {
                Logging.LogVerbose("Starting execution for task {0}", task.Type);
                task.WorkMethod();
            }
            else
            {
                // Shouldn't ever happen, but be careful
                Logging.LogWarning($"Task {task.Type} did not have a work method.");
            }
        }
        catch (Exception ex)
        {
            Logging.LogError("Exception while executing task {0}: {1}", task.Type, ex.Message);
        }
        finally
        {
            Logging.LogVerbose("Completed execution for task {0}.", task.Type);

            TaskCompleted(task);
            Stopwatch.WriteTotals();
        }
    }

    /// <summary>
    /// Close ourselves down; remove all of the threads and clear any running tasks.
    /// </summary>
    public void Shutdown()
    {
        timer.Dispose();

        lock( runningTaskLock )
        {
            foreach( var task in runningTasks )
            {
                Logging.Log($"Terminating thread for {task.Key}...");
                // TODO - use cancellation token to end the thread.
            }

            runningTasks.Clear();
        }
    }
}
