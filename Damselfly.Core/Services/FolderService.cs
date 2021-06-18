using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Damselfly.Core.Models;
using Damselfly.Core.Utils;

namespace Damselfly.Core.Services
{
    /// <summary>
    /// Service to load all of the folders monitored by Damselfly, and present
    /// them as a single collection to the UI.
    /// </summary>
    public class FolderService
    {
        private readonly SearchService _searchService;
        private List<FolderListItem> allFolderItems = new List<FolderListItem>();
        public event Action OnChange;

        public FolderService( IndexingService _indexingService, SearchService searchService)
        {
            _indexingService.OnFoldersChanged += OnFoldersChanged;
            _searchService = searchService;

            PreLoadFolderData();
        }

        private void OnFoldersChanged()
        {
            // Do this async?
            _ = LoadFolders();
        }

        private void NotifyStateChanged()
        {
            Logging.LogVerbose($"Folders changed: {allFolderItems.Count}");

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

            Folder[] folders = new Folder[0];

            try
            {
                // Only pull folders with images
                allFolderItems = await db.Folders.Where(x => x.Images.Any())
                                .Select(x =>
                                    new FolderListItem
                                    {
                                        Folder = x,
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

        public void PreLoadFolderData()
        {
            _ = LoadFolders();
        }

        /// <summary>
        /// Process a filter on the service's in-memory list, and present it back
        /// to be displayed to the UI.
        /// </summary>
        /// <param name="filterTerm"></param>
        /// <returns></returns>
        public async Task<List<FolderListItem>> GetFilteredFolders( string filterTerm, bool force )
        {
            List<FolderListItem> items = null;

            if (allFolderItems != null && allFolderItems.Any() && !string.IsNullOrEmpty(filterTerm))
            {
                items = await Task.FromResult(allFolderItems
                                .Where(x => x.Folder.Name.ContainsNoCase(filterTerm)
                                            // Always include the currently selected folder so it remains highlighted
                                            || _searchService.Folder?.FolderId == x.Folder.FolderId)
                                .ToList());
            }
            else
                items = allFolderItems;

            return items;
        }
    }
}
