using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Damselfly.Core.Models;
using Damselfly.Core.Utils;
using System.Threading.Tasks;
using EFCore.BulkExtensions;

namespace Damselfly.Core.Services
{
    /// <summary>
    /// Service to add/remove/update tags on image files on disk. This uses
    /// ExifTool to do the actual EXIF tag manipulation on the disk files,
    /// since there aren't currently any native C# libraries which can
    /// efficiently write tags to images without re-encoding the JPEG data
    /// (which would be lossy, and therefore destructive). Plus ExifTool
    /// is FAST!
    /// </summary>
    public class MetaDataService
    {
        public static MetaDataService Instance { get; private set; }

        public MetaDataService()
        {
            Instance = this;
        }

        /// <summary>
        /// Add a list of IPTC tags to an image on the disk.
        /// </summary>
        /// <param name="images"></param>
        /// <param name="tagToAdd"></param>
        /// <returns></returns>
        public async Task AddTagAsync(Image[] images, string tagToAdd)
        {
            var tagList = new List<string> { tagToAdd };
            await UpdateTagsAsync(images, tagList, null);
        }

        /// <summary>
        /// Takes an image and a set of keywords, and writes them to the DB queue for
        /// keywords to be added. These will then be processed asynchronously.
        /// </summary>
        /// <param name="images"></param>
        /// <param name="tagsToAdd"></param>
        /// <param name="tagsToRemove"></param>
        /// <returns></returns>
        public async Task UpdateTagsAsync(Image[] images, List<string> tagsToAdd, List<string> tagsToRemove)
        {
            var timestamp = DateTime.UtcNow;
            var changeDesc = string.Empty;

            using var db = new ImageContext();
            var keywordOps = new List<KeywordOperation>();

            if (tagsToAdd != null)
            {
                foreach (var image in images)
                {
                    keywordOps.AddRange(tagsToAdd.Select(keyword => new KeywordOperation
                    {
                        ImageId = image.ImageId,
                        Keyword = keyword,
                        Operation = KeywordOperation.OperationType.Add,
                        TimeStamp = timestamp
                    }));
                }

                changeDesc += $"added: {string.Join(',', tagsToAdd)}";
            }

            if (tagsToRemove != null)
            {
                foreach (var image in images)
                {
                    keywordOps.AddRange( tagsToRemove.Select(keyword => 
                        new KeywordOperation
                        {
                            ImageId = image.ImageId,
                            Keyword = keyword,
                            Operation = KeywordOperation.OperationType.Remove,
                            TimeStamp = timestamp
                        } ) );
                }

                if (!string.IsNullOrEmpty(changeDesc))
                    changeDesc += ", ";
                changeDesc += $"removed: {string.Join(',', tagsToRemove)}";
            }

            Logging.Log($"Bulk inserting {keywordOps.Count()} keyword operations (for {images.Count()}) into queue. ");

            try
            {
                db.BulkInsert(keywordOps);

                StatusService.Instance.StatusText = $"Saved tags ({changeDesc}) for {images.Count()} images.";
                await Task.Run(() =>
                            {
                                // Temp
                                return Task.FromResult<bool>(PerformKeywordOpScan());
                            });
            }
            catch (Exception ex)
            {
                Logging.LogError($"Exception inserting keyword operations: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper method to actually run ExifTool and update the tags on disk.
        /// </summary>
        /// <param name="imagePath"></param>
        /// <param name="tagsToAdd"></param>
        /// <param name="tagsToRemove"></param>
        /// <returns></returns>
        private bool UpdateTags(string imagePath, List<KeywordOperation> keywordOps )
        {
            bool success = false;
            if (File.Exists(imagePath))
            {
                Logging.LogVerbose("Updating tags for file {0}", imagePath);

                string args = string.Empty;

                foreach (var op in keywordOps)
                {
                    if (op.Operation == KeywordOperation.OperationType.Add)
                    {
                        Logging.Log($" Adding keyword {op.Keyword} to {op.Image.FileName}");
                        args += $" -keywords+=\"{op.Keyword}\" ";
                    }
                    else
                    {
                        Logging.Log($" Removing keyword {op.Keyword} from {op.Image.FileName}");
                        args += $" -keywords-=\"{op.Keyword}\" ";
                    }
                }

                args += " -overwrite_original";
                args += " \"" + imagePath + "\"";

                var process = new ProcessStarter();

                // Fix perl local/env issues for exiftoola
                var env = new Dictionary<string, string>();
                env["LANGUAGE"] = "en_US.UTF-8";
                env["LANG"] = "en_US.UTF-8";
                env["LC_ALL"] = "en_US.UTF-8";

                success = process.StartProcess("exiftool", args, env);

                if (!success)
                {
                    keywordOps.ForEach(x => x.State = KeywordOperation.FileWriteState.Failed);
                    Logging.LogWarning("ExifTool Tag update failed for image: {0}", imagePath);
                }
                else
                {
                    keywordOps.ForEach(x => x.State = KeywordOperation.FileWriteState.Written);
                }
            }

            return success;
        }

        /// <summary>
        /// Clean up processed keyword operations
        /// </summary>
        /// <param name="cleanupFreq"></param>
        public void CleanUpKeywordOperations(TimeSpan cleanupFreq)
        {
            using var db = new ImageContext();

            // Clean up completed operations older than 24hrs
            var cutOff = DateTime.UtcNow.AddDays(-1);

            try
            {
                int cleanedUp = db.KeywordOperations.Where(op => op.State == KeywordOperation.FileWriteState.Written
                                                                          && op.TimeStamp < cutOff )
                                    .BatchDelete();

                Logging.LogVerbose($"Cleaned up {cleanedUp} completed Keyword Operations.");
            }
            catch( Exception ex )
            {
                Logging.LogError($"Exception whilst cleaning up keyword operations: {ex.Message}");
            }
        }

        /// <summary>
        /// Process a batch of pending keyword tag operations.
        /// </summary>
        public bool PerformKeywordOpScan()
        {
            Logging.LogVerbose("Processing pending keyword operations");

            try
            {
                var watch = new Stopwatch("KeywordOps", -1);

                using var db = new ImageContext();

                bool complete = false;

                while (!complete)
                {
                    var queueQueryWatch = new Stopwatch("KeywordOpsQuery", -1);

                    // Find all images where there's either no metadata, or where the image
                    // was updated more recently than the image metadata
                    var tagsToProcess = db.KeywordOperations.AsQueryable()
                                            .Where( x => x.State == KeywordOperation.FileWriteState.Pending )
                                            .OrderByDescending(x => x.TimeStamp)
                                            .Take(100)
                                            .Include( x => x.Image )
                                            .Include( x => x.Image.Folder )
                                            .ToList();

                    queueQueryWatch.Stop();

                    complete = !tagsToProcess.Any();

                    if (!complete)
                    {
                        var conflatedOps = ConflateOperations(tagsToProcess);

                        var batchWatch = new Stopwatch("KeywordOpBatch", 100000);

                        foreach (var imageOpList in conflatedOps)
                        {
                            var image = imageOpList.Key;

                            // Write the actual tags to the image file on disk
                            if (UpdateTags(image.FullPath, imageOpList.Value))
                            {
                                // Updating the timestamp on the image to newer than its metadata will
                                // trigger its metadata and tags to be refreshed during the next scan
                                image.LastUpdated = DateTime.UtcNow;
                            }
                        }

                        var totals = string.Join(",", tagsToProcess.GroupBy(x => x.State)
                                        .Select(x => $"{x.Key}: {x.Count()}" )
                                        .ToList() );

                        Logging.Log($"Tag update complete: {string.Join(",", totals )}");

                        foreach( var op in tagsToProcess )
                        {
                            if (op.State == KeywordOperation.FileWriteState.Pending)
                            {
                                Logging.Log("Logic exception - pending tag after processing.");
                                continue;
                            }

                            // Flag the ops as updated. They should be either
                            // written, discarded or failed.
                            db.KeywordOperations.Update( op );
                        }

                        // TODO: Bulk update here.
                        db.SaveChanges("KeywordOpProcessed");

                        batchWatch.Stop();

                        Logging.Log($"Completed keyword op batch ({tagsToProcess.Count()} operations on {conflatedOps.Count()} images in {batchWatch.HumanElapsedTime}).");

                        StatusService.Instance.StatusText = $"Keywords processed. {totals}";
                    }
                }

                watch.Stop();
            }
            catch (Exception ex)
            {
                Logging.LogError($"Exception caught during keyword op scan: {ex}");
                return false;
            }

            Logging.LogVerbose("Keyword op scan Complete.");
            return true;
        }

        /// <summary>
        /// Takes a list of time-ordered keyword operations (add/remove) and for
        /// each keyword conflates down to a single distinct operation. So if there
        /// was:
        ///     Image1 Add 'cat'
        ///     Image2 Add 'cat'
        ///     Image2 Remove 'cat'
        ///     Image2 Add 'dog'
        ///     Image2 Add 'cat'
        ///     Image1 Remove 'cat'
        /// THen this would conflate down to: 
        ///     Image2 Add 'cat'
        ///     Image2 Add 'dog'
        ///     Image1 Remove 'cat'
        /// </summary>
        /// <param name="tagsToProcess"></param>
        /// <returns></returns>
        private IDictionary<Image, List<KeywordOperation>> ConflateOperations(List<KeywordOperation> tagsToProcess )
        {
            var result = new Dictionary<Image, List<KeywordOperation>>();

            var imageKeywords = tagsToProcess.GroupBy(x => x.Image);

            foreach( var imageOpList in imageKeywords )
            {
                var theImage = imageOpList.Key;
                var keywordOps = imageOpList.GroupBy(x => x.Keyword );

                // Now, for each keyword for this image, collect up the most recent
                // operation and store it in a dictionary. Note, keywords are
                // case-sensitive
                var keywordOpDict = new Dictionary<string, KeywordOperation>();

                foreach( var op in keywordOps )
                {
                    var orderedOps = op.OrderBy(x => x.TimeStamp).ToList();

                    foreach( var imageKeywordOp in orderedOps )
                    {
                        // Store the most recent op for each image/keyword,
                        // over-writing the previous
                        if( keywordOpDict.TryGetValue( imageKeywordOp.Keyword, out var existing ) )
                        {
                            // Update the state before it's replaced in the dict.
                            existing.State = KeywordOperation.FileWriteState.Discarded; 
                        }

                        keywordOpDict[imageKeywordOp.Keyword] = imageKeywordOp;
                    }
                }

                // By now we've got a dictionary of keywords/operations. Now we just
                // add them into the final result.
                result[theImage] = keywordOpDict.Values.ToList();
            }

            return result;
        }
    }
}
