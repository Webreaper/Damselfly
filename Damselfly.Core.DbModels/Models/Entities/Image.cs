using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using Damselfly.Core.Constants;

namespace Damselfly.Core.Models;

/// <summary>
///     An image, or photograph file on disk. Has a folder associated
///     with it. There's a BasketEntry which, if it exists, indicates
///     the picture is selected.
///     It also has a many-to-many relationship with IPTC keyword tags; so
///     a tag can apply to many images, and an image can have many tags.
/// </summary>
public class Image
{
    [Key] public int ImageId { get; set; }

    public int FolderId { get; set; }
    public virtual Folder Folder { get; set; }

    // Image File metadata
    public string? FileName { get; set; }
    public int FileSizeBytes { get; set; }
    public DateTime FileCreationDate { get; set; }
    public DateTime FileLastModDate { get; set; }

    // Date used for search query orderby
    public DateTime SortDate { get; set; }

    // Damselfy state metadata
    public DateTime LastUpdated { get; set; }

    public virtual ImageMetaData MetaData { get; set; }
    public virtual Hash Hash { get; set; }

    // An image may have a set of image transforms
    public virtual Transformations? Transforms { get; set; }

    // An image can appear in many baskets
    public virtual List<BasketEntry> BasketEntries { get; init; } = new();

    // An image can have many tags
    public virtual List<ImageTag> ImageTags { get; init; } = new();

    // Machine learning fields
    public int? ClassificationId { get; set; }
    public virtual ImageClassification Classification { get; set; }
    public double ClassificationScore { get; set; }

    // NOTE: setter needed for serialization only
    public virtual List<ImageObject> ImageObjects { get; init; } = new();

    [NotMapped] public string FullPath => Path.Combine(Folder.Path, FileName);

    [NotMapped] public string RawImageUrl => $"/rawimage/{ImageId}";

    [NotMapped] public string DownloadImageUrl => $"/dlimage/{ImageId}";

    public override string ToString()
    {
        return $"{FileName} [{ImageId}]";
    }

    /// <summary>
    ///     URL mapped with last-updated time to ensure we always refresh the thumb
    ///     when the image is updated.
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    public string ThumbUrl(ThumbSize size)
    {
        var nocacheDate = this.MetaData?.ThumbLastUpdated;
        
        return $"/thumb/{size}/{ImageId}?nocache={nocacheDate:yyyyMMddHHmmss}";
    }

    public void FlagForMetadataUpdate()
    {
        LastUpdated = DateTime.UtcNow;
    }
}