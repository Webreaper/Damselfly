using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Damselfly.Core.Models;
using Damselfly.Core.Utils;
using Damselfly.Core.Services;
using Damselfly.Core.Constants;

namespace Damselfly.Core.ScopedServices;

/// <summary>
/// Service to load all of the folders monitored by Damselfly, and present
/// them as a single collection to the UI.
/// </summary>
public class UserFolderService
{
    private readonly FolderService _folderService;
    private readonly SearchService _searchService;
    private readonly UserConfigService _configService;

    public UserFolderService( FolderService folderService, SearchService searchService, UserConfigService configService)
    {
        _folderService = folderService;
        _searchService = searchService;
        _configService = configService;
    }

    /// <summary>
    /// Process a filter on the service's in-memory list, and present it back
    /// to be displayed to the UI.
    /// </summary>
    /// <param name="filterTerm"></param>
    /// <returns></returns>
    public async Task<List<Folder>> GetFilteredFolders( string filterTerm )
    {
        var allFolderItems = _folderService.FolderItems.FirstOrDefault();

        if (allFolderItems == null)
            return new List<Folder>();

        var sortAscending = _configService.GetBool(ConfigSettings.FolderSortAscending, true);
        var sortMode = _configService.Get(ConfigSettings.FolderSortMode, "Date");

        IEnumerable<Folder> items;

        if ( sortMode == "Date" )
            items = allFolderItems.SortedChildren(x => x.FolderItem.MaxImageDate, sortAscending).ToList();
        else
            items = allFolderItems.SortedChildren(x => x.Name, sortAscending).ToList();


        if (items != null && items.Any() && !string.IsNullOrEmpty(filterTerm))
        {
            items = await Task.FromResult(items
                            .Where(x => x.FolderItem.DisplayName.ContainsNoCase(filterTerm)
                                        // Always include the currently selected folder so it remains highlighted
                                        || _searchService.Folder?.FolderId == x.FolderId)
                            .Where(x => x.Parent is null || x.Parent.FolderItem.IsExpanded));
        }

        bool flat = _configService.GetBool( ConfigSettings.FlatView, true );

        if (flat)
        {
            var foldersWithImages = items.Where(x => x.FolderItem.ImageCount > 0);

            // TODO: Refactor to make this more generic
            if (sortMode == "Date")
            {
                if (sortAscending)
                    return foldersWithImages.OrderBy(x => x.FolderItem.MaxImageDate).ToList();
                else
                    return foldersWithImages.OrderByDescending(x => x.FolderItem.MaxImageDate).ToList();
            }
            else
            {
                if (sortAscending)
                    return foldersWithImages.OrderBy(x => x.Name).ToList();
                else
                    return foldersWithImages.OrderByDescending(x => x.Name).ToList();
            }
        }
        else
            return items.Where(x => x.ParentFolders.All(x => x.FolderItem.IsExpanded)).ToList();
    }

    /// <summary>
    /// Toggle the state of a folder.
    /// </summary>
    /// <param name="item"></param>
    public void ToggleExpand(Folder item)
    {
        item.FolderItem.IsExpanded = ! item.FolderItem.IsExpanded;
    }
}
