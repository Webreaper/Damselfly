using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Damselfly.Core.Models;

/// <summary>
///     An image classification detected via ML
/// </summary>
public class ImageClassification
{
    [Key]
    
    public Guid ClassificationId { get; set; } = new Guid();

    public string? Label { get; set; }

    public override string ToString()
    {
        return $"{Label} [{ClassificationId}]";
    }
}