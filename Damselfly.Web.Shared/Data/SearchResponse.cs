using System;
using Damselfly.Core.Models;

namespace Damselfly.Web.Models.Data;

public class SearchResponse
{
    public bool MoreDataAvailable { get; set; }
    public Image[] SearchResults { get; set; }
}


