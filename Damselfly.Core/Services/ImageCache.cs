using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Damselfly.Core.Models;
using Damselfly.Core.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Damselfly.Core.Services
{
    public class ImageCache
    {
        private readonly IMemoryCache _memoryCache;
        private readonly MemoryCacheEntryOptions _cacheOptions;

        public ImageCache(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
            _cacheOptions = new MemoryCacheEntryOptions()
                            .SetSize( 1 )
                            .SetSlidingExpiration(TimeSpan.FromDays( 2 ));
        }

        /// <summary>
        /// Get an enriched image back from the cache for a particular ID
        /// </summary>
        /// <param name="imgId"></param>
        /// <returns></returns>
        public async Task<Image> GetCachedImage(int imgId)
        {
            Image cachedImage;

            var ids = new List<int>() { imgId };
            var cachedImages = await EnrichAndCache( ids );

            cachedImage = cachedImages.FirstOrDefault();

            return cachedImage;
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
        public async Task<List<Image>> GetCachedImages(List<int> imgIds)
        {
            var result = new List<Image>();

            // First, get the list that aren't in the cache
            var needLoad = imgIds.Where( x => ! _memoryCache
                                    .TryGetValue( x, out var _ ) )
                                    .ToList();

            // Now load and cache them
            if (needLoad.Any())
                await EnrichAndCache(needLoad);

            // Now, re-enumerate the list - everything should be in the cache this time
            foreach ( var imgId in imgIds )
            {
                Image image;
                if (_memoryCache.TryGetValue(imgId, out image))
                    result.Add(image);
                else
                    Logging.LogError("Cached image was not found in cache.");
            }

            return result;
        }

        public async Task<Image> GetCachedImage( Image img )
        {
            Image cachedImage;

            if( ! _memoryCache.TryGetValue(img.ImageId, out cachedImage) )
            {
                Logging.LogVerbose($"Cache miss for image {img.ImageId}");
                cachedImage = await EnrichAndCache(img);
            }

            return cachedImage;
        }

        private async Task<List<Image>> EnrichAndCache( List<int> imageIds )
        {
            using var db = new ImageContext();

            var images = await db.Images
                            .Where(x => imageIds.Contains( x.ImageId) )
                            .Include(x => x.Folder)
                            .Include(x => x.MetaData)
                            .Include(x => x.Hash)
                            .Include(x => x.MetaData.Camera)
                            .Include(x => x.MetaData.Lens)
                            .Include(x => x.BasketEntries)
                            .Include(x => x.ImageTags.Where(y => imageIds.Contains(y.ImageId) ) )
                            .ThenInclude( x => x.Tag )
                            .Include( x => x.ImageObjects.Where( y => imageIds.Contains( y.ImageId ) ) )
                            .ThenInclude(x => x.Tag)
                            .Include( x => x.ImageObjects.Where(y => imageIds.Contains(y.ImageId) ) )
                            .ThenInclude(x => x.Person)
                            .ToListAsync();

            foreach (var enrichedImage in images)
            {
                _memoryCache.Set(enrichedImage.ImageId, enrichedImage, _cacheOptions);
            }

            return images;
        }

        private async Task<Image> EnrichAndCache( Image image )
        {
            var enrichedImage = await GetImage(image);

            if (enrichedImage != null)
            {
                _memoryCache.Set(enrichedImage.ImageId, enrichedImage, _cacheOptions);
            }

            return enrichedImage;
        }

        /// <summary>
        /// Get a single image and its metadata, ready to be cached.
        /// </summary>
        /// <param name="imageId"></param>
        /// <returns></returns>
        private static async Task<Image> GetImage(Image image)
        {
            using var db = new ImageContext();
            var watch = new Stopwatch("EnrichForCache");
            var loadtype = "unknown";

            try
            {
                // TODO: Use AsNoTracking here, for speed?

                // We're either passed an existing image, or an image id.
                if (image != null)
                {
                    loadtype = "object";
                    var entry = db.Attach(image);

                    if (!entry.Reference(x => x.Folder).IsLoaded)
                        await entry.Reference(x => x.Folder)
                                .LoadAsync();

                    if (!entry.Reference(x => x.MetaData).IsLoaded)
                        await entry.Reference(x => x.MetaData)
                                   .Query()
                                   .Include(x => x.Camera)
                                   .Include(x => x.Lens)
                                   .LoadAsync();

                    if (!entry.Reference(x => x.Hash).IsLoaded)
                        await entry.Reference(x => x.Hash)
                                   .LoadAsync();

                    if (!entry.Collection(x => x.BasketEntries).IsLoaded)
                        await entry.Collection(x => x.BasketEntries).LoadAsync();
                }

                if (image != null)
                {
                    /// Because of this issue: https://github.com/dotnet/efcore/issues/19418
                    /// we have to explicitly load the tags, rather than using eager loading.

                    if (!db.Entry(image).Collection(e => e.ImageTags).IsLoaded)
                    {
                        // Now load the tags
                        await db.Entry(image).Collection(e => e.ImageTags)
                                    .Query()
                                    .Include(e => e.Tag)
                                    .LoadAsync();
                    }

                    if (!db.Entry(image).Collection(e => e.ImageObjects).IsLoaded)
                    {
                        await db.Entry(image).Collection(e => e.ImageObjects)
                                     .Query()
                                     .Include(x => x.Tag)
                                     .Include(x => x.Person)
                                     .LoadAsync();
                    }
                }
                else
                    throw new ArgumentException("Logic error.");
            }
            catch (Exception ex)
            {
                Logging.Log($"Exception retrieving image: {ex.Message}");
            }
            finally
            {
                watch.Stop();
                Logging.LogVerbose($"Cache enrich from {loadtype} took {watch.ElapsedTime}ms");
            }


            return image;
        }

        public void Evict(int imageId)
        {
            Logging.LogVerbose($"Evicting from cache: {imageId}");
            _memoryCache.Remove(imageId);
        }
    }
}

