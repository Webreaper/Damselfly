using System;
using System.ComponentModel.DataAnnotations;

namespace Damselfly.Core.Models;

/// <summary>
///     A person
/// </summary>
public class Person
{
    public enum PersonState
    {
        Unknown = 0,
        Identified = 1
    }

    [Key] public int PersonId { get; set; }

    [Required] public string Name { get; set; } = "Unknown";

    public PersonState State { get; set; } = PersonState.Unknown;
    public string? PersonGuid { get; set; }

    // This is the set of face properties, stored as a comma-separated list of floats.
    public string Embeddings { get; set; }
    
    public DateTime LastUpdated { get; set; } = DateTime.MinValue;

    public override string ToString()
    {
        return $"{PersonId}=>{Name} [{State}, GUID: {PersonGuid}]";
    }
}