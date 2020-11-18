using System;
using System.Collections.Generic;
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
        private List<FolderListItem> allFolderItems = new List<FolderListItem>();
        public static FolderService Instance { get; private set; }
        public event Action OnChange;

        public FolderService()
        {
            Instance = this;

            IndexingService.Instance.OnFoldersChanged += OnFoldersChanged;
        }

        private void OnFoldersChanged()
        {
            Logging.Log("Folders added or removed - re-loading cached folder list...");

            // Do this async?
            LoadFolders();

            // Update the GUI
            NotifyStateChanged();
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
        /// TODO: Make this async
        public void LoadFolders()
        {
            using (var db = new ImageContext())
            {
                var watch = new Stopwatch("GetFolders");

                Folder[] folders = new Folder[0];

                // TODO - groupby query here

                // Only pull folders with images
                var folderQuery = db.Folders.Where(x => x.Images.Any());

                var results = folderQuery.Select(x =>
                            new FolderListItem {
                                    Folder = x,
                                    ImageCount = x.Images.Count,
                                    MaxImageDate = x.Images.Max(i => i.MetaData != null ? i.MetaData.DateTaken : DateTime.MinValue)
                                })
                            .OrderByDescending(x => x.MaxImageDate)
                            .ToArray();

                watch.Stop();

                // Save the updated list
                allFolderItems = results.ToList();

                NotifyStateChanged();
            }
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

            if (allFolderItems != null && allFolderItems.Any() && !string.IsNullOrEmpty(filterTerm))
            {
                items = await Task.FromResult(allFolderItems
                                .Where(x => x.Folder.Name.ContainsNoCase(filterTerm)
                                            // Always include the currently selected folder so it remains highlighted
                                            || SearchService.Instance.Folder?.FolderId == x.Folder.FolderId)
                                .ToList());
            }
            else
                items = allFolderItems;

            return items;
        }
    }
}
