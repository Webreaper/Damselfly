using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Damselfly.Core.DbModels.Authentication;

namespace Damselfly.Core.Models;

/// <summary>
///    A transaction sequence of transformations (crop, rotate, etc)
/// </summary>
public class Transformations
{
    [Key]
    
    public Guid TransformationId { get; set; } = new Guid();

    [Required] public virtual Image Image { get; set; }

    public Guid ImageId { get; set; }

    public string TransformsJson { get; set; }
}