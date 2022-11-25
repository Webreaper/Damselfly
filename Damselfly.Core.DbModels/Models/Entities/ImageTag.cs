using System.ComponentModel.DataAnnotations;

namespace Damselfly.Core.Models;

/// <summary>
///     Many-to-many relationship table joining images and tags.
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