using System;

namespace Damselfly.Core.DbModels.Models.APIModels;

public class NameChangeRequest
{
    public int? PersonId { get; set; }
    public int? ImageObjectId { get; set; }
    public string NewName { get; set; }
    public bool Merge { get; set; }
}