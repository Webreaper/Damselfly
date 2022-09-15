using Damselfly.Core.DbModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Shared.Utils;
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
    private readonly NotificationsService _notifications;

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

    private void CacheImage(Image image)
    {
        _memoryCache.Set(image.ImageId, image, _cacheOptions);
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

    private async Task PreCacheImageList(ICollection<int> imgIds)
    {
        const int chunkSize = 10;

        if (imgIds.Count() <= chunkSize)
        {
            var watch = new Stopwatch("ClientGetImages");
            try
            {
                var images = await GetImages( imgIds );
                _logger.LogInformation( $"Retreived {imgIds.Count} images from server in {watch.ElapsedTime}ms." );
                images.ForEach( x => CacheImage( x ) );
            }
            catch ( Exception ex )
            {
                _logger.LogError( $"PreCacheImageList failed: {ex.Message}" );
            }
            finally
            {
                watch.Stop();
            }
        }
        else
        {
            List<Task> tasks = imgIds.Chunk(chunkSize)
                                     .Select(x => PreCacheImageList(x))
                                     .ToList();
            var watch = new Stopwatch("ClientCacheImage");
            await Task.WhenAll(tasks);
            watch.Stop();

            _logger.LogInformation($"Cached {tasks.Count} batches of images (total {imgIds.Count}) in {watch.ElapsedTime}ms.");
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
            // First pre-cache them in batch
            // await PreCacheImageList(imgIds);

            // This must be done in-order, otherwise we'll end up with a mess
            foreach (var imgId in imgIds)
            {
                var image = await LoadAndCacheImage(imgId);

                if (image != null)
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
        ImageResponse response = await httpClient.CustomPostAsJsonAsync<ImageRequest, ImageResponse>("/api/images/batch", req);
        return response.Images;
    }

    private async Task<Image> LoadAndCacheImage(int imageId)
    {
        if (_memoryCache.TryGetValue<Image>(imageId, out var image))
            return image;

        try
        {
            image = await GetImage(imageId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Exception loading image {imageId}: {ex}");
        }

        if (image != null)
            CacheImage(image);
        else
            _logger.LogWarning($"No image loaded for ID: {imageId}.");

        return image;
    }
}