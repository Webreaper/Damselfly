using System.Collections.Generic;

namespace Damselfly.Core.DbModels.Models.TransformationModels;

public class TransformationSequence
{
    public ICollection<TransformBase> Transforms { get; set; }
}

