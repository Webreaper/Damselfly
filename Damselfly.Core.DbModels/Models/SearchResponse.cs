using System;
using Damselfly.Core.Models;

namespace Damselfly.Core.DbModels;

public class SearchResponse
{
    public bool MoreDataAvailable { get; set; }
    public Image[] SearchResults { get; set; }
}

public class SearchRequest
{
    public SearchQuery Query { get; set; }
    public int First { get; set; }
    public int Count { get; set; }
}

