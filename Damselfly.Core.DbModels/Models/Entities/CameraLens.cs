using System.ComponentModel.DataAnnotations;

namespace Damselfly.Core.Models;

/// <summary>
///     A camera, which is associated with an image
/// </summary>
public class Camera
{
    [Key] public int CameraId { get; set; }

    public string Model { get; set; }
    public string Make { get; set; }
    public string Serial { get; set; }
}

/// <summary>
///     A lens, also associated with an image
/// </summary>
public class Lens
{
    [Key] public int LensId { get; set; }

    public string Model { get; set; }
    public string Make { get; set; }
    public string Serial { get; set; }
}