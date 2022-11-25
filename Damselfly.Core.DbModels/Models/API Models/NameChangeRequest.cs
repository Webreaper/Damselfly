using System;
namespace Damselfly.Core.DbModels.Models.APIModels;

public class NameChangeRequest
{
    public int ObjectId { get; set; }
    public string NewName { get; set; }
}

