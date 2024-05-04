using System;
namespace Damselfly.Core.DbModels.Models.APIModels;

public class NameChangeRequest
{
    public Guid? PersonId { get; set; }
    public Guid? ImageObjectId { get; set; }
    public string NewName { get; set; }
    public bool Merge { get; set; }
}

