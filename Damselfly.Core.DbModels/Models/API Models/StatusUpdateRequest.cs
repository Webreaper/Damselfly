using System;
namespace Damselfly.Core.DbModels.Models.APIModels;

public class StatusUpdateRequest
{
    public string NewStatus { get; set; }
    public int? UserId { get; set; }
}

