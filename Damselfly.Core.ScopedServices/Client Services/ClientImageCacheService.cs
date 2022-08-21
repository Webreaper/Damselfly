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
    private static JsonSerializerOptions jsonOptions = new JsonSerializerOptions { ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve };

    public ClientImageCacheService(RestClient client, IMemoryCache cache, ILogger<ClientImageCacheService> logger)
    {
        _logger = logger;
        _memoryCache = cache;
        httpClient = client;
        _cacheOptions = new MemoryCacheEntryOptions()
                        .SetSize(1)
                        .SetSlidingExpiration(TimeSpan.FromHours(4));
    }

    public async Task<Image> GetCachedImage(int imgId)
    {
        var list = new int[] { imgId };
        var result = await GetCachedImages( list );
        return result.FirstOrDefault();
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
                _logger.LogInformation("Some images were not in the client side cache");
                await LoadAndCacheImages(needLoad);
            }
            else
                _logger.LogInformation("All images were in the client side cache");

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
            Logging.LogError($"Exception during caching enrichment: {ex.Message}");
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

    private async Task LoadAndCacheImages(ICollection<int> imageIds)
    {
        bool useParallel = true;
        int batchSize = 1;

        var tasks = new List<Task>();

        var batches = imageIds.Chunk(batchSize);

        if (useParallel)
        {
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
        else
        {
            if (batchSize == 1)
            {
                foreach (var id in imageIds)
                {
                    _logger.LogInformation($"Adding image {id} to cache...");
                    var i = await GetImage(id);
                    _memoryCache.Set(i.ImageId, i, _cacheOptions);
                }
            }
            else
            {
                foreach (var batch in batches)
                {
                    _logger.LogInformation($"Loading batch of {batch.Count()} images ({string.Join(", ", batch)})...");
                    var imgs = await GetImages(batch);
                    foreach (var i in imgs)
                    {
                        _logger.LogInformation($"Adding images {string.Join(", ", batch)} to cache...");
                        _memoryCache.Set(i.ImageId, i, _cacheOptions);
                    }
                }
            }
        }
    }
}

