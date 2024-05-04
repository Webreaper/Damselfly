using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

    [Key]
    
    public Guid PersonId { get; set; } = new Guid();

    [Required] public string Name { get; set; } = "Unknown";

    public PersonState State { get; set; } = PersonState.Unknown;
    public string? PersonGuid { get; set; }

    public virtual List<PersonFaceData> FaceData { get; init; } = new();

    public DateTime LastUpdated { get; set; } = DateTime.MinValue;

    public override string ToString()
    {
        return $"{PersonId}=>{Name} [{State}, GUID: {PersonGuid}]";
    }
}