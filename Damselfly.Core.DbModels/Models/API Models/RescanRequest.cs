using System;
using System.Collections.Generic;
using Damselfly.Core.Constants;

namespace Damselfly.Core.DbModels.Models.APIModels;

public class RescanRequest
{
    public RescanTypes ScanType { get; set; }
    public Guid? FolderId { get; set; }
    public ICollection<Guid>? ImageIds { get; set; }
    public bool RescanAll { get; set; } = false;
}