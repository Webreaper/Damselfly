using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Damselfly.Core.Models;

/// <summary>
///     A camera, which is associated with an image
/// </summary>
public class Camera
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int CameraId { get; set; }

    public string? Model { get; set; }
    public string? Make { get; set; }
    public string? Serial { get; set; }
}

/// <summary>
///     A lens, also associated with an image
/// </summary>
public class Lens
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int LensId { get; set; }

    public string? Model { get; set; }
    public string? Make { get; set; }
    public string? Serial { get; set; }
}