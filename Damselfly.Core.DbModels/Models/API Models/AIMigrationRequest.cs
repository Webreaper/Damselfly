namespace Damselfly.Core.DbModels.Models.API_Models;

public class AIMigrationRequest
{
    public bool MigrateAllImages { get; set;  }
    public bool MigrateImagesWithFaces { get; set; }
}