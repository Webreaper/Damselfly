using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Damselfly.Core.Models;
using Damselfly.Core.Utils;
using Damselfly.Core.Services;

namespace Damselfly.Core.ScopedServices;

/// <summary>
/// Service to load all of the folders monitored by Damselfly, and present
/// them as a single collection to the UI.
/// </summary>
public class UserFolderService
{
    private readonly FolderService _folderService;
    private readonly SearchService _searchService;

    public UserFolderService( FolderService folderService, SearchService searchService)
    {
        _folderService = folderService;
        _searchService = searchService;
    }

    /// <summary>
    /// Process a filter on the service's in-memory list, and present it back
    /// to be displayed to the UI.
    /// </summary>
    /// <param name="filterTerm"></param>
    /// <returns></returns>
    public async Task<List<FolderListItem>> GetFilteredFolders( string filterTerm )
    {
        List<FolderListItem> items = null;

        var allFolderItems = _folderService.FolderItems;

        if (allFolderItems != null && allFolderItems.Any() && !string.IsNullOrEmpty(filterTerm))
        {
            items = await Task.FromResult(allFolderItems
                            .Where(x => x.DisplayName.ContainsNoCase(filterTerm)
                                        // Always include the currently selected folder so it remains highlighted
                                        || _searchService.Folder?.FolderId == x.Folder.FolderId)
                            .ToList());
        }
        else
            items = allFolderItems;

        return items;
    }
}
