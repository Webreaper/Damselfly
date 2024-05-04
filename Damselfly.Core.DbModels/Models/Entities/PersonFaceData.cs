using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Damselfly.Core.Models;

/// <summary>
/// A set of face datapoints for a person
/// There may be more than one set of datapoints per person.
/// </summary>
public class PersonFaceData
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int FaceDataId { get; set; }
    
    public int PersonId { get; set; }
    public virtual Person Person { get; set; }

    // This is the set of face properties, stored as a comma-separated list of floats.
    public string Embeddings { get; set; }
    
    public float Score { get; set; }

    public override string ToString()
    {
        return $"{FaceDataId}=>{PersonId} [{Embeddings}]";
    }
}