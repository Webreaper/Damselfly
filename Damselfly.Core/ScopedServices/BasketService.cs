using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Damselfly.Shared.Utils;
using Damselfly.Core.Models;
using Damselfly.Core.Utils;
using Microsoft.EntityFrameworkCore;
using Damselfly.Core.DbModels;
using Damselfly.Core.Services;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Damselfly.Core.ScopedServices;

/// <summary>
/// Singleton service which is responsible for maintaining the selection
/// of images saved in the 'basket' for export, sharing, upload and other p
/// rocessing.
/// </summary>
public class BasketService : IBasketService
{
    private readonly DownloadService _downloadService;
    private readonly IStatusService _statusService;
    private readonly ImageCache _imageCache;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ServerNotifierService _notifier;

    private const string s_MyBasket = "My Basket";
    private const string s_DefaultBasket = "default";

    public event Action OnBasketChanged;
    public Basket CurrentBasket { get; set; }

    // WASM: Does this make sense in client-server?!
    /// <summary>
    /// The list of selected images in the basket
    /// </summary>
    public List<Image> BasketImages { get; private set; } = new List<Image>();


    public BasketService(IServiceScopeFactory scopeFactory,
                         IStatusService statusService,
                            DownloadService downloadService,
                            ImageCache imageCache, ServerNotifierService notifier)
    {
        _scopeFactory = scopeFactory;
        _statusService = statusService;
        _imageCache = imageCache;
        _downloadService = downloadService;
        _notifier = notifier;
    }

    private void NotifyStateChanged()
    {
        OnBasketChanged?.Invoke();

        _ = _notifier.NotifyClients(Constants.NotificationType.BasketChanged);
    }

    /// <summary>
    /// Loads the selected images in the basket, and adds them to the in-memory
    /// SelectedImages collection. 
    /// </summary>
    public async Task LoadSelectedImages( Basket basket )
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        var watch = new Stopwatch("GetSelectedImages");

        // Get the list of image IDs
        var imageIds = basket.BasketEntries
                           .Select(x => x.ImageId)
                           .ToList();

        // Cache and enrich the entries
        var enrichedImages = await _imageCache.GetCachedImages(imageIds);

        // Replace the images with the enriched ones
        foreach (var be in basket.BasketEntries)
            be.Image = enrichedImages.First(x => x.ImageId == be.ImageId);

        watch.Stop();

        BasketImages.Clear();
        BasketImages.AddRange(enrichedImages);

        NotifyStateChanged();
    }

    /// <summary>
    /// Clears the selection from the basket
    /// </summary>
    /// <returns></returns>
    public async Task Delete( int basketId )
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        var existingBasket = db.Baskets.Where(x => x.BasketId == basketId);

        await db.BatchDelete(existingBasket);
    }

    /// <summary>
    /// Clears the selection from the basket
    /// </summary>
    /// <returns></returns>
    public async Task Clear( int basketId = -1)
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        try
        {
            if( basketId == -1 )
                basketId = CurrentBasket.BasketId;

            BasketImages.Clear();
            await db.BatchDelete( db.BasketEntries.Where( x => x.BasketId.Equals( basketId ) ) );
            Logging.Log($"Basket id {basketId} cleared.");

            NotifyStateChanged();

            if( basketId == CurrentBasket.BasketId )
                _statusService.UpdateStatus( "Basket selection cleared." );
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
    public async Task<ICollection<Basket>> GetUserBaskets( int? userId )
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        var myBaskets = await db.Baskets.Where(x => x.UserId == null || x.UserId == userId)
                                        .OrderBy( x => x.UserId == null ? 1 : 0 )
                                        .ThenBy( x => x.Name.ToLower() )
                                        .ToListAsync();

        if( ! myBaskets.Any( x => x.UserId == userId ))
        {

            var newBasketName = (userId.HasValue) ? s_MyBasket : s_DefaultBasket;

            if( userId.HasValue && myBaskets.Any( x => x.Name.Equals(s_DefaultBasket)))
            {
                // Don't create another default basket if one already exists.
                return myBaskets;
            }

            // Create a default (user) basket if none exists.
            var userBasket = new Basket { Name = newBasketName, UserId = userId };
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
            _statusService.UpdateStatus( $"Basket selection downloaded to {virtualZipPath}." );
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
    public async Task SetBasketState(ICollection<int> images, bool newState )
    {
        await SetBasketState(images, newState, CurrentBasket.BasketId);
    }

    /// <summary>
    /// Select or deselect an image - adding or removing it from the basket.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="newState"></param>
    /// <param name="basket"></param>
    public async Task SetBasketState( ICollection<int> images, bool newState, int? basketId )
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            using var db = scope.ServiceProvider.GetService<ImageContext>();

            bool changed = false;
            var watch = new Stopwatch("SetSelection");

            if (basketId == null)
                basketId = CurrentBasket.BasketId;

            var basket = await db.Baskets.Where(x => x.BasketId == basketId)
                                .Include( x => x.BasketEntries )
                                .FirstOrDefaultAsync();

            var existingEntries = db.BasketEntries.Where(x => x.BasketId == basketId &&
                        images.Contains(x.ImageId)).ToList();

            if (newState)
            {
                // TODO: skip existing. Do we need this?!
                var newBasketEntries = images.Except(basket.BasketEntries.Select(x => x.ImageId) )
                                          .Select(img => new BasketEntry
                                            {
                                                ImageId = img,
                                                BasketId = basketId.Value,
                                                DateAdded = DateTime.UtcNow
                                            }).ToList();

                if (newBasketEntries.Any())
                {
                    await db.BulkInsert(db.BasketEntries, newBasketEntries);

                    changed = true;
                    _statusService.UpdateStatus($"Added {newBasketEntries.Count} image to the basket {basket.Name}.");

                    foreach (var img in newBasketEntries)
                    {
                        var actualImage = await _imageCache.GetCachedImage(img.ImageId);

                        if (actualImage.BasketEntries.Any(x => x.BasketId == basketId))
                        {
                            // Associate the basket entry with the image in the cache
                            actualImage.BasketEntries.Add( newBasketEntries.First(x => x.ImageId == img.ImageId));
                        }

                        if( CurrentBasket != null && CurrentBasket.BasketId == basket.BasketId )
                            BasketImages.Add( actualImage );
                    }
                }
            }
            else if (!newState)
            {
                if( await db.BulkDelete( db.BasketEntries, existingEntries ) )
                {
                    foreach( var imageId in images )
                    {
                        var actualImage = await _imageCache.GetCachedImage(imageId);
                        actualImage.BasketEntries.RemoveAll(x => x.BasketId == basketId);
                    }

                    changed = true;
                    _statusService.UpdateStatus($"Removed {existingEntries.Count} images from the basket {basket.Name}.");

                    if(CurrentBasket != null && CurrentBasket.BasketId == basketId)
                        BasketImages.RemoveAll(x => images.Contains(x.ImageId));
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
    public async Task<Basket> Create( string name, int? userId )
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        var existing = db.Baskets.FirstOrDefault(x => x.Name == name);

        if (existing == null)
        {
            // TODO: check there isn't an existing basket with the same name and user?
            var newBasket = new Basket { Name = name, UserId = userId };
            db.Baskets.Add(newBasket);
            await db.SaveChangesAsync("SaveBasket");

            return newBasket;
        }
        else
            throw new ArgumentException($"A basket called {name} already exists.");
    }

    public async Task Save(Basket basket)
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        db.Baskets.Update(basket);
        await db.SaveChangesAsync("EditBasket");

        // Tell listeners so they can reload
        NotifyStateChanged();
    }

    public async Task<Basket> SwitchBasketById(int basketId)
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        var newBasket = await db.Baskets.Where(x => x.BasketId.Equals(basketId))
                                        .Include(x => x.BasketEntries)
                                        .FirstOrDefaultAsync();

        if (newBasket != null)
        {
            // Load and enrich the images
            await LoadSelectedImages(newBasket);

            // Switch the actual basket
            CurrentBasket = newBasket;
        }

        return newBasket;
    }

    /// <summary>
    /// Select a default basket - used to ensure we always
    /// have a valid basket selected.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public async Task<Basket> SwitchToDefaultBasket( int? userId )
    {
        // Get the list of user baskets. This will always return at
        // least one (because if there are none, one will be created).
        var userBaskets = await GetUserBaskets( userId );

        var defaultBasket = userBaskets.FirstOrDefault(x => x.Name == s_MyBasket && x.UserId == userId );

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

    public Task DeleteBasket(int basketId)
    {
        throw new NotImplementedException();
    }
}
