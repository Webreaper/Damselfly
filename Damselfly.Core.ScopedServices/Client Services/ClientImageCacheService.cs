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
            // First, get the list that aren't in the cache
            var needLoad = imgIds.Where(x => !_memoryCache.TryGetValue(x, out var _))
                                    .ToList();

            // Now load and cache them
            if (needLoad.Any())
            {
                _logger.LogTrace("Some images were not in the client side cache");
                await LoadAndCacheImages(needLoad);
            }
            else
                _logger.LogTrace("All images were in the client side cache");

            // Now, re-enumerate the list, but in-order. Note that everything
            // should be in the cache this time
            foreach (var imgId in imgIds)
            {
                Image image;
                if (!_memoryCache.TryGetValue(imgId, out image))
                {
                    // Somehow an item which we just supposedly cached, is no
                    // longer in the cache. This is very bad indeed.
                    _logger.LogError($"Cached image {imgId} was not found in cache.");
                    continue;
                }

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

    /// <summary>
    /// Submit the image load requests in parallel so we wait as little a time as possible.
    /// TODO: Still need to understand why GetImages doesn't work (so we could submit them
    /// in batches.
    /// </summary>
    /// <param name="imageIds"></param>
    /// <returns></returns>
    private async Task LoadAndCacheImages(ICollection<int> imageIds)
    {
        try
        {
            int batchSize = 1;

            var tasks = new List<Task>();

            var batches = imageIds.Chunk(batchSize);

            foreach (var batch in batches)
            {
                async Task func()
                {
                    _logger.LogInformation($"Loading images {string.Join(", ", batch)}...");
                    if (batch.Count() == 1)
                    {
                        var i = await GetImage(batch.First());
                        _memoryCache.Set(i.ImageId, i, _cacheOptions);
                    }
                    else
                    {
                        var imgs = await GetImages(batch);
                        foreach (var i in imgs)
                            _memoryCache.Set(i.ImageId, i, _cacheOptions);
                    }
                }

                tasks.Add(func());
            }

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception during LoadCacheImages: {ex}");
        }
    }
}