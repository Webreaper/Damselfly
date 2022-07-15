using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Damselfly.Core.Utils;
using Microsoft.EntityFrameworkCore;

namespace Damselfly.Core.Models;

/// <summary>
/// Metadata associated with an image. Also, an optional lens and camera. 
/// </summary>
public class ImageMetaData
{
    [Key]
    public int MetaDataId { get; set; }

    [Required]
    public virtual Image Image { get; set; }
    public int ImageId { get; set; }

    public DateTime DateTaken { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public double AspectRatio { get; set; } = 1;
    public int Rating { get; set; } // 1-5, stars
    public string Caption { get; set; }
    public string Copyright { get; set; }
    public string Credit { get; set; }
    public string Description { get; set; }
    public string ISO { get; set; }
    public string FNum { get; set; }
    public string Exposure { get; set; }
    public bool FlashFired { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public int? CameraId { get; set; }
    public virtual Camera Camera { get; set; }

    public int? LensId { get; set; }
    public virtual Lens Lens { get; set; }

    public string DominantColor { get; set; }
    public string AverageColor { get; set; }

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

    /// <summary>
    /// Temporary workaround for the fact that EFCore.BulkExtensions doesn't support joined
    /// updates in its BatchUpdateAsync method. So we just execute the raw SQL directly.
    /// </summary>
    /// <param name="db"></param>
    /// <param name="folderId"></param>
    /// <param name="updateField"></param>
    /// <param name="newValue"></param>
    /// <returns></returns>
    public static async Task<int> UpdateFields(ImageContext db, Folder folder, string updateField, string newValue)
    {
        string sql = $@"UPDATE ImageMetaData SET {updateField} = {newValue} FROM (SELECT i.ImageId, i.FolderId FROM Images i where i.FolderId = {folder.FolderId}) AS imgs WHERE imgs.ImageID = ImageMetaData.ImageID";

        try
        {
            return await db.Database.ExecuteSqlRawAsync(sql);
        }
        catch (Exception ex)
        {
            Logging.LogError($"Exception updating Metadata Field {updateField}: {ex.Message}");
            return 0;
        }
    }
}

