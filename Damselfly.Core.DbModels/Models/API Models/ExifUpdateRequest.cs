using System;
using System.Collections.Generic;
using Damselfly.Core.Models;

namespace Damselfly.Core.DbModels.Models.APIModels;

public class ExifUpdateRequest
{
    public ICollection<Guid> ImageIDs { get; set; }
    public ExifOperation.ExifType ExifType { get; set; }
    public string NewValue { get; set; }
    public int? UserId { get; set; }
}