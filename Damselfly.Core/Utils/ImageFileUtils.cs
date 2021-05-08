using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Damselfly.Core.Services;

namespace Damselfly.Core.Utils
{
    public static class ImageFileUtils
    {
        public static bool IsImageFileType(this FileInfo filename)
        {
            if (filename.IsHidden())
                return false;

            return ImageProcessService.Instance.SupportedFileExtensions.Any(x => x.Equals(filename.Extension, StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsSidecarFileType(this FileInfo filename)
        {
            if (filename.IsHidden())
                return false;

            return SidecarUtils.SidecarExtensions.Any(x => x.Equals(filename.Extension, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get all image files in a subfolder, and return them, ordered by
        /// the most recently updated first. 
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        public static List<FileInfo> SafeGetImageFiles(this DirectoryInfo folder)
        {
            var watch = new Stopwatch("GetFiles");

            try
            {
                var files = folder
                    .GetFiles()
                    .Where(x => x.IsImageFileType())
                    .OrderByDescending(x => x.LastWriteTimeUtc)
                    .ToList();

                return files;
            }
            catch (Exception ex)
            {
                Logging.LogWarning("Unable to read files from {0}: {1}", folder.FullName, ex.Message);
                return new List<FileInfo>();
            }
            finally
            {
                watch.Stop();
            }
        }
    }
}
