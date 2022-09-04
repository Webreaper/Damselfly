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
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.Constants;
using EFCore.BulkExtensions;

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

    private void NotifyStateChanged( int basketId, BasketChangeType changeType, ICollection<int> updatedImageIds = null )
    {
        OnBasketChanged?.Invoke();

        List<int> imageIds = null;

        var payload = new BasketChanged
        {
            ChangeType = changeType,
            BasketId = basketId
        };

        _ = _notifier.NotifyClients(Constants.NotificationType.BasketChanged, payload );
    }

    /// <summary>
    /// Loads the selected images in the basket, and adds them to the
    /// cache
    /// </summary>
    private async Task<ICollection<Image>> LoadBasketImages( Basket basket )
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

        return enrichedImages;
    }

    /// <summary>
    /// Deletes a basket
    /// </summary>
    /// <returns></returns>
    public async Task Delete( int basketId )
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        var existingBasket = db.Baskets.Where(x => x.BasketId == basketId).FirstOrDefault();

        if (existingBasket != null)
        {
            db.Baskets.Remove( existingBasket );
            await db.SaveChangesAsync("DeleteBasket");

            NotifyStateChanged(basketId, BasketChangeType.BasketDeleted);
        }
    }

    /// <summary>
    /// Clears the selection from the basket
    /// </summary>
    /// <returns></returns>
    public async Task Clear( int basketId )
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        try
        {
            await db.BatchDelete( db.BasketEntries.Where( x => x.BasketId.Equals( basketId ) ) );

            NotifyStateChanged( basketId, BasketChangeType.ImagesRemoved, new List<int>() );
        }
        catch (Exception ex)
        {
            Logging.LogError($"Error clearing basket: {ex.Message}");
        }
    }

    /// <summary>
    /// Clears the selection from the basket
    /// </summary>
    /// <returns></returns>
    public async Task<int> CopyImages(int sourceBasketId, int destBasketId)
    {
        int result = 0;
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        try
        {
            var existingDestEntries = db.BasketEntries
                                        .Where(x => x.BasketId == destBasketId)
                                        .Select(x => x.ImageId);

            var newBasketEntries = await db.BasketEntries
                                     .Where(x => x.BasketId == sourceBasketId &&
                                                 ! existingDestEntries.Contains( x.BasketId ) )
                                .Select(x => new BasketEntry
                                {
                                    BasketId = destBasketId,
                                    ImageId = x.ImageId
                                }).ToListAsync();

            await db.BulkInsert(db.BasketEntries, newBasketEntries);

            result = newBasketEntries.Count();

            NotifyStateChanged(destBasketId, BasketChangeType.ImagesAdded, newBasketEntries.Select( x => x.ImageId).ToList());
        }
        catch (Exception ex)
        {
            Logging.LogError($"Error clearing basket: {ex.Message}");
        }

        return result;
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
                                        .Include( x => x.BasketEntries )
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
    public async Task<string> DownloadSelection( int basketId, ExportConfig config )
    {
        var basket = await GetBasketById(basketId);

        var images = await LoadBasketImages(basket);

        var virtualZipPath = await _downloadService.CreateDownloadZipAsync(images, config );

        if (!string.IsNullOrEmpty(virtualZipPath))
        {
            _statusService.UpdateStatus( $"Basket selection downloaded to {virtualZipPath}." );
            Logging.Log($"Basket selection downloaded to {virtualZipPath}.");

            return virtualZipPath;
        }

        return string.Empty;
    }

    /// <summary>
    /// Select or deselect an image - adding or removing it from the basket.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="newState"></param>
    /// <param name="basket"></param>
    public async Task SetImageBasketState( int basketId, bool newState, ICollection<int> images )
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            using var db = scope.ServiceProvider.GetService<ImageContext>();
            var change = newState ? BasketChangeType.ImagesAdded : BasketChangeType.ImagesRemoved;

            bool changed = false;
            var watch = new Stopwatch("SetSelection");

            var basket = await db.Baskets.Where(x => x.BasketId == basketId)
                                .Include( x => x.BasketEntries )
                                .FirstOrDefaultAsync();

            var existingEntries = await db.BasketEntries.Where(x => x.BasketId == basketId &&
                                                    images.Contains(x.ImageId))
                                                    .ToListAsync();

            var basketImageIds = basket.BasketEntries.Select(x => x.ImageId).ToList();

            if (change == BasketChangeType.ImagesAdded)
            {
                // TODO: skip existing. Do we need this?!
                var newBasketEntries = images.Except(basket.BasketEntries.Select(x => x.ImageId) )
                                          .Select(img => new BasketEntry
                                            {
                                                ImageId = img,
                                                BasketId = basketId,
                                                DateAdded = DateTime.UtcNow
                                            }).ToList();

                if (newBasketEntries.Any())
                {
                    basketImageIds.AddRange(newBasketEntries.Select(x => x.ImageId));

                    await db.BulkInsert(db.BasketEntries, newBasketEntries);

                    foreach (var img in newBasketEntries)
                    {
                        var cachedImage = await _imageCache.GetCachedImage(img.ImageId);

                        if (cachedImage.BasketEntries.Any(x => x.BasketId == basketId))
                        {
                            // Associate the basket entry with the image in the cache
                            cachedImage.BasketEntries.Add( newBasketEntries.First(x => x.ImageId == img.ImageId));
                        }
                    }

                    changed = true;
                    _statusService.UpdateStatus($"Added {newBasketEntries.Count} image to the basket {basket.Name}.");
                }
            }
            else if (change == BasketChangeType.ImagesRemoved)
            {
                basketImageIds = basketImageIds.Except(existingEntries.Select(x => x.ImageId)).ToList();

                if ( await db.BulkDelete( db.BasketEntries, existingEntries ) )
                {
                    foreach( var imageId in images )
                    {
                        var cachedImage = await _imageCache.GetCachedImage(imageId);
                        cachedImage.BasketEntries.RemoveAll(x => x.BasketId == basketId);
                    }

                    changed = true;
                    _statusService.UpdateStatus($"Removed {existingEntries.Count} images from the basket {basket.Name}.");
                }
            }

            watch.Stop();

            if (changed)
            {
                // Notify all the clients that the basket has changed
                NotifyStateChanged(basket.BasketId, change, basketImageIds);
            }
        }
        catch( Exception ex )
        {
            Logging.LogError($"Unable to update the basket: {ex.Message}");
        }
    }

    public bool IsSelected( int basketId, Image image)
    {
        var basket = GetBasketById(basketId).Result;
        return basket.BasketEntries.Any(x => x.ImageId == image.ImageId);
    }

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

            NotifyStateChanged(newBasket.BasketId, BasketChangeType.BasketCreated);

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
        NotifyStateChanged( basket.BasketId, BasketChangeType.BasketChanged );
    }

    public async Task<Basket> GetBasketById(int basketId)
    {
        using var scope = _scopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetService<ImageContext>();

        var newBasket = await db.Baskets.Where(x => x.BasketId.Equals(basketId))
                                        .Include(x => x.BasketEntries)
                                        .FirstOrDefaultAsync();

        if (newBasket != null)
        {
            // Load and enrich the images
            await LoadBasketImages(newBasket);
        }

        return newBasket;
    }

    /// <summary>
    /// Select a default basket - used to ensure we always
    /// have a valid basket selected.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public async Task<Basket> GetDefaultBasket( int? userId )
    {
        // TODO: WASM: - load basket entries here. Also, maybe make this more efficient, with a single query?
        // Maybe return ID here, and then caller can load?

        Basket defaultBasket = null;

        if (userId.HasValue)
        {
            // Get the list of user baskets. This will always return at
            // least one (because if there are none, one will be created).
            var userBaskets = await GetUserBaskets(userId);

            defaultBasket = userBaskets.FirstOrDefault(x => x.Name == s_MyBasket && x.UserId == userId);

            if (defaultBasket == null)
            {
                // For some reason it's not there, perhaps we're not
                // logged in or something. So just pick the first in
                // the list
                defaultBasket = userBaskets.First();
            }
        }
        else
        {
            using var scope = _scopeFactory.CreateScope();
            using var db = scope.ServiceProvider.GetService<ImageContext>();

            // Get the first default basket
            defaultBasket = db.Baskets
                                .Include( x => x.BasketEntries )
                                .FirstOrDefault(x => x.Name == s_DefaultBasket );

            if (defaultBasket == null)
            {
                // Probably shouldn't ever happen, but just pick the first one
                defaultBasket = db.Baskets.First();

                // TODO: If still null, should we create one?
                throw new ArgumentException("No baskets - this is unexpected!");
            }
        }

        return defaultBasket;
    }
}
