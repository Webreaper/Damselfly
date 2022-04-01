using System;
using System.Linq;
using System.Collections.Concurrent;
using Damselfly.Core.Interfaces;
using Damselfly.Core.Models;
using Damselfly.Core.Utils;
using System.Threading;

namespace Damselfly.Core.Services;

/// <summary>
/// Background processing service that is fed jobs, with various priorities,
/// from the services (indexing, exif keywords, AI, thumbnails, etc). It pulls
/// those jobs and processes them in priority order in the background.
/// 
/// This class also has the option to throttle CPU usage, so that the processor
/// won't get absolutely hammered.
/// </summary>
public class WorkService
{
    public enum JobStatus
    {
        Idle,
        Running,
        Paused,
        Disabled,
        Error
    }

    public class ServiceStatus
    {
        public string StatusText { get; set; } = "Initialising";
        public JobStatus Status { get; set; } = JobStatus.Idle;
        public int CPULevel { get; set; }
    };


#if DEBUG
    private const int jobFetchSleep = 10;
#else
    private const int jobFetchSleep = 30;
#endif

    // This is like an IRQ flag. If it's true, we check the queue 
    // for new entries rather than just ploughing through it. 
    private volatile bool _newJobsFlag = false;

    private readonly UniqueConcurrentPriorityQueue<IProcessJob, string> _jobQueue = new UniqueConcurrentPriorityQueue<IProcessJob, string>( x => x.Description, y => (int)y.Priority );
    private readonly ConcurrentBag<IProcessJobFactory> _jobSources = new ConcurrentBag<IProcessJobFactory>();
    private const int _maxQueueSize = 500;
    private CPULevelSettings _cpuSettings = new CPULevelSettings();

    public bool Paused { get; set; }
    public ServiceStatus Status { get; private set; } = new ServiceStatus();
    public event Action<ServiceStatus> OnStatusChanged;

    public WorkService( ConfigService configService )
    {
        _cpuSettings.Load(configService);
    }

    public void SetCPUSchedule( CPULevelSettings cpuSettings )
    {
        Logging.Log($"Work service updated with new CPU settings: {cpuSettings}");
        _cpuSettings = cpuSettings;
    }

    public void AddJobSource( IProcessJobFactory source )
    {
        Logging.Log($"Registered job processing source: {source.GetType().Name}");
        _jobSources.Add(source);
    }

    private void SetStatus(string newStatusText, JobStatus newStatus, int newCPULevel)
    {
        if( newStatusText != Status.StatusText || newStatus != Status.Status || newCPULevel != Status.CPULevel )
        {
            Status.StatusText = newStatusText;
            Status.Status = newStatus;
            Status.CPULevel = newCPULevel;
            OnStatusChanged?.Invoke(Status);
        }
    }

    public void StartService()
    {
        Logging.Log("Started Work service thread.");

        var thread = new Thread(new ThreadStart(ProcessJobs))
        {
            Name = "WorkThread",
            IsBackground = true,
            Priority = ThreadPriority.Lowest
        };
        thread.Start();
    }

    /// <summary>
    /// The thread loop for the job processing queue. Processes
    /// jobs in sequence - we never process jobs in parallel as
    /// it's too complex to avoid data integrity and concurrency
    /// problems (although we could perhaps allow that when a
    /// DB like PostGres is in use. For SQLite, definitely not.
    /// </summary>
    private void ProcessJobs()
    {
        while (true)
        {
            int cpuPercentage = _cpuSettings.CurrentCPULimit;

            if ( Paused || cpuPercentage == 0 )
            {
                if( Paused )
                    SetStatus("Paused", JobStatus.Paused, cpuPercentage);
                else
                    SetStatus("Disabled", JobStatus.Disabled, cpuPercentage);

                // Nothing to do, so have a kip.
                Thread.Sleep(jobFetchSleep * 1000);
                continue;
            }

            var getNewJobs = _newJobsFlag;
            _newJobsFlag = false;
            var item = _jobQueue.TryDequeue();

            if ( item != null )
            {
                ProcessJob(item, cpuPercentage);
            }
            else
            {
                // No job to process, so we want to grab more
                getNewJobs = true;
            }

            // See if there's any higher-priority jobs to process
            if ( getNewJobs && ! PopulateJobQueue() )
            {
                if (_jobQueue.IsEmpty)
                {

                    // Nothing to do, so set the status to idle, and have a kip.
                    SetStatus("Idle", JobStatus.Idle, cpuPercentage);
                    Thread.Sleep(jobFetchSleep * 1000);
                }
            }
        }
    }

    /// <summary>
    /// Callback to notify the work service to look for new jobs. 
    /// Will be called async, from another thread, and should
    /// process the PopulateJobs method on another thread and
    /// return immediately. 
    /// </summary>
    /// <param name="source"></param>
    /// <param name="waitSeconds"></param>
    public void FlagNewJobs( IProcessJobFactory source )
    {
        Logging.Log($"Flagging new jobs state for {source.GetType().Name}");

        Stopwatch watch = new Stopwatch("FlagNewJobs");

        _newJobsFlag = true;

        watch.Stop();
    }

    /// <summary>
    /// Check with the work providers and see if there's any work to do.
    /// </summary>
    /// <returns></returns>
    private bool PopulateJobQueue()
    {
        Logging.LogVerbose("Looking for new jobs...");

        Stopwatch watch = new Stopwatch("PopulateJobQueue");

        bool newJobs = false;

        foreach (var source in _jobSources.OrderBy( x => x.Priority ) )
        {
            if (PopulateJobsForService(source, _maxQueueSize - _jobSources.Count))
                newJobs = true;
        }

        watch.Stop();

        return newJobs;
    }

    /// <summary>
    /// For the given service, checks for new jobs that might be 
    /// processed, and adds any that are found into the queue.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="maxCount"></param>
    /// <returns></returns>
    private bool PopulateJobsForService(IProcessJobFactory source, int maxCount)
    {
        bool newJobs = false;

        Stopwatch watch = new Stopwatch("PopulateJobsForService");

        if (maxCount > 0)
        {
            try
            {
                var jobs = source.GetPendingJobs(maxCount).Result;

                foreach (var job in jobs)
                {
                    if( _jobQueue.TryAdd( job ) )
                        newJobs = true;
                }
            }
            catch (Exception ex)
            {
                Logging.LogError($"Exception getting jobs: {ex.Message}");
            }
        }

        watch.Stop();

        return newJobs;
    }

    /// <summary>
    /// Do the actual work in processing a task in the queue
    /// </summary>
    /// <param name="job"></param>
    /// <param name="cpuPercentage"></param>
    /// <returns></returns>
    private void ProcessJob(IProcessJob job, int cpuPercentage)
    {
        // If we can't process, we'll discard this job, and pick it 
        // up again in future during the next GetPendingJobs call.
        if( job.CanProcess )
        {
            string jobName = job.GetType().Name;

            SetStatus($"{job.Name}", JobStatus.Running, cpuPercentage);

            Logging.LogVerbose($"Processing job type: {jobName}");

            Stopwatch stopwatch = new Stopwatch($"ProcessJob{jobName}");
            try
            {
                job.Process().Wait();
            }
            catch( Exception ex )
            {
                Logging.LogError($"Exception processing {job.Description}: {ex.Message}");
            }
            finally
            {
                stopwatch.Stop();
            }

            // Now, decide how much we need to sleep, in order to throttle CPU to the desired percentage
            // E.g., if the job took 2.5s to execute, then in order to maintain 25% CPU usage, we need to
            // sleep for 7.5s. Similarly, if the job took 0.5s, and we want to maintain 75% CPU usage,
            // we'd sleep for 0.33s.
            double sleepFactor = (1.0 / (cpuPercentage / 100.0)) - 1;

            if (sleepFactor > 0)
            {
                // Never ever sleep for more than 10s. Otherwise a long-running job that takes a minute
                // to complete could end up freezing the worker thread for 3 minutes, which makes no
                // sense whatsoeever. :)
                const int maxWaitTime = 10 * 1000;
                int waitTime = Math.Min( (int)(sleepFactor * stopwatch.ElapsedTime), maxWaitTime);
                Logging.LogVerbose($"Job '{jobName}' took {stopwatch.ElapsedTime}ms, so sleeping {waitTime} to give {cpuPercentage}% CPU usage.");
                Thread.Sleep(waitTime);
            }
        }
    }
}

