using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface ITagService
{
    event Action OnFavouritesChanged;
    Task<ICollection<Tag>> GetFavouriteTags();
    Task<bool> ToggleFavourite(Tag tag);

    Task UpdateTagsAsync(ICollection<int> imageIds, ICollection<string> tagsToAdd, ICollection<string> tagsToDelete,
        int? userId = null);

    Task SetExifFieldAsync(ICollection<int> imageIds, ExifOperation.ExifType exifType, string newValue,
        int? userId = null);
}

public interface IRecentTagService
{
    Task<ICollection<string>> GetRecentTags();
}

public interface ITagSearchService
{
    Task<ICollection<Tag>> SearchTags(string filterText);
    Task<ICollection<Tag>> GetAllTags();
}