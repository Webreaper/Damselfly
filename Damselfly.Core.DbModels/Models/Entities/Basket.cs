using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Damselfly.Core.DbModels.Authentication;

namespace Damselfly.Core.Models;

/// <summary>
///     A saved basket of images - witha a description and saved date.
/// </summary>
public class Basket
{
    [Key]
    
    public Guid BasketId { get; set; } = new Guid();

    public DateTime DateAdded { get; set; } = DateTime.UtcNow;
    public string? Name { get; set; }

    public int? UserId { get; set; }
    public virtual AppIdentityUser User { get; set; }

    public virtual List<BasketEntry> BasketEntries { get; init; } = new();
}