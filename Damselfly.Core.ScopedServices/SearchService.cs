using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.Models;
using Damselfly.Core.Utils;
using Microsoft.EntityFrameworkCore;
using Humanizer;
using Damselfly.Core.Constants;
using Damselfly.Core.DbModels;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Net.Http.Json;
using System.Net.Http;
using Damselfly.Core.ScopedServices.Interfaces;
using System.Text.Json;

namespace Damselfly.Core.ScopedServices;

/// <summary>
/// The search service manages the current set of parameters that make up the search
/// query, and thus determine the set of results returned to the image browser list.
/// The results are stored here, and returned as a virtualised set - so we only pass
/// back (say) 200 images, and then requery for the next 200 when the user scrolls.
/// This saves us returning thousands of items for a search.
/// </summary>
public class SearchService : BaseClientService
{
    public SearchService(HttpClient client, ICachedDataService dataService) : base(client)
    {
        _service = dataService;
    }

    private readonly ICachedDataService _service;
    private readonly SearchQuery query = new SearchQuery();
    public List<Image> SearchResults { get; private set; } = new List<Image>();

    public void NotifyStateChanged()
    {
        Logging.LogVerbose($"Filter changed: {query}");

        OnSearchChanged?.Invoke();
    }

    public event Action OnSearchChanged;

    public string SearchText { get { return query.SearchText; } set { if (query.SearchText != value.Trim() ) { query.SearchText = value.Trim(); QueryChanged(); } } }
    public DateTime? MaxDate { get { return query.MaxDate; } set { if (query.MaxDate != value) { query.MaxDate = value; QueryChanged(); } } }
    public DateTime? MinDate { get { return query.MinDate; } set { if (query.MinDate != value) { query.MinDate = value; QueryChanged(); } } }
    public int? MaxSizeKB { get { return query.MaxSizeKB; } set { if (query.MaxSizeKB != value) { query.MaxSizeKB = value; QueryChanged(); } } }
    public int? MinSizeKB { get { return query.MinSizeKB; } set { if (query.MinSizeKB != value) { query.MinSizeKB = value; QueryChanged(); } } }
    public Folder Folder { get { return query.Folder; } set { if (query.Folder != value) { query.Folder = value; QueryChanged(); } } }
    public bool TagsOnly { get { return query.TagsOnly; } set { if (query.TagsOnly != value) { query.TagsOnly = value; QueryChanged(); } } }
    public bool IncludeAITags { get { return query.IncludeAITags; } set { if (query.IncludeAITags != value) { query.IncludeAITags = value; QueryChanged(); } } }
    public bool UntaggedImages { get { return query.UntaggedImages; } set { if (query.UntaggedImages != value) { query.UntaggedImages = value; QueryChanged(); } } }
    public int? CameraId { get { return query.CameraId; } set { if (query.CameraId != value) { query.CameraId = value; QueryChanged(); } } }
    public int? LensId { get { return query.LensId; } set { if (query.LensId != value) { query.LensId = value; QueryChanged(); } } }
    public int? Month { get { return query.Month; } set { if (query.Month != value) { query.Month = value; QueryChanged(); } } }
    public int? MinRating { get { return query.MinRating; } set { if (query.MinRating != value) { query.MinRating = value; QueryChanged(); } } }
    public Tag Tag { get { return query.Tag; } set { if (query.Tag != value) { query.Tag = value; QueryChanged(); } } }
    public Image SimilarTo { get { return query.SimilarTo; } set { if (query.SimilarTo != value) { query.SimilarTo = value; QueryChanged(); } } }
    public Person Person { get { return query.Person; } set { if (query.Person != value) { query.Person = value; QueryChanged(); } } }
    public GroupingType Grouping { get { return query.Grouping; } set { if (query.Grouping != value) { query.Grouping = value; QueryChanged(); } } }
    public SortOrderType SortOrder { get { return query.SortOrder; } set { if (query.SortOrder != value) { query.SortOrder = value; QueryChanged(); } } }
    public FaceSearchType? FaceSearch { get { return query.FaceSearch; } set { if (query.FaceSearch != value) { query.FaceSearch = value; QueryChanged(); } } }
    public OrientationType? Orientation { get { return query.Orientation; } set { if (query.Orientation != value) { query.Orientation = value; QueryChanged(); } } }

    public void Reset() { ApplyQuery(new SearchQuery()); }
    public void Refresh() { QueryChanged(); }
    public SearchQuery Query {  get { return query; } }

    public void ApplyQuery(SearchQuery newQuery)
    {
        if (newQuery.CopyPropertiesTo(query))
        {
            QueryChanged();
        }
    }

    public void SetDateRange( DateTime? min, DateTime? max )
    {
        if (query.MinDate != min || query.MaxDate != max)
        {
            query.MinDate = min;
            query.MaxDate = max;
            QueryChanged();
        }
    }

    private void QueryChanged()
    {
        Task.Run(() =>
        {
            SearchResults.Clear();
            NotifyStateChanged();
        });
    }

    public string SearchBreadcrumbs
    {
        get
        {
            var hints = new List<string>();

            if (!string.IsNullOrEmpty(SearchText))
                hints.Add($"Text: {SearchText}");

            if (Folder != null)
                hints.Add($"Folder: {Folder.Name}");

            if (Person != null)
                hints.Add($"Person: {Person.Name}");

            if (MinRating != null)
                hints.Add($"Rating: at least {MinRating} stars");

            if (SimilarTo != null)
                hints.Add($"Looks Like: {SimilarTo.FileName}");

            if (Tag != null)
                hints.Add($"Tag: {Tag.Keyword}");

            string dateRange = string.Empty;
            if (MinDate.HasValue)
                dateRange = $"{MinDate:dd-MMM-yyyy}";

            if (MaxDate.HasValue &&
               (! MinDate.HasValue || MaxDate.Value.Date != MinDate.Value.Date))
            {
                if (!string.IsNullOrEmpty(dateRange))
                    dateRange += " - ";
                dateRange += $"{MaxDate:dd-MMM-yyyy}";
            }

            if (!string.IsNullOrEmpty(dateRange))
                hints.Add($"Date: {dateRange}");

            if (UntaggedImages)
                hints.Add($"Untagged images");

            if (FaceSearch.HasValue)
                hints.Add($"{FaceSearch.Humanize()}");

            if (Orientation.HasValue)
                hints.Add($"{Orientation.Humanize()}");

            if ( CameraId > 0 )
            {
                var cam = _service.Cameras.FirstOrDefault(x => x.CameraId == CameraId);
                if (cam != null)
                    hints.Add($"Camera: {cam.Model}");
            }

            if (LensId > 0)
            {
                var lens = _service.Lenses.FirstOrDefault(x => x.LensId == LensId);
                if (lens != null)
                    hints.Add($"Lens: {lens.Model}");
            }

            if( hints.Any() )
                return string.Join(", ", hints);

            return "No Filter";
        }
    }

    public async Task<SearchResponse> GetQueryImagesAsync( int start, int count )
    {
        return await httpClient.GetFromJsonAsync<SearchResponse>("/api/search");
    }
}
