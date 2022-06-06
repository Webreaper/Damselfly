using System;
using System.ComponentModel.DataAnnotations;

namespace Damselfly.Core.Models;

/// <summary>
/// A person
/// </summary>
public class Person
{
    public enum PersonState
    {
        Unknown = 0,
        Identified = 1
    };

    [Key]
    public int PersonId { get; set; }

    [Required]
    public string Name { get; set; } = "Unknown";

    public PersonState State { get; set; } = PersonState.Unknown;
    public string AzurePersonId { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.MinValue;

    public override string ToString()
    {
        return $"{PersonId}=>{Name} [{State}, AzureID: {AzurePersonId}]";
    }
}

