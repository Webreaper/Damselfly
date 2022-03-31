using System;
using System.Linq;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace Damselfly.Core.Utils;

/// <summary>
/// Utilities for extracting data from Metadata
/// </summary>
public static class ExifUtils
{
    public static string SafeExifGetString(this Directory dir, int tagType)
    {
        try
        {
            return dir?.GetString(tagType);
        }
        catch
        {
            Logging.LogVerbose("Error reading string metadata!");
            return null;
        }
    }

    public static string SafeExifGetString(this Directory dir, string tagName)
    {
        try
        {
            var tag = dir?.Tags.FirstOrDefault(x => x.Name == tagName);

            return tag?.Description;
        }
        catch
        {
            Logging.LogVerbose("Error reading string metadata!");
            return null;
        }
    }

    public static int SafeGetExifInt(this Directory dir, int tagType)
    {
        int retVal = 0;
        try
        {
            var val = dir?.GetInt32(tagType);
            if (val.HasValue)
                retVal = val.Value;
        }
        catch
        {
            Logging.LogVerbose("Error reading int metadata!");
        }

        return retVal;
    }

    public static DateTime SafeGetExifDateTime(this Directory dir, int tagType)
    {
        DateTime retVal = DateTime.MinValue;
        try
        {
            var val = dir?.GetDateTime(tagType);
            if (val.HasValue)
                retVal = val.Value;
        }
        catch
        {
            Logging.LogVerbose("Error reading date/time metadata!");
        }

        return retVal;
    }
}
