using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Damselfly.Core.Models;

/// <summary>
/// Many-to-many relationship table joining images and tags.
/// </summary>
public class ImageTag
{
    [Key]
    public int ImageId { get; set; }
    public virtual Image Image { get; set; }

    [Key]
    public int TagId { get; set; }
    public virtual Tag Tag { get; set; }

    public override string ToString()
    {
        return $"{Image.FileName}=>{Tag.Keyword} [{ImageId}, {TagId}]";
    }

    public override bool Equals(object obj)
    {
        ImageTag objTag = obj as ImageTag;

        if (objTag != null)
            return objTag.ImageId.Equals(this.ImageId) && objTag.TagId.Equals(this.TagId);

        return false;
    }

    public override int GetHashCode()
    {
        return ImageId.GetHashCode() + '_' + TagId.GetHashCode();
    }
}