using System.Collections.Generic;

namespace Damselfly.Core.DbModels.Models.APIModels;

public class TagUpdateRequest
{
    public ICollection<int> ImageIDs { get; set; }
    public ICollection<string> TagsToAdd { get; set; }
    public ICollection<string> TagsToDelete { get; set; }
    public int? UserId { get; set; }
}