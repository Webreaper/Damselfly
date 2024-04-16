using System.Collections.Specialized;
using Damselfly.Core.Constants;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Shared.Utils;

namespace Damselfly.Core.ScopedServices;

/// <summary>
///     Service to load all of the folders monitored by Damselfly, and present
///     them as a single collection to the UI.
/// </summary>
public class UserFolderService : IDisposable, IUserFolderService
{
    private readonly IConfigService _configService;
    private readonly IFolderService _folderService;
    private readonly NotificationsService _notifications;
    private readonly ISearchService _searchService;
    private readonly IUserService _userService;
    
    private ICollection<Folder>? folderItems; // Ordered as returned from the service
    private Dictionary<int, Folder> folderLookup = new();
    private IDictionary<int, UserFolderState> folderStates;

    public UserFolderService(IFolderService folderService, 
                ISearchService searchService, 
                IConfigService configService,
                IUserService userService,
                NotificationsService notifications)
    {
        _folderService = folderService;
        _searchService = searchService;
        _userService = userService;
        _configService = configService;
        _notifications = notifications;
        
        _folderService.OnChange += OnFolderChanged;

        _notifications.SubscribeToNotification(NotificationType.FoldersChanged, OnFolderChanged);
    }

    public void Dispose()
    {
        _folderService.OnChange -= OnFolderChanged;
    }

    public event Action OnFoldersChanged;

    public bool IsExpanded(Folder folder)
    {
        if ( folderStates.TryGetValue(folder.FolderId, out var folderState) )
            return folderState.Expanded;

        return false;
    }

    /// <summary>
    ///     Process a filter on the service's in-memory list, and present it back
    ///     to be displayed to the UI.
    /// </summary>
    /// <param name="filterTerm"></param>
    /// <returns></returns>
    public async Task<List<Folder>> GetFilteredFolders(string filterTerm)
    {
        if ( folderItems == null )
        {
            // Load the folders if we haven't already.
            folderItems = await _folderService.GetFolders();
            folderLookup = folderItems.ToDictionary(x => x.FolderId, x => x);

            if( _userService.UserId.HasValue )
            {
                var userId = _userService.UserId.Value;
                folderStates = await _folderService.GetUserFolderStates(userId);
            }
        }

        var rootFolderItem = folderItems.FirstOrDefault();

        if ( rootFolderItem == null )
            return new List<Folder>();

        var sortAscending = _configService.GetBool(ConfigSettings.FolderSortAscending, true);
        var sortMode = _configService.Get(ConfigSettings.FolderSortMode, "Date");

        IEnumerable<Folder> items;

        if ( sortMode == "Date" )
            items = SortedChildren(rootFolderItem, x => x.MetaData.MaxImageDate, sortAscending).ToList();
        else
            items = SortedChildren(rootFolderItem, x => x.Name, sortAscending).ToList();


        if ( items != null && items.Any() && !string.IsNullOrEmpty(filterTerm) )
            items = items.Where(x => x.MetaData.DisplayName.ContainsNoCase(filterTerm)
                                     // Always include the currently selected folder so it remains highlighted
                                     || _searchService.Folder?.FolderId == x.FolderId)
                .Where(x => x.Parent is null || IsExpanded(x.Parent));

        var flat = _configService.GetBool(ConfigSettings.FlatView, true);

        if ( flat )
        {
            var foldersWithImages = items.Where(x => x.MetaData.ImageCount > 0);

            // TODO: Refactor to make this more generic
            if ( sortMode == "Date" )
            {
                if ( sortAscending )
                    return foldersWithImages.OrderBy(x => x.MetaData.MaxImageDate).ToList();
                return foldersWithImages.OrderByDescending(x => x.MetaData.MaxImageDate).ToList();
            }

            if ( sortAscending )
                return foldersWithImages.OrderBy(x => x.Name).ToList();
            return foldersWithImages.OrderByDescending(x => x.Name).ToList();
        }

        return items.Where(x => x.ParentFolders.All(x => IsExpanded(x))).ToList();
    }

    public Task<Folder?> GetFolder(int folderId)
    {
        if ( ! folderLookup.TryGetValue(folderId, out var result) )
            result = null;
        
        return Task.FromResult( result );
    }

    /// <summary>
    ///     Toggle the state of a folder.
    /// </summary>
    /// <param name="item"></param>
    public void ToggleExpand(Folder item)
    {
        if ( folderStates.TryGetValue(item.FolderId, out var folderState) )
        {
            folderState.Expanded = !folderState.Expanded;
            _folderService.SaveFolderState(folderState);
        }
    }

    private void OnFolderChanged()
    {
        // Clear the cache so next time we'll reload from the server service
        folderItems = null;

        OnFoldersChanged?.Invoke();
    }

    /// <summary>
    ///     Recursive method to create the sorted list of all hierarchical folders
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="folder"></param>
    /// <param name="sortFunc"></param>
    /// <returns></returns>
    public IEnumerable<Folder> SortedChildren<T>(Folder f, Func<Folder, T> sortFunc, bool descending = true)
    {
        IEnumerable<Folder> sortedChildren =
            descending ? f.Children.OrderBy(sortFunc) : f.Children.OrderByDescending(sortFunc);

        return new[] { f }.Concat(sortedChildren.SelectMany(x => SortedChildren(x, sortFunc, descending)));
    }
}