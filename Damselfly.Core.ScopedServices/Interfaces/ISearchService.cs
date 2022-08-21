using System;
using Damselfly.Core.Constants;
using Damselfly.Core.DbModels;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface ISearchService
{
    event Action OnSearchChanged;

    ICollection<int> SearchResults { get; }

    string SearchText { get; set; }
    Folder Folder { get; set; }
    Person Person { get; set;  }
    Tag Tag { get; set; }
    Image SimilarTo { get; set; }
    DateTime? MaxDate { get; set; }
    DateTime? MinDate { get; set; }
    int? CameraId { get; set; }
    int? LensId { get; set; }
    int? Month { get; set; }
    int? MinRating { get; set; }
    int? MaxSizeKB { get; set; }
    int? MinSizeKB { get; set; }
    bool TagsOnly { get; set; }
    bool IncludeAITags { get; set; }
    bool UntaggedImages { get; set; }
    FaceSearchType? FaceSearch { get; set; }
    GroupingType Grouping { get; set; }
    SortOrderType SortOrder { get; set; }
    OrientationType? Orientation { get; set; }
    void SetDateRange(DateTime? min, DateTime? max);

    string SearchBreadcrumbs { get; }

    void Refresh();
    void Reset();
    void NotifyStateChanged(); // TODO: Do we need this?

    Task<SearchResponse> GetQueryImagesAsync(int start, int count);
}

