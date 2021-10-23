using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Damselfly.Core.Interfaces;
using Damselfly.Core.Models;
using Damselfly.Core.Services;
using Damselfly.Core.Utils;
using System.Threading;

namespace Damselfly.Core.Services
{
    public class WorkService
    {
        private readonly ConcurrentQueue<IProcessJob> _jobQueue = new ConcurrentQueue<IProcessJob>();

        private readonly StatusService _statusService;
        private readonly ImageCache _imageCache;

        // Setting this to > 1 may improve performance if we're IO-bound
        const int maxThreads = 1;

        private readonly List<IProcessJobFactory> _jobSources = new List<IProcessJobFactory>();

        public WorkService(StatusService statusService, ImageRecognitionService aiService,
                            ThumbnailService thumbService, IndexingService indexingService,
                            MetaDataService metadataService, ExifService exifService,
                            ImageCache imageCache)
        {
            _imageCache = imageCache;
            _statusService = statusService;

            _jobSources.Add(indexingService);
            _jobSources.Add(metadataService);
            _jobSources.Add(thumbService);
            //_jobSources.Add(exifService);
            _jobSources.Add(aiService);
        }

        private string _statusText = string.Empty;
        private const int _maxQueueSize = 2000;

        public event Action<string> OnChange;

        private void SetStatus(string newStatus)
        {
            OnChange?.Invoke(newStatus);
        }

        public void StartService()
        {
            Logging.Log("Started Work service.");

            var thread = new Thread(new ThreadStart(WorkerThread));
            thread.Name = "WorkThread";
            thread.IsBackground = true;
            thread.Priority = ThreadPriority.Lowest;
            thread.Start();
        }

        private void WorkerThread()
        {
            while (true)
            {
                ProcessJobs().Wait();
            }
        }

        // Job may be an individua,image (thumbnails, AI) or a batch of
        // many images (indexing, for keyword efficiency?)?
        // Context menu on Status to pause the queue processing. 

        private async Task ProcessJobs()
        {
            await PopulateJobQueue();

            // Need to refill the queue while consuming it. 
            await _jobQueue.ExecuteInParallel(async job => await ProcessJob(job), maxThreads);

            await Task.Delay(5 * 1000);
        }

        private async Task PopulateJobQueue()
        {
            foreach (var service in _jobSources)
            {
                try
                {
                    var jobs = await service.GetPendingJobs(_maxQueueSize - _jobSources.Count);

                    foreach (var job in jobs)
                        _jobQueue.Enqueue(job);
                }
                catch ( Exception ex )
                {
                    Logging.LogError($"Exception getting jobs...");
                }
            }
        }

        /// <summary>
        /// Do the actual work in processing a task in the queue
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        private async Task ProcessJob(IProcessJob job)
        {
            // If we can't process, we'll discard this job, and pick it 
            // up again in future during the next GetPendingJobs call.
            if (job.CanProcess)
            {
                await job.Process();

                SetStatus($"Queue: {_jobQueue.Count}");
            }
        }
    }
}

