using System.Globalization;
using Damselfly.Core.Constants;
using Damselfly.Core.DbModels;
using Damselfly.Core.DbModels.Models.API_Models;
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
    protected readonly List<Guid> _searchResults = new();
    private readonly ICachedDataService _service;
    private readonly IImageCacheService _imageCache;

    protected abstract Task<SearchResponse> GetQueryImagesAsync(int count = DamselflyContants.PageSize);

    public BaseSearchService(ICachedDataService dataService, IImageCacheService imageCache, ILogger<BaseSearchService> logger)
    {
        _service = dataService;
        _imageCache = imageCache;
        _logger = logger;
    }

    public ICollection<Guid> SearchResults => _searchResults;

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

    public bool IncludeChildFolders
    {
        get => Query.IncludeChildFolders;
        set
        {
            if( Query.IncludeChildFolders != value )
            {
                Query.IncludeChildFolders = value;
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

    public Guid? CameraId
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

    public Guid? LensId
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

    public Guid? SimilarToId
    {
        get => Query.SimilarToId;
        set
        {
            if ( Query.SimilarToId != value )
            {
                Query.SimilarToId = value;
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

    public IEnumerable<ISearchHint> SearchHints
    {
        get
        {
            var hints = new List<ISearchHint>();

            if( !string.IsNullOrEmpty( SearchText) )
            {
                var text = (TagsOnly ? "Text (tags only): " : "Text: ") + SearchText;
                hints.Add( new SearchHint{ Description = text, Clear = () => SearchText = String.Empty });
            }

            if( Folder != null )
                hints.Add( new SearchHint { Description = $"Folder: {Folder.Name}", Clear = () => Folder = null });
            
            if( Person != null )
                hints.Add( new SearchHint { Description = $"Person: {Person.Name}", Clear = () => Person = null });

            if( Tag != null )
                hints.Add( new SearchHint { Description = $"Tag: {Tag.Keyword}", Clear = () => Tag = null });

            if( MinRating != null )
                hints.Add( new SearchHint { Description = $"Rating: at least {MinRating} stars", Clear = () => MinRating = null });
            
            if (SimilarToId != null)
            {
                var image = _imageCache.GetCachedImage(SimilarToId.Value).Result;
                if( image is not null )
                    hints.Add( new SearchHint { Description = $"Looks Like: {image.FileName}", Clear = () => SimilarToId = null } );
            }

            string dateString = string.Empty;
            if( MaxSizeKB.HasValue && MinSizeKB.HasValue )
            {
                dateString = $"Between {MinSizeKB.Value}KB and {MaxSizeKB.Value}KB";
            }
            else
            {
                if( MaxSizeKB.HasValue )
                    dateString = $"Less than: {MaxSizeKB.Value}KB";

                if( MinSizeKB.HasValue )
                    dateString = $"At least: {MinSizeKB.Value}KB";
            }
            
            if( ! string.IsNullOrEmpty(dateString) )
                hints.Add( new SearchHint { Description = dateString, Clear = () =>
                    {
                        MinSizeKB = null;
                        MaxSizeKB = null;
                    }
                } );

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
                hints.Add( new SearchHint {Description = $"Date: {dateRange}", Clear = () =>
                {
                    MinDate = null;
                    MaxDate = null; } 
                });

            if ( UntaggedImages )
                hints.Add( new SearchHint {Description = "Untagged Images", Clear = () => UntaggedImages = false });
            
            if( !IncludeChildFolders )
                hints.Add( new SearchHint {Description = "Exclude child folders", Clear = () => IncludeChildFolders = true });
            
            if( Month.HasValue )
            {
                string monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Month.Value);
                hints.Add( new SearchHint { Description = $"During: {monthName}", Clear = () => Month = null } );
            }

            if ( CameraId != Guid.Empty )
            {
                var cam = _service.Cameras.FirstOrDefault(x => x.CameraId == CameraId);
                if ( cam != null )
                    hints.Add( new SearchHint{ Description = $"Camera: {cam.Model}", Clear = () => CameraId = null});
            }

            if ( LensId != Guid.Empty )
            {
                var lens = _service.Lenses.FirstOrDefault(x => x.LensId == LensId);
                if ( lens != null )
                    hints.Add( new SearchHint{ Description = $"Lens: {lens.Model}", Clear = () => LensId = null});
            }
            
            if ( FaceSearch.HasValue )
                hints.Add( new SearchHint{ Description = FaceSearch.Humanize(), Clear = () => FaceSearch = null});

            if ( Orientation.HasValue )
                hints.Add( new SearchHint{ Description = Orientation.Humanize(), Clear = () => Orientation = null});

            return hints;
        }    
    }
    
    public string SearchBreadcrumbs
    {
        get
        {
            var hints = new List<string>();

            if( hints.Any() )
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
        OnSearchQueryChanged?.Invoke();
    }

    public void NotifySearchComplete(SearchResponse response)
    {
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

    public async Task LoadMore( int count = DamselflyContants.PageSize )
    {
        await GetQueryImagesAsync( count );
    }
}