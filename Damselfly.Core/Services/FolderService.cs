using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Damselfly.Core.Constants;
using Damselfly.Core.Database;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Utils;
using Damselfly.Shared.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;

namespace Damselfly.Core.Services;

/// <summary>
///     Service to load all of the folders monitored by Damselfly, and present
///     them as a single collection to the UI.
/// </summary>
public class FolderService : IFolderService
{
    private readonly ServerNotifierService _notifier;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EventConflator conflator = new( 10 * 1000 );
    private List<Folder> allFolders = new();

    public FolderService(IndexingService _indexingService, 
        IServiceScopeFactory scopeFactory,
        ServerNotifierService notifier)
    {
        _scopeFactory = scopeFactory;
        _notifier = notifier;

        // After we've loaded the data, start listening
        _indexingService.OnFoldersChanged += OnFoldersChanged;

        // Initiate pre-loading the folders.
        _ = LoadFolders();
    }
    
    public event Action OnChange;

    public Task<ICollection<Folder>> GetFolders()
    {
        ICollection<Folder> result = allFolders;
        return Task.FromResult(result);
    }

    public async Task<Dictionary<int, UserFolderState>> GetUserFolderStates(int? userId)
    {
        var result = new Dictionary<int, UserFolderState>();

        if( !userId.HasValue )
            return result;

        using var scope = _scopeFactory.CreateScope();
        await using var db = scope.ServiceProvider.GetService<ImageContext>();

        var watch = new Stopwatch("GetUserFolderStates");

        Logging.Log($"Loading folder states for user {userId}...");

        try
        {
            result = await db.UserFolderStates
                .Where( x => x.UserId == userId)
                .ToDictionaryAsync(x => x.FolderId, x => x);
        }
        catch ( Exception ex )
        {
            Logging.LogError($"Error loading folder states: {ex.Message}");
        }

        List<UserFolderState> toCreate = new();
        
        // Fill in any that don't have a folder state (since we only persist expanded folders)
        // TODO: Only store state for folders with subfolders? But how?
        foreach( var folder in allFolders )
        {
            if( !result.ContainsKey(folder.FolderId) )
            {
                var newState = new UserFolderState
                {
                    // Default expanded state is true if there are subfolders
                    Expanded = folder.HasSubFolders,
                    FolderId = folder.FolderId,
                    UserId = userId.Value
                };
                
                toCreate.Add( newState);
                result.Add( folder.FolderId, newState);
            }
        }

        // Save the new one
        await SaveFolderStates([], toCreate);

        watch.Stop();

        return result;
    }

    private async Task SaveFolderStates(IEnumerable<UserFolderState> updatedStates,
        IEnumerable<UserFolderState> newStates)
    {
        if( updatedStates.Any() || newStates.Any() )
        {
            using var scope = _scopeFactory.CreateScope();
            await using var db = scope.ServiceProvider.GetService<ImageContext>();

            var watch = new Stopwatch("SaveUserFolderState");

            try
            {

                db.UserFolderStates.AddRange( newStates);
                db.UserFolderStates.UpdateRange( updatedStates);
                await db.SaveChangesAsync("SaveUserFolderState");
            }
            catch ( Exception ex )
            {
                Logging.LogError($"Error saving folder states: {ex.Message}");
            }

            watch.Stop();
        }
    }

    public async Task SaveFolderStates(IEnumerable<UserFolderState> newStates)
    {
        await SaveFolderStates( newStates, []);
    }

    private void OnFoldersChanged()
    {
        conflator.HandleEvent(ConflatedCallback);
    }

    private void ConflatedCallback(object state)
    {
        _ = LoadFolders();
    }

    private void NotifyStateChanged()
    {
        Logging.Log($"Folders changed: {allFolders.Count}");

        OnChange?.Invoke();

        _ = _notifier.NotifyClients(NotificationType.FoldersChanged);
    }

    /// <summary>
    ///     Load the folders from the DB, and create a FolderListItem
    ///     which has a summary of the number of images in each folder
    ///     and the most recent modified date of any image in the folder.
    /// </summary>
    public async Task LoadFolders()
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        var watch = new Stopwatch("GetFolders");

        Logging.Log("Loading folder data...");

        try
        {
            allFolders = await db.Folders
                .Include(x => x.Children)
                .Select(x => CreateFolderWrapper(x, x.Images.Count, x.Images.Max(i => i.SortDate)))
                .ToListAsync();
        }
        catch ( Exception ex )
        {
            Logging.LogError($"Error loading folders: {ex.Message}");
        }

        watch.Stop();

        // Update the GUI
        NotifyStateChanged();
    }

    /// <summary>
    ///     Bolt some metadata onto the folder object so it can be used by the UI.
    /// </summary>
    /// <param name="folder"></param>
    /// <param name="imageCount"></param>
    /// <param name="maxDate"></param>
    /// <returns></returns>
    private static Folder CreateFolderWrapper(Folder folder, int imageCount, DateTime? maxDate)
    {
        var item = folder.MetaData;

        if ( item == null )
        {
            item = new FolderMetadata
            {
                ImageCount = imageCount,
                MaxImageDate = maxDate,
                DisplayName = GetFolderDisplayName(folder)
            };

            folder.MetaData = item;
        }

        ;

        var parent = folder.Parent;

        while ( parent != null )
        {
            if ( parent.MetaData == null )
                parent.MetaData = new FolderMetadata { DisplayName = GetFolderDisplayName(parent) };

            if ( parent.MetaData.MaxImageDate == null || parent.MetaData.MaxImageDate < maxDate )
                parent.MetaData.MaxImageDate = maxDate;

            parent.MetaData.ChildImageCount += imageCount;

            item.Depth++;
            parent = parent.Parent;
        }

        return folder;
    }

    /// <summary>
    ///     Clean up the display name
    /// </summary>
    /// <param name="folder"></param>
    /// <returns></returns>
    private static string GetFolderDisplayName(Folder folder)
    {
        var display = folder.Name;

        while ( display.StartsWith('/') || display.StartsWith('\\') )
            display = display.Substring(1);

        return display;
    }
}