using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Damselfly.Core.Services;
using Damselfly.Core.Utils;
using Damselfly.Core.Models;
using Damselfly.Core.ImageProcessing;

namespace Damselfly.Web.Data
{
    /// <summary>
    /// Data access methods for the various image controls.
    /// Being steadily replaced by the ImageService etc in
    /// Damselfly.Core.
    /// </summary>
    public class ImageService : Controller
    {
        /// <summary>
        /// TODO: Move to a dedicated TagService
        /// </summary>
        /// <param name="keyword"></param>
        /// <returns></returns>
        public static Task<Image[]> GetTagImagesAsync(string keyword)
        {
            using var db = new ImageContext();
            var watch = new Stopwatch("GetTagImages");

            // TODO - load this in a more efficient way
            Image[] images;
            images = db.Tags.Where(x => x.Keyword.Equals(keyword))
                        .Include(x => x.ImageTags)
                            .ThenInclude(x => x.Image)
                                .ThenInclude(x => x.MetaData)
                        .Include(x => x.ImageTags)
                            .ThenInclude( x => x.Image )
                                .ThenInclude(x => x.Folder)
                        .SelectMany(x => x.ImageTags.Select(t => t.Image))
                        .OrderByDescending(x => x.MetaData.DateTaken)
                        .Take(200)
                        .ToArray();

            watch.Stop();

            return Task.FromResult(images);
        }

        public static Task<Folder> GetFolderAsync( int folderId )
        {
            using var db = new ImageContext();

            var folder = db.Folders.Where(x => x.FolderId.Equals(folderId)).FirstOrDefault();

            return Task.FromResult( folder );
        }

        /// <summary>
        /// TODO: Move to a dedicated TagService
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static Task<string[]> GetAllTagsAsync(string filter)
        {
            using var db = new ImageContext();
            var watch = new Stopwatch("GetAllTags");

            var tagQuery = db.Tags.Select(x => x.Keyword);

            if (!string.IsNullOrEmpty(filter))
            {
                string likeTerm = $"%{filter}%";

                tagQuery = tagQuery.Where(x => EF.Functions.Like(x, likeTerm));
            }

            var tags = tagQuery.OrderBy(x => x)
                        .ToArray();

            watch.Stop();

            return Task.FromResult(tags);
        }

        /// <summary>
        /// Get a single image
        /// </summary>
        /// <param name="imageId"></param>
        /// <returns></returns>
        public static async Task<Image> GetImage(int imageId, bool includeMetadata = true, bool includeTags = true )
        {
            using var db = new ImageContext();
            var watch = new Stopwatch("GetImage");
            Image image = null;
            try
            {
                IQueryable<Image> query = db.Images
                                            .Where(x => x.ImageId == imageId)
                                            .Include(x => x.Folder);

                if (includeMetadata)
                {
                    query = query.Include(x => x.MetaData)
                                 .Include(x => x.MetaData.Camera)
                                 .Include(x => x.MetaData.Lens)
                                 .Include(x => x.BasketEntries);
                }

                // Now execute the actual query
                image = await query.FirstOrDefaultAsync();
            }

            catch (Exception ex)
            {
                Logging.Log($"Exception retrieving image: {ex.Message}");
            }
            finally
            {
                watch.Stop();
            }

            if( includeTags )
                await db.LoadTags(image);

            return image;
        }

        public static List<List<Image>> GetImagesWithDuplicates()
        {
            using var db = new ImageContext();
            var watch = new Stopwatch("GetImagesWithDupes");

            // Craft the SQL manually as server-side groupby isn't supported by EF Core.
            // Select all images that have the same hash as more than one image.
            var subQuery = "SELECT hash from ImageMetaData where hash is not null and hash <> \"\" group by hash having count( distinct ImageID ) > 1";
            var sql = $"SELECT im.* from ImageMetaData im where im.hash in ({subQuery})";

            var dupes = db.ImageMetaData.FromSqlRaw(sql)
                                    .Where(x => x.Hash != null)
                                    .Include(x => x.Image)
                                    .ThenInclude(x => x.Folder)
                                    .ToList();

            // Backfill the metadata for the child image object, so we can select it.
            dupes.ForEach(x => x.Image.MetaData = x );

            // Group by the hash and pick all of the images for each one.
            var listOfLists = dupes.Select( x => x.Image )
                                      .GroupBy(x => x.MetaData.Hash)
                                      .Select( x => x.OrderBy( x => x.SortDate ).ToList() )
                                      .ToList();

            watch.Stop();

            return listOfLists;
        }

        public static List<Image> GetImageDuplicates(Image image)
        {
            using var db = new ImageContext();
            var watch = new Stopwatch("GetImageDupes");

            var dupes = db.ImageMetaData
                            .Where(x => x.Hash.Equals(image.MetaData.Hash) && x.ImageId != image.ImageId)
                            .Include( x => x.Image )
                            .ThenInclude( x => x.Folder )
                            .Select( x => x.Image );

            return dupes.ToList();
        }
    }
}
