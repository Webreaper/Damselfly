using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Damselfly.Core.Models;

/// <summary>
///     Many-to-many relationship table joining images and tags.
/// </summary>
public class ImageTag
{
    [Key]
    
    public Guid ImageId { get; set; }

    public virtual Image Image { get; set; }

    [Key]
    
    public Guid TagId { get; set; } = new Guid();

    public virtual Tag Tag { get; set; }

    public override string ToString()
    {
        return $"{Image.FileName}=>{Tag.Keyword} [{ImageId}, {TagId}]";
    }

    public override bool Equals(object? obj)
    {
        var objTag = obj as ImageTag;

        if ( objTag != null )
            return objTag.ImageId.Equals(ImageId) && objTag.TagId.Equals(TagId);

        return false;
    }

    public override int GetHashCode()
    {
        return ImageId.GetHashCode() + '_' + TagId.GetHashCode();
    }
}