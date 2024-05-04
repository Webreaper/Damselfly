using System;
using System.Collections.Generic;
using Damselfly.Core.Models;

namespace Damselfly.Core.DbModels.Models.APIModels;

public class DownloadRequest
{
    public ICollection<Guid> ImageIds { get; set; }
    public ExportConfig Config { get; set; }
    public string? Password { get; set; }
}

public class DownloadResponse
{
    public string DownloadUrl { get; set; }
}