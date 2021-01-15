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
        public static Task<Image> GetImage(int imageId, bool includeMetadata = true )
        {
            using var db = new ImageContext();
            var watch = new Stopwatch("GetImage");
            Image image = null;
            try
            {
                IQueryable<Image> query = db.Images.Where(x => x.ImageId == imageId)
                            .Include(x => x.Folder);

                if (includeMetadata)
                {
                    query = query.Include(x => x.MetaData.Camera)
                                 .Include(x => x.MetaData.Lens)
                                 .Include(x => x.BasketEntry);
                }

                // Now execute the actual query
                image = query.FirstOrDefault();
            }

            catch (Exception ex)
            {
                Logging.Log($"Exception retrieving image: {ex.Message}");
            }
            finally
            {
                watch.Stop();
            }

            db.LoadTags(image);

            return Task.FromResult(image);
        }

        public static List<List<Image>> GetImagesWithDuplicates()
        {
            using var db = new ImageContext();
            var watch = new Stopwatch("GetImagesWithDupes");

            // Craft the SQL manually as server-side groupby isn't supported by EF Core.
            var sql = "SELECT im.* from ImageMetaData im where im.hash in (SELECT hash from ImageMetaData where hash is not null group by hash having count( distinct ImageID ) > 1)";

            var dupeImageMetaData = db.ImageMetaData.FromSqlRaw(sql)
                                      .Include(x => x.Image)
                                      .ThenInclude(x => x.Folder)
                                      .Select( x => x.Image )
                                      .ToList();

            var listOfLists = dupeImageMetaData.Where( x => x.MetaData != null )
                                      .GroupBy(x => x.MetaData.Hash)
                                      .Where( x => x != null )
                                      .Select( x => x.ToList() )
                                      .ToList();

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

        public static string GetImageThumbUrl(Image image, ThumbSize size)
        {
            string url = "/no-image.jpg";

            if (image.Folder != null)
            {
                var file = new FileInfo(image.FullPath);

                var path = ThumbnailService.Instance.GetThumbRequestPath(file, size, "/no-image.png");

                // This is a bit tricky. We need to UrlEncode all of the folders in the path
                // but we don't want to UrlEncode the slashes itself. So we have to split,
                // UrlEncode them all, and rejoin.
                //var parts = path.Split(Path.DirectorySeparatorChar)
                //                .Select(x => HttpUtility.UrlEncode(x) );
                // path = string.Join(Path.DirectorySeparatorChar, parts);

                url = path.Replace( "#", "%23" );
            }
            else
            {
                Logging.Log("ERROR: No folder for image {0}", image.FileName);
            }

            return url;
        }
    }
}
