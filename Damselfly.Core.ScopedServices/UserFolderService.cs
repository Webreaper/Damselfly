using Damselfly.Core.Models;
using Damselfly.Shared.Utils;
using Damselfly.Core.Utils;
using Damselfly.Core.Constants;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Interfaces;

namespace Damselfly.Core.ScopedServices;

/// <summary>
/// Service to load all of the folders monitored by Damselfly, and present
/// them as a single collection to the UI.
/// </summary>
public class UserFolderService : IDisposable, IUserFolderService
{
    private readonly IFolderService _folderService;
    private readonly ISearchService _searchService;
    private readonly IConfigService _configService;

    // WASM: TODO:
    public event Action OnFoldersChanged;
    private ICollection<Folder> folderItems;
    private IDictionary<int, bool> expandedSate;

    public UserFolderService( IFolderService folderService, ISearchService searchService, IConfigService configService)
    {
        _folderService = folderService;
        _searchService = searchService;
        _configService = configService;

        _folderService.OnChange += OnFolderChanged;
    }

    public void Dispose()
    {
        _folderService.OnChange -= OnFolderChanged;
    }

    private void OnFolderChanged()
    {
        // Clear the cache so next time we'll reload from the server service
        folderItems = null;

        OnFoldersChanged?.Invoke();
    }

    /// <summary>
    /// Recursive method to create the sorted list of all hierarchical folders
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="folder"></param>
    /// <param name="sortFunc"></param>
    /// <returns></returns>
    public IEnumerable<Folder> SortedChildren<T>(Folder f, Func<Folder, T> sortFunc, bool descending = true)
    {
        IEnumerable<Folder> sortedChildren = descending ? f.Children.OrderBy(sortFunc) : f.Children.OrderByDescending(sortFunc);

        return new[] { f }.Concat(sortedChildren.SelectMany(x => SortedChildren(x, sortFunc, descending)));
    }

    public bool IsExpanded( Folder folder )
    {
        if (expandedSate.TryGetValue(folder.FolderId, out var expanded))
            return expanded;

        return false;
    }

    /// <summary>
    /// Process a filter on the service's in-memory list, and present it back
    /// to be displayed to the UI.
    /// </summary>
    /// <param name="filterTerm"></param>
    /// <returns></returns>
    public async Task<List<Folder>> GetFilteredFolders( string filterTerm )
    {
        if( folderItems == null )
        {
            // Load the folders if we haven't already.
            folderItems = await _folderService.GetFolders();
            // Default expanded state is true if there are subfolders
            expandedSate = folderItems.ToDictionary(x => x.FolderId, y => y.HasSubFolders);
        }

        var rootFolderItem = folderItems.FirstOrDefault();

        if (rootFolderItem == null)
            return new List<Folder>();

        var sortAscending = _configService.GetBool(ConfigSettings.FolderSortAscending, true);
        var sortMode = _configService.Get(ConfigSettings.FolderSortMode, "Date");

        IEnumerable<Folder> items;

        if ( sortMode == "Date" )
            items = SortedChildren(rootFolderItem, x => x.MetaData.MaxImageDate, sortAscending).ToList();
        else
            items = SortedChildren(rootFolderItem, x => x.Name, sortAscending).ToList();


        if (items != null && items.Any() && !string.IsNullOrEmpty(filterTerm))
        {
            items = items.Where(x => x.MetaData.DisplayName.ContainsNoCase(filterTerm)
                                        // Always include the currently selected folder so it remains highlighted
                                        || _searchService.Folder?.FolderId == x.FolderId)
                            .Where(x => x.Parent is null || IsExpanded( x.Parent ));
        }

        bool flat = _configService.GetBool( ConfigSettings.FlatView, true );

        if (flat)
        {
            var foldersWithImages = items.Where(x => x.MetaData.ImageCount > 0);

            // TODO: Refactor to make this more generic
            if (sortMode == "Date")
            {
                if (sortAscending)
                    return foldersWithImages.OrderBy(x => x.MetaData.MaxImageDate).ToList();
                else
                    return foldersWithImages.OrderByDescending(x => x.MetaData.MaxImageDate).ToList();
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
            return items.Where(x => x.ParentFolders.All(x => IsExpanded( x ) )).ToList();
    }

    /// <summary>
    /// Toggle the state of a folder.
    /// </summary>
    /// <param name="item"></param>
    public void ToggleExpand(Folder item)
    {
        expandedSate[item.FolderId] = ! IsExpanded(item);
    }

}
