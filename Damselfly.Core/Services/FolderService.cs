using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Damselfly.Core.Models;
using Damselfly.Core.Utils;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Shared.Utils;
using Damselfly.Core.ScopedServices;
using Microsoft.AspNetCore.SignalR;
using Damselfly.Core.Constants;

namespace Damselfly.Core.Services;

/// <summary>
/// Service to load all of the folders monitored by Damselfly, and present
/// them as a single collection to the UI.
/// </summary>
public class FolderService : IFolderService
{
    private List<Folder> allFolders = new List<Folder>();
    public event Action OnChange;
    private EventConflator conflator = new EventConflator(10 * 1000);
    private ServerNotifierService _notifier;

    public FolderService( IndexingService _indexingService, ServerNotifierService notifier)
    {
        _notifier = notifier;

        // After we've loaded the data, start listening
        _indexingService.OnFoldersChanged += OnFoldersChanged;

        // Initiate pre-loading the folders.
        _ = LoadFolders();
    }

    private void OnFoldersChanged()
    {
        conflator.HandleEvent(ConflatedCallback);
    }

    private void ConflatedCallback(object state)
    {
        _ = LoadFolders();
    }

    public async Task<ICollection<Folder>> GetFolders()
    {
        return allFolders;
    }

    private void NotifyStateChanged()
    {
        Logging.Log($"Folders changed: {allFolders.Count}");

        OnChange?.Invoke();

        _ = _notifier.NotifyClients(NotificationType.FoldersChanged);
    }

    /// <summary>
    /// Load the folders from the DB, and create a FolderListItem
    /// which has a summary of the number of images in each folder
    /// and the most recent modified date of any image in the folder.
    /// </summary>
    public async Task LoadFolders()
    {
        using var db = new ImageContext();
        var watch = new Stopwatch("GetFolders");

        Logging.Log("Loading folder data...");

        try
        {
            allFolders = await db.Folders
                            .Include(x => x.Children)
                            .Select(x => CreateFolderWrapper(x, x.Images.Count, x.Images.Max(i => i.SortDate)))
                            .ToListAsync();
        }
        catch (Exception ex)
        {
            Logging.LogError($"Error loading folders: {ex.Message}");
        }

        watch.Stop();

        // Update the GUI
        NotifyStateChanged();
    }

    /// <summary>
    /// Bolt some metadata onto the folder object so it can be used by the UI.
    /// </summary>
    /// <param name="folder"></param>
    /// <param name="imageCount"></param>
    /// <param name="maxDate"></param>
    /// <returns></returns>
    private static Folder CreateFolderWrapper( Folder folder, int imageCount, DateTime? maxDate )
    {

        var item = folder.MetaData;

        if( item == null )
        {
            item = new FolderMetadata
            {
                ImageCount = imageCount,
                MaxImageDate = maxDate,
                DisplayName = GetFolderDisplayName(folder)
            };

            folder.MetaData = item;
        };

        var parent = folder.Parent;

        while ( parent != null )
        {
            if (parent.MetaData == null)
                parent.MetaData = new FolderMetadata { DisplayName = GetFolderDisplayName(parent) };

            if (parent.MetaData.MaxImageDate == null || parent.MetaData.MaxImageDate < maxDate)
                parent.MetaData.MaxImageDate = maxDate;

            parent.MetaData.ChildImageCount += imageCount;

            item.Depth++;
            parent = parent.Parent;
        }

        return folder;
    }

    /// <summary>
    /// Clean up the display name
    /// </summary>
    /// <param name="folder"></param>
    /// <returns></returns>
    private static string GetFolderDisplayName( Folder folder )
    {
        var display = folder.Name;

        while (display.StartsWith('/') || display.StartsWith('\\'))
            display = display.Substring(1);

        return display;
    }
}
