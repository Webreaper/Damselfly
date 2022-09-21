using Damselfly.Core.Constants;
using Damselfly.Core.DbModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Utils;
using Humanizer;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.ScopedServices;

/// <summary>
///     The search service manages the current set of parameters that make up the search
///     query, and thus determine the set of results returned to the image browser list.
///     The results are stored here, and returned as a virtualised set - so we only pass
///     back (say) 200 images, and then requery for the next 200 when the user scrolls.
///     This saves us returning thousands of items for a search.
/// </summary>
public abstract class BaseSearchService
{
    protected readonly ILogger<BaseSearchService> _logger;
    protected readonly List<int> _searchResults = new();
    private readonly ICachedDataService _service;

    protected abstract Task<SearchResponse> GetQueryImagesAsync(int count = 250);

    public BaseSearchService(ICachedDataService dataService, ILogger<BaseSearchService> logger)
    {
        _service = dataService;
        _logger = logger;
    }

    public ICollection<int> SearchResults => _searchResults;

    public string SearchText
    {
        get => Query.SearchText;
        set
        {
            if ( Query.SearchText != value.Trim() )
            {
                Query.SearchText = value.Trim();
                QueryChanged();
            }
        }
    }

    public DateTime? MaxDate
    {
        get => Query.MaxDate;
        set
        {
            if ( Query.MaxDate != value )
            {
                Query.MaxDate = value;
                QueryChanged();
            }
        }
    }

    public DateTime? MinDate
    {
        get => Query.MinDate;
        set
        {
            if ( Query.MinDate != value )
            {
                Query.MinDate = value;
                QueryChanged();
            }
        }
    }

    public int? MaxSizeKB
    {
        get => Query.MaxSizeKB;
        set
        {
            if ( Query.MaxSizeKB != value )
            {
                Query.MaxSizeKB = value;
                QueryChanged();
            }
        }
    }

    public int? MinSizeKB
    {
        get => Query.MinSizeKB;
        set
        {
            if ( Query.MinSizeKB != value )
            {
                Query.MinSizeKB = value;
                QueryChanged();
            }
        }
    }

    public Folder Folder
    {
        get => Query.Folder;
        set
        {
            if ( Query.Folder != value )
            {
                Query.Folder = value;
                QueryChanged();
            }
        }
    }

    public bool TagsOnly
    {
        get => Query.TagsOnly;
        set
        {
            if ( Query.TagsOnly != value )
            {
                Query.TagsOnly = value;
                QueryChanged();
            }
        }
    }

    public bool IncludeAITags
    {
        get => Query.IncludeAITags;
        set
        {
            if ( Query.IncludeAITags != value )
            {
                Query.IncludeAITags = value;
                QueryChanged();
            }
        }
    }

    public bool UntaggedImages
    {
        get => Query.UntaggedImages;
        set
        {
            if ( Query.UntaggedImages != value )
            {
                Query.UntaggedImages = value;
                QueryChanged();
            }
        }
    }

    public int? CameraId
    {
        get => Query.CameraId;
        set
        {
            if ( Query.CameraId != value )
            {
                Query.CameraId = value;
                QueryChanged();
            }
        }
    }

    public int? LensId
    {
        get => Query.LensId;
        set
        {
            if ( Query.LensId != value )
            {
                Query.LensId = value;
                QueryChanged();
            }
        }
    }

    public int? Month
    {
        get => Query.Month;
        set
        {
            if ( Query.Month != value )
            {
                Query.Month = value;
                QueryChanged();
            }
        }
    }

    public int? MinRating
    {
        get => Query.MinRating;
        set
        {
            if ( Query.MinRating != value )
            {
                Query.MinRating = value;
                QueryChanged();
            }
        }
    }

    public Tag Tag
    {
        get => Query.Tag;
        set
        {
            if ( Query.Tag != value )
            {
                Query.Tag = value;
                QueryChanged();
            }
        }
    }

    public Image SimilarTo
    {
        get => Query.SimilarTo;
        set
        {
            if ( Query.SimilarTo != value )
            {
                Query.SimilarTo = value;
                QueryChanged();
            }
        }
    }

    public Person Person
    {
        get => Query.Person;
        set
        {
            if ( Query.Person != value )
            {
                Query.Person = value;
                QueryChanged();
            }
        }
    }

    public GroupingType Grouping
    {
        get => Query.Grouping;
        set
        {
            if ( Query.Grouping != value )
            {
                Query.Grouping = value;
                QueryChanged();
            }
        }
    }

    public SortOrderType SortOrder
    {
        get => Query.SortOrder;
        set
        {
            if ( Query.SortOrder != value )
            {
                Query.SortOrder = value;
                QueryChanged();
            }
        }
    }

    public FaceSearchType? FaceSearch
    {
        get => Query.FaceSearch;
        set
        {
            if ( Query.FaceSearch != value )
            {
                Query.FaceSearch = value;
                QueryChanged();
            }
        }
    }

    public OrientationType? Orientation
    {
        get => Query.Orientation;
        set
        {
            if ( Query.Orientation != value )
            {
                Query.Orientation = value;
                QueryChanged();
            }
        }
    }

    public SearchQuery Query { get; } = new();

    public string SearchBreadcrumbs
    {
        get
        {
            var hints = new List<string>();

            if ( !string.IsNullOrEmpty(SearchText) )
                hints.Add($"Text: {SearchText}");

            if ( Folder != null )
                hints.Add($"Folder: {Folder.Name}");

            if ( Person != null )
                hints.Add($"Person: {Person.Name}");

            if ( MinRating != null )
                hints.Add($"Rating: at least {MinRating} stars");

            if ( SimilarTo != null )
                hints.Add($"Looks Like: {SimilarTo.FileName}");

            if ( Tag != null )
                hints.Add($"Tag: {Tag.Keyword}");

            var dateRange = string.Empty;
            if ( MinDate.HasValue )
                dateRange = $"{MinDate:dd-MMM-yyyy}";

            if ( MaxDate.HasValue &&
                 (!MinDate.HasValue || MaxDate.Value.Date != MinDate.Value.Date) )
            {
                if ( !string.IsNullOrEmpty(dateRange) )
                    dateRange += " - ";
                dateRange += $"{MaxDate:dd-MMM-yyyy}";
            }

            if ( !string.IsNullOrEmpty(dateRange) )
                hints.Add($"Date: {dateRange}");

            if ( UntaggedImages )
                hints.Add("Untagged images");

            if ( FaceSearch.HasValue )
                hints.Add($"{FaceSearch.Humanize()}");

            if ( Orientation.HasValue )
                hints.Add($"{Orientation.Humanize()}");

            if ( CameraId > 0 )
            {
                var cam = _service.Cameras.FirstOrDefault(x => x.CameraId == CameraId);
                if ( cam != null )
                    hints.Add($"Camera: {cam.Model}");
            }

            if ( LensId > 0 )
            {
                var lens = _service.Lenses.FirstOrDefault(x => x.LensId == LensId);
                if ( lens != null )
                    hints.Add($"Lens: {lens.Model}");
            }

            if ( hints.Any() )
                return string.Join(", ", hints);

            return "No Filter";
        }
    }

    protected void ClearSearchResults()
    {
        _searchResults.Clear();
    }

    public void NotifyQueryChanged()
    {
        _logger.LogInformation($"ImageSearch: Filter changed: {Query}");

        OnSearchQueryChanged?.Invoke();
    }

    public void NotifySearchComplete(SearchResponse response)
    {
        _logger.LogInformation($"ImageSearch: Filter changed: {Query}");

        OnSearchResultsChanged?.Invoke(response);
    }

    public event Action OnSearchQueryChanged;
    public event Action<SearchResponse> OnSearchResultsChanged;

    public void Reset()
    {
        Query.Reset();
        QueryChanged();
    }

    public void Refresh()
    {
        QueryChanged();
    }

    public void SetDateRange(DateTime? min, DateTime? max)
    {
        if ( Query.MinDate != min || Query.MaxDate != max )
        {
            Query.MinDate = min;
            Query.MaxDate = max;
            QueryChanged();
        }
    }

    private void QueryChanged()
    {
        ClearSearchResults();
        NotifyQueryChanged();
        _ = GetQueryImagesAsync();
    }

    public async Task LoadMore( int count = 250 )
    {
        await GetQueryImagesAsync( count );
    }
}