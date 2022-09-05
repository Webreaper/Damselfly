using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Damselfly.Core.DbModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Serilog.Core;

namespace Damselfly.Core.ScopedServices;

/// <summary>
/// Cached static data that the server knows, but the client needs to know
/// </summary>
public class ClientImageCacheService : IImageCacheService
{
    private readonly ILogger<ClientImageCacheService> _logger;
    private readonly RestClient httpClient;
    private readonly IMemoryCache _memoryCache;
    private readonly MemoryCacheEntryOptions _cacheOptions;
    private readonly NotificationsService _notifications;
    private static JsonSerializerOptions jsonOptions = new JsonSerializerOptions { ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve };

    public ClientImageCacheService(RestClient client, IMemoryCache cache, NotificationsService notifications, ILogger<ClientImageCacheService> logger)
    {
        _logger = logger;
        _notifications = notifications;
        _memoryCache = cache;
        httpClient = client;
        _cacheOptions = new MemoryCacheEntryOptions()
                        .SetSize(1)
                        .SetSlidingExpiration(TimeSpan.FromHours(4));

        _notifications.SubscribeToNotification<string>(Constants.NotificationType.CacheEvict, Evict);
    }

    public async Task<Image> GetCachedImage(int imgId)
    {
        var list = new int[] { imgId };
        var result = await GetCachedImages(list);
        return result.FirstOrDefault();
    }

    /// <summary>
    /// When we received a notification from the server,
    /// evict an image from the cache. 
    /// </summary>
    /// <param name="imageId"></param>
    private void Evict(string imageId)
    {
        _logger.LogTrace($"Evicting image {imageId} from client-side cache");

        if (int.TryParse(imageId, out var id))
        {
            _memoryCache.Remove(id);
        }
    }

    /// <summary>
    /// For a given list of IDs, load them into the cache, and then return.
    /// Note, it's critical that the results are returned in the same order
    /// as the IDs passed in, so we iterate once to find the ones not in
    /// the cache, then cache them, then iterate again to pull them all out
    /// of the cache, in order.
    /// </summary>
    /// <param name="imgIds"></param>
    /// <returns></returns>
    public async Task<List<Image>> GetCachedImages(ICollection<int> imgIds)
    {
        var result = new List<Image>();

        try
        {
            // Now, re-enumerate the list, but in-order. Note that everything
            // should be in the cache this time
            foreach (var imgId in imgIds)
            {
                Image image = await LoadAndCacheImage(imgId);

                if( image != null )
                    result.Add(image);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception during caching enrichment: {ex.Message}");
        }

        return result;
    }

    private async Task<Image> GetImage(int imgId)
    {
        return await httpClient.CustomGetFromJsonAsync<Image>($"/api/image/{imgId}");
    }

    private async Task<List<Image>> GetImages(ICollection<int> imgIds)
    {
        var req = new ImageRequest { ImageIds = imgIds.ToList() };
        ImageResponse response = await httpClient.CustomPostAsJsonAsync<ImageRequest, ImageResponse>("/api/images", req);
        return response.Images;
    }

    private async Task<Image> LoadAndCacheImage( int imageId )
    {
        if (_memoryCache.TryGetValue<Image>(imageId, out var image))
            return image;

        try
        {
            image = await GetImage(imageId);
        }
        catch( Exception ex )
        {
            _logger.LogWarning($"Exception loading image {imageId}: {ex}");
        }

        if (image != null)
            _memoryCache.Set(image.ImageId, image, _cacheOptions);
        else
            _logger.LogWarning($"No image loaded for ID: {imageId}.");

        return image;
    }
}