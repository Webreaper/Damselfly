using System;
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
    public int? MaxSizeKB { get; set; } = null;
    public int? MinSizeKB { get; set; } = null;
    public int? CameraId { get; set; } = null;
    public int? LensId { get; set; } = null;
    public int? Month { get; set; } = null;
    public int? MinRating { get; set; } = null;
    public Image SimilarTo { get; set; } = null;
    public Folder Folder { get; set; } = null;
    public Tag Tag { get; set; } = null;
    public Person Person { get; set; } = null;
    public DateTime? MaxDate { get; set; } = null;
    public DateTime? MinDate { get; set; } = null;
    public FaceSearchType? FaceSearch { get; set; } = null;
    public OrientationType? Orientation { get; set; } = null;

    public GroupingType Grouping { get; set; } = GroupingType.None;
    public SortOrderType SortOrder { get; set; } = SortOrderType.Descending;

    public override string ToString()
    {
        return
            $"Filter: T={SearchText}, F={Folder?.FolderId}, Tag={Tag?.TagId}, Max={MaxDate}, Min={MinDate}, Max={MaxSizeKB}KB, Rating={MinRating}, Min={MinSizeKB}KB, Tags={TagsOnly}, Grouping={Grouping}, Sort={SortOrder}, Face={FaceSearch}, Person={Person?.Name}, SimilarTo={SimilarTo?.ImageId}";
    }
}

public class SearchQueryDTO
{
    public string SearchText { get; set; } = string.Empty;
    public bool TagsOnly { get; set; } = false;
    public bool IncludeAITags { get; set; } = true;
    public bool UntaggedImages { get; set; } = false;
    public int? MaxSizeKB { get; set; } = null;
    public int? MinSizeKB { get; set; } = null;
    public int? CameraId { get; set; } = null;
    public int? LensId { get; set; } = null;
    public int? Month { get; set; } = null;
    public int? MinRating { get; set; } = null;
    public int? SimilarToImageId { get; set; }
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

        result.SimilarToImageId = source?.SimilarTo?.ImageId;
        result.FolderId = source?.Folder?.FolderId;
        result.TagId = source?.Tag?.TagId;
        result.PersonId = source?.Person?.PersonId;

        return result;
    }
}