using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace Damselfly.Core.Models;

/// <summary>
///     Metadata associated with an image. Also, an optional lens and camera.
/// </summary>
public class ImageMetaData
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int MetaDataId { get; set; }

    [Required] public virtual Image Image { get; set; }

    public int ImageId { get; set; }

    public DateTime DateTaken { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public double AspectRatio { get; set; } = 1;
    public int Rating { get; set; } // 1-5, stars
    public string? Caption { get; set; }
    public string? Copyright { get; set; }
    public string? Credit { get; set; }
    public string? Description { get; set; }
    public string? ISO { get; set; }
    public string? FNum { get; set; }
    public string? Exposure { get; set; }
    public bool FlashFired { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public int? CameraId { get; set; }
    public virtual Camera Camera { get; set; }

    public int? LensId { get; set; }
    public virtual Lens Lens { get; set; }

    public string? DominantColor { get; set; }
    public string? AverageColor { get; set; }

    // The date that this metadata was read from the image
    // If this is older than Image.LastUpdated, the image
    // will be re-indexed
    public DateTime LastUpdated { get; set; }

    // Date the thumbs were last created. If this is null
    // the thumbs will be regenerated
    public DateTime? ThumbLastUpdated { get; set; }

    // Date we last performed face/object/image recognition
    // If this is null, AI will be reprocessed
    public DateTime? AILastUpdated { get; set; }

    public void Clear()
    {
        this.DateTaken = DateTime.MinValue;
        this.Height = 0;
        this.Width = 0;
        this.Description = null;
        this.Caption = null;
        this.DominantColor = null;
        this.AspectRatio = 1;
        this.Rating = 0;
        this.Credit = null;
        this.ISO = null;
        this.FNum = null;
        this.Exposure = null;
        this.FNum = null;
        this.FlashFired = false;
        this.Latitude = null;
        this.Longitude = null;
    }
}