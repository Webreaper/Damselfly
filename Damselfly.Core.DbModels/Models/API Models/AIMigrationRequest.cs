namespace Damselfly.Core.DbModels.Models.APIModels;

public class AIMigrationRequest
{
    public bool MigrateAllImages { get; set;  }
    public bool MigrateImagesWithFaces { get; set; }
}