using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Damselfly.Core.Models;

/// <summary>
///     A basket entry represents a persistent selection of an image. So if a basket entry
///     exists, the image is in the basket. We can then perform operations on those entries
///     (export, etc).
/// </summary>
public class BasketEntry
{
    [Key]
    
    public Guid BasketEntryId { get; set; } = new Guid();

    public DateTime DateAdded { get; set; } = DateTime.UtcNow;

    [Required]
    public virtual Image Image { get; set; }

    public Guid ImageId { get; set; }

    [Required]
    public virtual Basket Basket { get; set; }

    public Guid BasketId { get; set; }

    public override string ToString()
    {
        return $"{Image.FileName} [{Image.ImageId} - added {DateAdded}]";
    }
}