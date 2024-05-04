using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Damselfly.Core.Models;

/// <summary>
///     A camera, which is associated with an image
/// </summary>
public class Camera
{
    [Key]
    
    public Guid CameraId { get; set; } = new Guid();

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
    
    public Guid LensId { get; set; } = new Guid();

    public string? Model { get; set; }
    public string? Make { get; set; }
    public string? Serial { get; set; }
}