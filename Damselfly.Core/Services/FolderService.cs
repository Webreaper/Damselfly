using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Damselfly.Core.Models;
using Damselfly.Core.Utils;
using System.IO;

namespace Damselfly.Core.Services;

/// <summary>
/// Service to load all of the folders monitored by Damselfly, and present
/// them as a single collection to the UI.
/// </summary>
public class FolderService
{
    private List<FolderListItem> allFolderItems = new List<FolderListItem>();
    public event Action OnChange;
    private EventConflator conflator = new EventConflator(10 * 1000);

    public FolderService( IndexingService _indexingService)
    {
        // After we've loaded the data, start listening
        _indexingService.OnFoldersChanged += OnFoldersChanged;

        // Trigger a change now to initiate pre-loading the folders.
        OnFoldersChanged();
    }

    private void OnFoldersChanged()
    {
        conflator.HandleEvent(ConflatedCallback);
    }

    private void ConflatedCallback(object state)
    {
        _ = LoadFolders();
    }

    public List<FolderListItem> FolderItems { get { return allFolderItems;  } }

    private void NotifyStateChanged()
    {
        Logging.Log($"Folders changed: {allFolderItems.Count}");

        OnChange?.Invoke();
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
        Folder[] folders = new Folder[0];

        try
        {
            // Only pull folders with images
            allFolderItems = await db.Folders.Where(x => x.Images.Any())
                            .Select(x =>
                                new FolderListItem
                                {
                                    Folder = x,
                                    DisplayName = GetFolderDisplayName( x ),
                                    ImageCount = x.Images.Count,
                                    // Not all images may have metadata yet.
                                    MaxImageDate = x.Images.Max(i => i.SortDate)
                                })
                            .OrderByDescending(x => x.MaxImageDate)
                            .ToListAsync();
        }
        catch( Exception ex )
        {
            Logging.LogError($"Error loading folders: {ex.Message}");
        }

        watch.Stop();

        // Update the GUI
        NotifyStateChanged();
    }

    /// <summary>
    /// Build up the folder display name from enough short or
    /// numeric folder names to give us a meaningful display.
    /// </summary>
    /// <param name="folder"></param>
    /// <returns></returns>
    private static string GetFolderDisplayName( Folder folder )
    {
        string display = folder.Name;
        DirectoryInfo dir = new DirectoryInfo(folder.Path).Parent;

        while( dir != null && display.Length <= 10 )
        {
            // If the name is short, or it's an integer, add it
            if (dir.Name.Length < 6 || int.TryParse(dir.Name, out var _))
            {
                if (display.Length != 0)
                    display = Path.DirectorySeparatorChar + display;

                display = dir.Name + display;

                // Now look at the parent
                dir = dir.Parent;
            }
            else
                break;
        }

        return display;
    }
}
