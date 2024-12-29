using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Damselfly.Core.Database;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Damselfly.Core.Services;

public class FolderWatcherService
{
    private static readonly UniqueConcurrentPriorityQueue<string, string> folderQueue = new( x => x );
    private readonly ImageProcessService _imageProcessService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IStatusService _statusService;

    private readonly IDictionary<string, FileSystemWatcher> _watchers =
        new Dictionary<string, FileSystemWatcher>(StringComparer.OrdinalIgnoreCase);

    private bool _fileWatchersDisabled;
    private IndexingService _indexingService;
    private Task _queueTask;

    public FolderWatcherService(IServiceScopeFactory scopeFactory,
        IStatusService statusService,
        ImageProcessService imageService)
    {
        _scopeFactory = scopeFactory;
        _statusService = statusService;
        _imageProcessService = imageService;

        // Start a thread which will periodically drain the queue
        _queueTask = Task.Run(FolderQueueProcessor);
    }

    public void LinkIndexingServiceInstance(IndexingService indexingService)
    {
        _indexingService = indexingService;
    }

    /// <summary>
    ///     Processor to drain the folder queue and update the DB. This
    /// </summary>
    /// <returns></returns>
    private async Task FolderQueueProcessor()
    {
        // Process the queue every 30s
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        while ( true )
        {
            // Wait until the next iteration
            await timer.WaitForNextTickAsync();

            // First, take all the queued folder changes and persist them to the DB
            // by setting the FolderScanDate to null.
            var folders = new List<string>();

            while ( folderQueue.TryDequeue(out var folder) )
            {
                Logging.Log($"Flagging change for folder: {folder}");
                folders.Add(folder);
            }

            if ( _indexingService != null && folders.Any() )
            {
                using var scope = _scopeFactory.CreateScope();
                using var db = scope.ServiceProvider.GetService<ImageContext>();

                var uniqueFolders = folders.Distinct(StringComparer.OrdinalIgnoreCase);
                var pendingFolders = await db.Folders.Where(f => uniqueFolders.Contains(f.Path))
                    .Select(x => x.FolderId)
                    .ToListAsync();

                // Call this method synchronously, we don't want to continue otherwise
                // we'll end up with race conditions as the timer triggers while
                // the method is completing. 
                _indexingService.MarkFoldersForScan(pendingFolders).Wait();
            }
        }
    }

    public void CreateFileWatcher(DirectoryInfo path)
    {
        if ( _fileWatchersDisabled )
            return;

        if ( !_watchers.ContainsKey(path.FullName) )
            try
            {
                var watcher = new FileSystemWatcher();

                Logging.LogVerbose($"Creating FileWatcher for {path}");

                watcher.Path = path.FullName;

                // Watch for changes in LastAccess and LastWrite
                // times, and the renaming of files.
                watcher.NotifyFilter = NotifyFilters.LastWrite
                                       | NotifyFilters.FileName
                                       | NotifyFilters.Size
                                       | NotifyFilters.DirectoryName;

                // Add event handlers.
                watcher.Changed += OnChanged;
                watcher.Created += OnChanged;
                watcher.Deleted += OnChanged;
                watcher.Renamed += OnRenamed;
                watcher.Error += WatcherError;

                // Store it in the map
                _watchers[path.FullName] = watcher;

                // Begin watching.
                watcher.EnableRaisingEvents = true;
            }
            catch ( Exception ex )
            {
                Logging.LogError($"Exception creating filewatcher for {path}: {ex.Message}");

                if ( ex.Message.Contains("process limit on the number of open file descriptors has been reached") )
                {
                    _fileWatchersDisabled = true;

                    _statusService.UpdateStatus(
                        "OS inotify/file watcher limit reached. See the installation guide on how to increase this.");

                    Logging.LogError(
                        @"OS inotify/ file-watcher limit reached. Damselfly cannot monitor any more folders for changes.");
                    Logging.LogError(
                        "You should increase the watcher limit - see this article: https://github.com/Webreaper/Damselfly/blob/master/docs/Installation.md#filewatcher-inotify-limits");
                }
            }
    }

    /// <summary>
    ///     Process disk-level inotify changes. Note that this should be *very*
    ///     fast to keep up with updates as they come in. So we put all distinct
    ///     changes into a queue and then return, and the queue contents will be
    ///     processed in batch later. This has the effect of us being able to
    ///     collect up a conflated list of actual changes with minimal blocking.
    /// </summary>
    /// <param name="file"></param>
    /// <param name="changeType"></param>
    private void EnqueueFolderChangeForRescan(FileInfo file, WatcherChangeTypes changeType)
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        var folder = file.Directory.FullName;

        if( changeType != WatcherChangeTypes.Deleted )
            // This check isn't relevant for deleted files, which don't have valid attributes
            // If it's hidden, ignore it
            if( file.IsHidden() )
                return;

        // Ignore non images, and hidden files/folders.
        if( file.IsDirectory() || _imageProcessService.IsImageFileType(file) || file.IsSidecarFileType() )
        {
            // We prioritise deletes, because they're quicker to process, and it's
            // more important to remove them from folders as soon as possible.
            var priority = changeType switch
            {
                WatcherChangeTypes.Deleted => 0,
                WatcherChangeTypes.Changed => 1,
                _ => 2
            };

            if( folderQueue.TryAdd(folder, priority) )
                Logging.Log( $"FileWatcher: adding to queue: {folder} {changeType}" );
            else
                Logging.Log( $"FileWatcher: folder was not added to the queue: {folder} {changeType}" );
        }
    }

    private static void WatcherError(object sender, ErrorEventArgs e)
    {
        // TODO - need to catch many of these and abort - if the inotify count is too large
        Logging.LogError($"Flagging Error for folder: {e.GetException().Message}");
    }

    private void OnChanged(object source, FileSystemEventArgs e)
    {
        Logging.LogVerbose($"FileWatcher: {e.FullPath} {e.ChangeType}");

        var file = new FileInfo(e.FullPath);

        EnqueueFolderChangeForRescan(file, e.ChangeType);
    }

    private void OnRenamed(object source, RenamedEventArgs e)
    {
        Logging.LogVerbose($"FileWatcher: {e.OldFullPath} => {e.FullPath} {e.ChangeType}");

        var oldfile = new FileInfo(e.OldFullPath);
        var newfile = new FileInfo(e.FullPath);

        EnqueueFolderChangeForRescan(oldfile, e.ChangeType);
        EnqueueFolderChangeForRescan(newfile, e.ChangeType);
    }

    public void RemoveFileWatcher(string path)
    {
        if ( _watchers.TryGetValue(path, out var fsw) )
        {
            Logging.Log($"Removing FileWatcher for {path}");

            _watchers.Remove(path);

            fsw.EnableRaisingEvents = false;
            fsw.Changed -= OnChanged;
            fsw.Created -= OnChanged;
            fsw.Deleted -= OnChanged;
            fsw.Renamed -= OnRenamed;
            fsw.Error -= WatcherError;
            fsw = null;
        }
    }
}