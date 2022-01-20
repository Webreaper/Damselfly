using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Damselfly.Core.Utils
{
    /// <summary>
    /// Helper extensions for file operations
    /// </summary>
    public static class FileUtils
    {
        public static bool IsDirectory(this FileInfo file)
        {
            return (file.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
        }

        /// <summary>
        /// See if a file is hidden, or is in a hidden directory structure
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static bool IsHidden( this FileInfo file )
        {
            // Ignore all hidden files.
            if( (file.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                return true;

            // Files are considered hidden if they're in a hidden folder too
            var dir = file.Directory;

            while( dir != null )
            {
                if ((dir.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                {
                    // Special case: on Windows, C:\ seems to always have the hidden
                    // attribute, which can result in the whole tree being assumed
                    // to be hidden. So we check for that by looking for the Parent
                    // to be null, and if it is, we ignore the hidden attribute.
                    // See https://github.com/Webreaper/Damselfly/issues/333
                    if (dir.Parent != null)
                        return true;
                }

                dir = dir.Parent;
            }                

            return false;
        }

        /// <summary>
        /// Little wrapper for managing relative paths without trailing slashes.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="root"></param>
        /// <returns></returns>
        public static string MakePathRelativeTo(this string path, string root)
        {
            if (!root.EndsWith(Path.DirectorySeparatorChar))
                root += Path.DirectorySeparatorChar;

            var result = Path.GetRelativePath(root, path);
            return result;
        }

        /// <summary>
        /// Predicate to filter out unwanted folders. We don't care about
        /// folders which are hidden (including .folder) and we don't want
        /// to look at @eaDir folders on Synology.
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        public static bool IsMonitoredFolder( this DirectoryInfo folder )
        {
            if (!folder.Exists)
                return false;

            if((folder.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden )
                return false;

            if (folder.FullName.Contains("@eaDir", StringComparison.OrdinalIgnoreCase))
                return false;

            if (folder.Name.StartsWith("."))
                return false;

            if (File.Exists(Path.Combine(folder.FullName, ".nomedia")))
                return false;

            // If the folder, or any of its descendents, start with a ., skip
            var parent = folder;
            while (parent != null)
            {
                if (parent.Name.StartsWith("."))
                    return false;

                parent = parent.Parent;
            }

            return true;
        }

        /// <summary>
        /// Extension to get all of the subfolders recursively for a particular folder,
        /// but wrapped up with exception handling, and some logging.
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        public static List<DirectoryInfo> SafeGetSubDirectories(this DirectoryInfo folder)
        {
            List<DirectoryInfo> subDirs;

            var watch = new Stopwatch("GetSubFolders");

            try
            {
                // Now, recurse - first get the folders on the disk. Skip syno eaDir folders (which
                // contain thumbs), and hidden folders.
                subDirs = folder.GetDirectories()
                                .OrderByDescending(x => x.LastWriteTimeUtc)
                                .ToList();
            }
            catch (Exception ex)
            {
                Logging.LogWarning("Unable to read sub-directories from {0}: {1}", folder.FullName, ex.Message);
                subDirs = new List<DirectoryInfo>();
            }
            finally
            {
                watch.Stop();
            }

            return subDirs;
        }

        /// <summary>
        /// Delta-based time comparison (returns true for any 
        /// times within 5s of each other).
        /// </summary>
        /// <param name="time1"></param>
        /// <param name="time2"></param>
        /// <returns>True if the times are approximately equivalent</returns>
        public static bool TimesMatch(this DateTime time1, DateTime time2)
        {
            const int delta = 5; // seconds
            var timeDiff = time1 - time2;
            if (Math.Abs(timeDiff.TotalSeconds) < delta)
                return true;

            return false;
        }

        /// <summary>
        /// Delta-based time comparisonfor file last-write/last-mod times.
        /// Returns true for a file that was modified within 5s of the
        /// last-write time.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="lastModTimeUtc"></param>
        /// <returns>True if the times are approximately equivalent</returns>
        public static bool WriteTimesMatch(this FileInfo file, DateTime lastModTimeUtc)
        {
            const int delta = 5; // seconds
            var timeDiff = file.LastWriteTimeUtc - lastModTimeUtc;
            if (Math.Abs(timeDiff.TotalSeconds) < delta)
                return true;

            return false;
        }

        /// <summary>
        /// Returns true if the file is more recent than the time passed in
        /// </summary>
        /// <param name="file"></param>
        /// <param name="lastModTimeUtc"></param>
        /// <returns>True if the times are approximately equivalent</returns>
        public static bool FileIsMoreRecentThan(this FileInfo file, DateTime timestamp)
        {
            return file.LastWriteTimeUtc > timestamp;
        }

        /// <summary>
        /// Delta-based time comparisonfor file last-write/last-mod times.
        /// Returns true for a file that was modified within 5s of the
        /// last-write time.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="createTimeUtc"></param>
        /// <returns>True if the times are approximately equivalent</returns>
        public static bool CreateTimesMatch(this FileInfo file, DateTime createTimeUtc)
        {
            const int delta = 5; // seconds
            var timeDiff = file.CreationTimeUtc - createTimeUtc;
            if (Math.Abs(timeDiff.TotalSeconds) < delta)
                return true;

            return false;
        }

        /// <summary>
        /// Delta-based time comparisonfor file last-write/last-mod times.
        /// Returns true for a file that was modified within 5s of the
        /// last-write time.
        /// </summary>
        /// <returns>True if the times are approximately equivalent</returns>
        public static bool WriteTimesMatch(this FileInfo file1, string path2)
        {
            var time2 = File.GetLastWriteTimeUtc(path2);
            return file1.WriteTimesMatch(time2);
        }

        /// <summary>
        /// Delta-based time comparisonfor file last-write/last-mod times.
        /// Returns true for a file that was modified within 5s of the
        /// last-write time.
        /// </summary>
        /// <returns>True if the times are approximately equivalent</returns>
        public static bool CreateTimesMatch(this FileInfo file1, string path2)
        {
            var time2 = File.GetCreationTimeUtc(path2);
            return file1.CreateTimesMatch(time2);
        }

        /// <summary>
        /// Delta-based time comparisonfor file last-write/last-mod times.
        /// Returns true for a file that was modified within 5s of the
        /// last-write time.
        /// </summary>
        /// <returns>True if the times are approximately equivalent</returns>
        public static bool WriteTimesMatch( this FileInfo file1, FileInfo file2 )
        {
            return file1.WriteTimesMatch(file2.LastWriteTimeUtc);
        }

        /// <summary>
        /// Delete a file, but wrap up any exceptions caught to make the
        /// calling code cleaner.
        /// </summary>
        /// <param name="fileToDelete"></param>
        /// <returns></returns>
        public static bool SafeDelete( this FileInfo fileToDelete )
        {
            if( fileToDelete.Exists )
            {
                try
                {
                    fileToDelete.Delete();
                    Logging.LogWarning("Deleted file {0}", fileToDelete.FullName);
                    return true;
                }
                catch (Exception ex)
                {
                    Logging.LogWarning("Unable to delete file: {0}, {1}", fileToDelete.FullName, ex.Message);
                }
            }

            return false;
        }
    }
}
