using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Damselfly.Core.Models;
using Damselfly.Core.Utils;
using Microsoft.EntityFrameworkCore;
using Damselfly.Core.DbModels;
using Damselfly.Core.Services;

namespace Damselfly.Core.ScopedServices;

/// <summary>
/// Singleton service which is responsible for maintaining the selection
/// of images saved in the 'basket' for export, sharing, upload and other p
/// rocessing.
/// </summary>
public class BasketService
{
    private readonly DownloadService _downloadService;
    private readonly UserStatusService _statusService;
    private readonly ImageCache _imageCache;

    private const string s_MyBasket = "My Basket";
    private const string s_DefaultBasket = "default";

    public event Action OnBasketChanged;
    public Basket CurrentBasket { get; set; }

    /// <summary>
    /// The list of selected images in the basket
    /// </summary>
    public List<Image> BasketImages { get; private set; } = new List<Image>();


    public BasketService( UserStatusService statusService,
                            DownloadService downloadService,
                            ImageCache imageCache)
    {
        _statusService = statusService;
        _imageCache = imageCache;
        _downloadService = downloadService;
    }

    private void NotifyStateChanged()
    {
        OnBasketChanged?.Invoke();
    }

    /// <summary>
    /// Loads the selected images in the basket, and adds them to the in-memory
    /// SelectedImages collection. 
    /// </summary>
    public async Task LoadSelectedImages()
    {
        using var db = new ImageContext();
        var watch = new Stopwatch("GetSelectedImages");

        // TODO Assign current basket?
        var images = await db.Baskets.Where(x => x.BasketId == CurrentBasket.BasketId)
                        .Include(x => x.BasketEntries)
                        .ThenInclude(b => b.Image)
                        .SelectMany(x => x.BasketEntries)
                        .Select(x => x.ImageId)
                        .ToListAsync();

        // Cache and enrich the entries
        var enrichedImages = await _imageCache.GetCachedImages(images); 

        watch.Stop();

        BasketImages.Clear();
        BasketImages.AddRange(enrichedImages);

        NotifyStateChanged();
    }

    /// <summary>
    /// Clears the selection from the basket
    /// </summary>
    /// <returns></returns>
    public async Task Clear()
    {
        using var db = new ImageContext();

        try
        {
            BasketImages.Clear();
            await db.BatchDelete( db.BasketEntries.Where( x => x.BasketId.Equals( CurrentBasket.BasketId ) ) );
            Logging.Log("Basket cleared.");

            NotifyStateChanged();

            _statusService.StatusText = "Basket selection cleared.";
        }
        catch (Exception ex)
        {
            Logging.LogError($"Error clearing basket: {ex.Message}");
        }
    }

    /// <summary>
    /// Return the baskets for a user
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public async Task<List<Basket>> GetUserBaskets( AppIdentityUser user )
    {
        int userId = 0;

        if( user != null )
            userId = user.Id;

        using var db = new ImageContext();

        var myBaskets = await db.Baskets.Where(x => x.UserId == null || x.UserId == userId)
                                        .OrderBy( x => x.UserId == null ? 1 : 0 )
                                        .ThenBy( x => x.Name.ToLower() )
                                        .ToListAsync();

        if( ! myBaskets.Any( x => x.UserId == userId ))
        {

            var newBasketName = (user != null) ? s_MyBasket : s_DefaultBasket;

            if( user == null && myBaskets.Any( x => x.Name.Equals(s_DefaultBasket)))
            {
                // Don't create another default basket if one already exists.
                return myBaskets;
            }

            // Create a default (user) basket if none exists.
            var userBasket = new Basket { Name = newBasketName, UserId = user?.Id };
            db.Baskets.Add(userBasket);
            await db.SaveChangesAsync("SaveBasket");

            myBaskets.Insert(0, userBasket);
        }

        return myBaskets;
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
    /// TODO: Maybe move this elsewhere. 
    public async Task<string> DownloadSelection( ExportConfig config )
    {
        var virtualZipPath = await _downloadService.CreateDownloadZipAsync(BasketImages, config );

        if (!string.IsNullOrEmpty(virtualZipPath))
        {
            _statusService.StatusText = $"Basket selection downloaded to {virtualZipPath}.";
            Logging.Log($"Basket selection downloaded to {virtualZipPath}.");

            return virtualZipPath;
        }

        return string.Empty;
    }

    /// <summary>
    /// Select or deselect an image - adding or removing it from the current basket.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="newState"></param>
    /// <param name="basket"></param>
    public async Task SetBasketState(ICollection<Image> images, bool newState )
    {
        await SetBasketState(images, newState, CurrentBasket);
    }

    /// <summary>
    /// Select or deselect an image - adding or removing it from the basket.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="newState"></param>
    /// <param name="basket"></param>
    public async Task SetBasketState( ICollection<Image> images, bool newState, Basket basket )
    {
        try
        {
            using var db = new ImageContext();
            bool changed = false;
            var watch = new Stopwatch("SetSelection");

            var existingEntries = db.BasketEntries.Where(x => x.BasketId == basket.BasketId &&
                        images.Select(img => img.ImageId).Contains(x.ImageId)).ToList();

            if (newState)
            {
                // TODO: skip existing. Do we need this?!
                var imagesToAdd = images.Where(img => !existingEntries.Select(x => x.ImageId).Contains( img.ImageId ) ).ToList();

                var basketEntries = imagesToAdd.Select(img => new BasketEntry
                            {
                                ImageId = img.ImageId,
                                BasketId = basket.BasketId,
                                DateAdded = DateTime.UtcNow
                            }).ToList();

                if (basketEntries.Any())
                {
                    await db.BulkInsert(db.BasketEntries, basketEntries);

                    if (CurrentBasket.BasketId == basket.BasketId)
                    {
                        imagesToAdd.ForEach(img =>
                            {
                                if (img.BasketEntries.Any(x => x.BasketId == basket.BasketId))
                                {
                                    // Associate the basket entry with the image
                                    img.BasketEntries.Add( basketEntries.First( x => x.ImageId == img.ImageId ) );
                                }

                                BasketImages.Add(img);
                            });

                        changed = true;
                        _statusService.StatusText = $"Added {imagesToAdd.Count} image to the basket.";
                    }
                }
            }
            else if (!newState)
            {
                if( await db.BulkDelete( db.BasketEntries, existingEntries ) )
                {
                    foreach( var image in images )
                    {
                        image.BasketEntries.RemoveAll(x => x.BasketId == basket.BasketId);
                    }

                    if (CurrentBasket.BasketId == basket.BasketId)
                    {
                        BasketImages.RemoveAll(x => images.Select(x => x.ImageId).Contains(x.ImageId));
                        changed = true;

                        _statusService.StatusText = $"Removed {existingEntries.Count} images from the basket.";
                    }
                }
            }

            watch.Stop();

            if (changed)
                NotifyStateChanged();
        }
        catch( Exception ex )
        {
            Logging.LogError($"Unable to update the basket: {ex.Message}");
        }
    }

    public bool IsSelected(Image image)
    {
        return BasketImages.Any(x => x.ImageId == image.ImageId);
    }

    // TODO: Async
    public async Task<Basket> CreateNewBasket( string name, int? userId )
    {
        using var db = new ImageContext();

        // TODO: check there isn't an existing basket with the same name and user?
        var newBasket = new Basket { Name = name, UserId = userId };
        db.Baskets.Add(newBasket);
        await db.SaveChangesAsync("SaveBasket");

        return newBasket;
    }

    public async Task ModifyBasket(Basket basket, string newName, int? newUserId )
    {
        using var db = new ImageContext();

        basket.Name = newName;
        basket.UserId = newUserId;
        db.Baskets.Update(basket);
        await db.SaveChangesAsync("EditBasket");

        // Tell listeners so they can reload
        NotifyStateChanged();
    }

    public async Task<Basket> SwitchBasketById(int basketId)
    {
        using var db = new ImageContext();

        var newBasket = await db.Baskets.Where(x => x.BasketId.Equals(basketId)).FirstOrDefaultAsync();

        if (newBasket != null)
        {
            // Only do something if anything's changed
            if (CurrentBasket == null || newBasket.BasketId != CurrentBasket.BasketId)
            {
                // Switch the actual basket
                CurrentBasket = newBasket;

                await LoadSelectedImages();
            }
        }

        return newBasket;
    }

    /// <summary>
    /// Select a default basket - used to ensure we always
    /// have a valid basket selected.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public async Task<Basket> SwitchToDefaultBasket( AppIdentityUser user )
    {
        // Get the list of user baskets. This will always return at
        // least one (because if there are none, one will be created).
        var userBaskets = await GetUserBaskets( user );

        var defaultBasket = userBaskets.FirstOrDefault(x => x.Name == s_MyBasket && x.UserId == user?.Id );

        if (defaultBasket == null)
        {
            // For some reason it's not there, perhaps we're not
            // logged in or something. So just pick the first in
            // the list
            defaultBasket = userBaskets.First();
        }
        await SwitchBasketById(defaultBasket.BasketId);

        return defaultBasket;
    }
}
