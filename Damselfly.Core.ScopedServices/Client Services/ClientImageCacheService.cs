using Damselfly.Core.Constants;
using Damselfly.Core.DbModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Shared.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.ScopedServices;

/// <summary>
///     Cached static data that the server knows, but the client needs to know
/// </summary>
public class ClientImageCacheService : IImageCacheService
{
    private readonly MemoryCacheEntryOptions _cacheOptions;
    private readonly ILogger<ClientImageCacheService> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly NotificationsService _notifications;
    private readonly RestClient httpClient;

    public ClientImageCacheService(RestClient client, IMemoryCache cache, NotificationsService notifications,
        ILogger<ClientImageCacheService> logger)
    {
        _logger = logger;
        _notifications = notifications;
        _memoryCache = cache;
        httpClient = client;
        _cacheOptions = new MemoryCacheEntryOptions()
            .SetSize(1)
            .SetAbsoluteExpiration(TimeSpan.FromHours(4));

        _notifications.SubscribeToNotification<string>(NotificationType.CacheEvict, Evict);
    }

    public async Task<Image> GetCachedImage(int imgId)
    {
        var list = new[] { imgId };
        var result = await GetCachedImages(list, CancellationToken.None);
        return result.FirstOrDefault();
    }

    private void DumpCacheStats(string context)
    {
        var cacheStats = _memoryCache.GetCurrentStatistics();
        if( cacheStats is not null )
            _logger.LogInformation(
                $"CacheStats {context}: Entries: {cacheStats.CurrentEntryCount}, Hits: {cacheStats.TotalHits}, Misses: {cacheStats.TotalMisses})");
        else
            _logger.LogInformation($"CacheStats {context}: Not found");
    }

    /// <summary>
    ///     For a given list of IDs, load them into the cache, and then return.
    ///     Note, it's critical that the results are returned in the same order
    ///     as the IDs passed in, so we iterate once to find the ones not in
    ///     the cache, then cache them, then iterate again to pull them all out
    ///     of the cache, in order.
    /// </summary>
    /// <param name="imgIds"></param>
    /// <returns></returns>
    public async Task<List<Image>> GetCachedImages(ICollection<int> imgIds, CancellationToken token)
    {
        var result = new List<Image>();

        try
        {
            // First pre-cache them in batch
            var cachedImages = await PreCacheImageList(imgIds, token);

            var lookup = cachedImages.ToDictionary(x => x.ImageId);

            // Keep the order
            foreach( var id in imgIds )
            {
                if( token.IsCancellationRequested )
                    break;
                
                if( !lookup.TryGetValue(id, out var cachedImage) )
                {
                    _logger.LogWarning("Pre-caching failed for Image ID {ID}", id);
                    cachedImage = await GetImage(id);
                }

                result.Add(cachedImage);
            }
        }
        catch ( Exception ex )
        {
            _logger.LogError($"Exception during caching enrichment: {ex}");
        }

        return result;
    }
    
    /// <summary>
    ///     When we received a notification from the server,
    ///     evict an image from the cache.
    /// </summary>
    /// <param name="imageId"></param>
    private void Evict(string imageId)
    {
        _logger.LogTrace($"Evicting image {imageId} from client-side cache");

        if ( int.TryParse(imageId, out var id) ) _memoryCache.Remove(id);
    }

    /// <summary>
    /// Given a list of IDs, if that list is smaller than the chunkSize (i.e.,
    /// max request size) we load them from the API/DB, and then cache. If 
    /// there are more than the chunk size, we split them up into multiple 
    /// requests and call async, and then recurse.
    /// </summary>
    /// <param name="imgIds"></param>
    /// <returns></returns>
    private async Task<IEnumerable<Image>> PreCacheImageList(ICollection<int> imgIds, CancellationToken token)
    {
        const int chunkSize = DamselflyContants.PageSize;
        List<Image>? images = new();
        List<int> uncachedIds = new();

        foreach( var id in imgIds )
        {
            if( token.IsCancellationRequested )
                return [];
            
            if( _memoryCache.TryGetValue<Image>(id, out var cachedImage) )
                images.Add(cachedImage);
            else
                uncachedIds.Add(id);
        }

        if ( uncachedIds.Count <= chunkSize )
        {
            var watch = new Stopwatch("ClientGetImages");
            try
            {
                var newImages = await GetImages(uncachedIds, token);
                
                images.AddRange(newImages);
            }
            catch ( Exception ex )
            {
                _logger.LogError($"PreCacheImageList failed: {ex.Message}");
            }
            finally
            {
                watch.Stop();
            }
        }
        else
        {
            var tasks = uncachedIds.Chunk(chunkSize)
                .Select(x => PreCacheImageList(x, token))
                .ToList();
            var watch = new Stopwatch("ClientCacheImage");
            var lists = await Task.WhenAll(tasks);
            watch.Stop();

            _logger.LogInformation(
                $"Cached {tasks.Count} batches of images (total {imgIds.Count}) in {watch.ElapsedTime}ms.");
            
            images = lists.SelectMany(x => x).ToList();
        }
        
        return images;
    }

    private async Task<Image> GetImage(int imgId)
    {
        var image = await httpClient.CustomGetFromJsonAsync<Image>($"/api/image/{imgId}");

        if( image != null)
            _memoryCache.Set(image.ImageId, image, _cacheOptions);
        
        return image;
    }

    private async Task<List<Image>> GetImages(ICollection<int> imgIds, CancellationToken token)
    {
        if ( imgIds.Any() )
        {
            var req = new ImageRequest { ImageIds = imgIds.ToList() };
            var response =
                await httpClient.CustomPostAsJsonAsync<ImageRequest, ImageResponse>("/api/images/batch", req, token);

            if( response != null )
            {
                foreach(var image in response.Images)
                    _memoryCache.Set(image.ImageId, image, _cacheOptions);

                return response.Images;
            }
        }

        return new List<Image>();
    }

    private async Task<Image> LoadAndCacheImage(int imageId)
    {
        if ( _memoryCache.TryGetValue<Image>(imageId, out var image) )
            return image;

        _logger.LogTrace($"No image found in client-side cache for ID: {imageId}.");

        try
        {
            image = await GetImage(imageId);
        }
        catch ( Exception ex )
        {
            _logger.LogWarning($"Exception loading image {imageId}: {ex}");
        }

        if ( image == null )
            _logger.LogWarning($"No image was pre-loaded for ID: {imageId}.");

        return image;
    }

    public Task ClearCache()
    {
        var memCache = _memoryCache as MemoryCache;
        if ( memCache is not null )
            // Force the cache to compact 100% of the memory
            memCache.Compact(1.0);

        return Task.CompletedTask;
    }
}