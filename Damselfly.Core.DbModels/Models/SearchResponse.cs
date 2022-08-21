using System;
using System.Collections.Generic;
using Damselfly.Core.Models;

namespace Damselfly.Core.DbModels;

public class SearchResponse
{
    public bool MoreDataAvailable { get; set; }
    public int[] SearchResults { get; set; }
}

public class SearchRequest
{
    public SearchQuery Query { get; set; }
    public int First { get; set; }
    public int Count { get; set; }
}

public class ImageRequest
{
    public List<int> ImageIds { get; set; }
}

public class ImageResponse
{
    public List<Image> Images { get; set; }
}
