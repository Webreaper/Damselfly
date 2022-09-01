using System;
namespace Damselfly.Core.DbModels.Models.APIModels;

public class ConfigSetRequest
{
    public string Name { get; set; } 
    public string NewValue { get; set; }
    public int? UserId { get; set; }
}

