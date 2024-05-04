using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Damselfly.Core.Models;

/// <summary>
///     A keyword tag. Primarily IPTC tags, these are sets of
///     keywords that are used to associate metadata with an
///     image.
/// </summary>
public class Tag
{
    public enum TagTypes
    {
        IPTC = 0,
        Classification = 1
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int TagId { get; set; }

    [Required]
    public string Keyword { get; set; }

    public TagTypes TagType { get; set; }
    public bool Favourite { get; set; }

    public DateTime TimeStamp { get; init; } = DateTime.UtcNow;

    [JsonIgnore] // This JsonIgnore prevents circular references when serializing the Image entity
    public virtual List<ImageTag> ImageTags { get; init; } = new();

    [JsonIgnore] // This JsonIgnore prevents circular references when serializing the Image entity
    public virtual List<ImageObject> ImageObjects { get; init; } = new();

    public override string ToString()
    {
        return $"{TagType}: {Keyword} [{TagId}]";
    }

    public override int GetHashCode()
    {
        return Keyword.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        var objTag = obj as Tag;

        if ( objTag != null )
            return objTag.Keyword.Equals(Keyword, StringComparison.OrdinalIgnoreCase);

        return false;
    }
}

/// <summary>
///     A Free-Text Search Tag. Separate from 'Tag' because EF doesn't currently support
///     free-text search so we've had to roll our own a bit. This is used for results
///     deserialization.
/// </summary>
public class FTSTag
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int FTSTagId { get; set; }

    public string? Keyword { get; set; }
}