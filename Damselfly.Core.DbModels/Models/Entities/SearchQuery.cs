using System;
using System.Collections.Generic;
using Damselfly.Core.Constants;
using Damselfly.Core.Utils;

namespace Damselfly.Core.Models;

/// <summary>
///     A search query, with a set of parameters. By saving these to the DB, we can have 'quick
///     search' type functionality (or 'favourite' searches).
/// </summary>
public class SearchQuery
{
    public string SearchText { get; set; } = string.Empty;
    public bool TagsOnly { get; set; } = false;
    public bool IncludeAITags { get; set; } = true;
    public bool UntaggedImages { get; set; } = false;
    public bool IncludeChildFolders { get; set; } = true;
    public int? MaxSizeKB { get; set; } = null;
    public int? MinSizeKB { get; set; } = null;
    public int? CameraId { get; set; } = null;
    public int? LensId { get; set; } = null;
    public IEnumerable<int>? Months { get; set; } = null;
    public DateTime? OnThisDay { get; set; } = null;
    public int? MinRating { get; set; } = null;
    public int? SimilarToId { get; set; } = null;
    public Folder? Folder { get; set; } = null;
    public Tag? Tag { get; set; } = null;
    public Person? Person { get; set; } = null;
    public DateTime? MaxDate { get; set; } = null;
    public DateTime? MinDate { get; set; } = null;
    public FaceSearchType? FaceSearch { get; set; } = null;
    public OrientationType? Orientation { get; set; } = null;

    public GroupingType Grouping { get; set; } = GroupingType.None;
    public SortOrderType SortOrder { get; set; } = SortOrderType.Descending;

    public SearchQuery()
    {
        Reset();
    }

    public void Reset()
    {
        SearchText = string.Empty;
        TagsOnly = false;
        IncludeAITags = true;
        IncludeChildFolders = true;
        UntaggedImages = false;
        MaxSizeKB = null;
        MinSizeKB = null;
        CameraId = null;
        LensId = null;
        Months = null;
        MinRating = null;
        SimilarToId = null;
        Folder = null;
        Tag = null;
        Person = null;
        MinDate = null;
        MaxDate = null;
        FaceSearch = null;
        Orientation = null;
    }

    public override string ToString()
    {
        return
            $"Filter: T={SearchText}, F={Folder?.FolderId}, ChildFolders={IncludeChildFolders}, Tag={Tag?.TagId}, Months={Months}, Max={MaxDate}, Min={MinDate}, Max={MaxSizeKB}KB, Rating={MinRating}, Min={MinSizeKB}KB, Tags={TagsOnly}, Grouping={Grouping}, Sort={SortOrder}, Face={FaceSearch}, Person={Person?.Name}, SimilarTo={SimilarToId}";
    }
}

/// <summary>
/// A shadow object which only references the IDs of the image and other objects, rather than the whole object.
/// Eventually we can probably convert the query to use IDs instead of entities, and this will become redundant.
/// </summary>
public class SearchQueryDTO
{
    public string SearchText { get; set; } = string.Empty;
    public bool TagsOnly { get; set; } = false;
    public bool IncludeAITags { get; set; } = true;
    public bool UntaggedImages { get; set; } = false;
    public bool IncludeChildFolders { get; set; } = true;
    public int? MaxSizeKB { get; set; } = null;
    public int? MinSizeKB { get; set; } = null;
    public int? CameraId { get; set; } = null;
    public int? LensId { get; set; } = null;
    public IEnumerable<int>? Months { get; set; } = null;
    public DateTime? OnThisDay { get; set; } = null;
    public int? MinRating { get; set; } = null;
    public int? SimilarToId { get; set; }
    public int? FolderId { get; set; }
    public int? TagId { get; set; }
    public int? PersonId { get; set; }
    public DateTime? MaxDate { get; set; } = null;
    public DateTime? MinDate { get; set; } = null;
    public FaceSearchType? FaceSearch { get; set; } = null;
    public OrientationType? Orientation { get; set; } = null;

    public GroupingType Grouping { get; set; } = GroupingType.None;
    public SortOrderType SortOrder { get; set; } = SortOrderType.Descending;

    public static SearchQueryDTO CreateFrom(SearchQuery source)
    {
        var result = new SearchQueryDTO();
        source.CopyPropertiesTo(result);

        result.FolderId = source?.Folder?.FolderId;
        result.TagId = source?.Tag?.TagId;
        result.PersonId = source?.Person?.PersonId;

        return result;
    }
}