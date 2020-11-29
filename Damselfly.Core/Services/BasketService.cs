using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Damselfly.Core.Models;
using Damselfly.Core.Utils;
using Microsoft.EntityFrameworkCore;
using EFCore.BulkExtensions;

namespace Damselfly.Core.Services
{
    /// <summary>
    /// Singleton service which is responsible for maintaining the selection
    /// of images saved in the 'basket' for export, sharing, upload and other p
    /// rocessing.
    /// </summary>
    public class BasketService
    {
        public static BasketService Instance { get; private set; }
        private const string s_DefaultBasket = "default";

        public event Action OnChange;
        public Basket CurrentBasket { get; set; }

        public BasketService()
        {
            Instance = this;
        }

        private void NotifyStateChanged()
        {
            OnChange?.Invoke();
        }

        /// <summary>
        /// Sets up the default basket and loads
        /// </summary>
        public void Initialise()
        {
            using var db = new ImageContext();

            CurrentBasket = db.Baskets.Where(x => x.Name.Equals(s_DefaultBasket)).FirstOrDefault();

            if( CurrentBasket == null )
            {
                var defaultBasket = new Basket { Name = s_DefaultBasket };
                db.Baskets.Add(defaultBasket);
                db.SaveChanges("SaveBasket");

                if (CurrentBasket == null)
                    CurrentBasket = defaultBasket;
            }

            LoadSelectedImages();
        }

        /// <summary>
        /// Load a current basket selection 
        /// </summary>
        /// <param name="name"></param>
        public void LoadBasket( string name )
        {
            using var db = new ImageContext();
            var watch = new Stopwatch("LoadBasket");

            var basket = db.Baskets
                                .Where(x => x.Name == name)
                                .Include( x => x.BasketEntries )
                                .ThenInclude(x => x.Image)
                                .ThenInclude(x => x.Folder)
                                .FirstOrDefault();

            // TODO If basket not found, load default?

            // We can't used ThenInclude to pull in the image tags due to this
            // but in the EF framework: https://github.com/dotnet/efcore/issues/19418
            // It's just too slow. So until they fix it (probably EF 5) we need
            // to manually explicitly load the tags for each image, which is
            // very quick.
            foreach (var img in basket.BasketEntries.Select( x => x.Image ) )
                db.LoadTags(img);

            watch.Stop();

            SelectedImages.Clear();
            SelectedImages.AddRange( basket.BasketEntries.Select(x => x.Image) );

            NotifyStateChanged();
        }

        /// <summary>
        /// The list of selected images in the basket
        /// </summary>
        public List<Image> SelectedImages { get; private set; } = new List<Image>();

        /// <summary>
        /// Loads the selected images in the basket, and adds them to the in-memory
        /// SelectedImages collection. 
        /// </summary>
        public void LoadSelectedImages()
        {
            using var db = new ImageContext();
            var watch = new Stopwatch("GetSelectedImages");

            // TODO Assign current basket?
            var images = db.Baskets.Where( x => x.BasketId == CurrentBasket.BasketId )
                            .Include(x => x.BasketEntries)
                            .ThenInclude(x => x.Image)
                            .ThenInclude(x => x.Folder)
                            .SelectMany( x => x.BasketEntries )
                            .Select( x => x.Image )
                            .ToList();

            // We can't used ThenInclude to pull in the image tags due to this
            // but in the EF framework: https://github.com/dotnet/efcore/issues/19418
            // It's just too slow. So until they fix it (probably EF 5) we need
            // to manually explicitly load the tags for each image, which is
            // very quick.
            foreach (var img in images)
                db.LoadTags(img);

            watch.Stop();

            SelectedImages.Clear();
            SelectedImages.AddRange(images);

            NotifyStateChanged();
        }

        public async Task Clear()
        {
            using var db = new ImageContext();

            try
            {
                SelectedImages.Clear();
                await db.BasketEntries.Where( x => x.BasketId.Equals( CurrentBasket.BasketId ) )
                                      .BatchDeleteAsync();
                Logging.Log("Basket cleared.");

                NotifyStateChanged();

                // TODO: This is a hack - think of a better way to propagate
                // the cleared selection to the search results.
                SearchService.Instance.NotifyStateChanged();
                StatusService.Instance.StatusText = "Basket selection cleared.";
            }
            catch (Exception ex)
            {
                Logging.LogError($"Error clearing basket: {ex.Message}");
            }
        }

        /// <summary>
        /// Async. Uses the download service to initiate a download of selected
        /// basket images, given a particular config - e.g., whether the images
        /// should be resized, watermarked, etc.
        /// </summary>
        /// <param name="config">Download configuration with size and watermark settings</param>
        /// <param name="keepPaths">True to keep folder structure, false for a flat zip of images.</param>
        /// <param name="OnProgress">Callback to give progress information to the UI</param>
        /// <returns>String path to the generated file, which is passed back to the doanload request</returns>
        public async Task<string> DownloadSelection(ExportConfig config, bool keepPaths )
        {
            var images = SelectedImages.Select(x => new FileInfo(x.FullPath)).ToArray();

            var virtualZipPath = await DownloadService.Instance.CreateDownloadZipAsync(images, config, keepPaths );

            if (!string.IsNullOrEmpty(virtualZipPath))
            {
                StatusService.Instance.StatusText = $"Basket selection downloaded to {virtualZipPath}.";

                return virtualZipPath;
            }

            return string.Empty;
        }

        /// <summary>
        /// Select or deselect an image - adding or removing it from the basket.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="newState"></param>
        public void SetBasketState(Image image, bool newState)
        {
            using var db = new ImageContext();
            bool changed = false;
            var watch = new Stopwatch("SetSelection");
            var entry = db.BasketEntries.FirstOrDefault(x => x.ImageId.Equals( image.ImageId ));

            if (newState && entry == null)
            {
                entry = new BasketEntry { ImageId = image.ImageId, BasketId = CurrentBasket.BasketId, DateAdded = DateTime.UtcNow };
                db.Add(entry);
                db.SaveChanges("AddToBasket");

                image.BasketEntry = entry;
                SelectedImages.Add(image);
                changed = true;

                // TODO: Lazy load tags here?! Or in SelectionService?
                db.LoadTags(image);

                StatusService.Instance.StatusText = $"{image.FileName} added to the basket.";
            }
            else if (!newState && entry != null)
            {
                db.Remove(entry);
                db.SaveChanges("RemoveFromBasket");

                image.BasketEntry = null;
                SelectedImages.Remove(image);
                changed = true;

                StatusService.Instance.StatusText = $"{image.FileName} removed from the basket.";
            }

            watch.Stop();

            if (changed)
                NotifyStateChanged();
        }

        public bool IsSelected(Image image)
        {
            return SelectedImages.Any(x => x.ImageId == image.ImageId);
        }

        public void CreateAndSelectNewBasket( string name )
        {
            using var db = new ImageContext();
            var newBasket = new Basket { Name = name };
            db.Baskets.Add(newBasket);
            db.SaveChanges("SaveBasket");

            CurrentBasket = newBasket;

            LoadSelectedImages();
        }

        public void SwitchBasket( string name )
        {
            using var db = new ImageContext();

            CurrentBasket = db.Baskets.FirstOrDefault(x => x.Name.Equals(name));

            if( CurrentBasket == null )
                CurrentBasket = db.Baskets.FirstOrDefault(x => x.Name.Equals(s_DefaultBasket));

            LoadSelectedImages();
        }
    }
}
