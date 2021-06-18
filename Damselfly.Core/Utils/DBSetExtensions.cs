using System;
using System.Linq;
using System.Linq.Expressions;
using Damselfly.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Damselfly.Core.Utils
{
    /// <summary>
    /// Some helper extension methods for DbSet operations
    /// </summary>
    public static class DbSetExtensions
    {
        /// <summary>
        /// Useful little helper to add an item but only if it's not already in the set,
        /// according to the predicate passed.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="set"></param>
        /// <param name="predicate"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static EntityEntry<T> AddIfNotExists<T>(this DbSet<T> set, Expression<Func<T, bool>> predicate, T entity) where T : class, new()
        {
            return !set.Any(predicate) ? set.Add(entity) : null;
        }

        /// <summary>
        /// Because of this issue: https://github.com/dotnet/efcore/issues/19418
        /// we have to explicitly load the tags, rather than using eager loading.
        /// This helper method does that.
        /// TODO: Remove this method when that issue is closed.
        /// </summary>
        /// <param name="db">Context</param>
        /// <param name="img">Image for which we want to load tags</param>
        public static void LoadTags(this ImageContext db, Image img)
        {
            var watch = new Stopwatch("LoadImageTags");

            try
            {
                // Need to ensure the image is re-attached to the
                // context if we didn't load it with this one
                db.Attach(img);

                if (!img.ImageTags.Any())
                {
                    // Now load the tags
                    db.Entry(img).Collection(e => e.ImageTags)
                                .Query()
                                .Include(e => e.Tag)
                                .Load();
                }

                if (!img.ImageObjects.Any())
                {
                    db.Entry(img).Collection(e => e.ImageObjects)
                                 .Query()
                                 .Include(x => x.Tag)
                                 .Include(x => x.Person)
                                 .Load();
                }
            }
            catch (Exception ex)
            {
                Logging.Log($"Exception retrieving image {img.ImageId}'s tags: {ex.Message}");
            }
            finally
            {
                watch.Stop();
            }
        }
    }
}
