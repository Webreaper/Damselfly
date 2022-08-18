using System;
using Damselfly.Core.Constants;
using Damselfly.Core.DbModels;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface ITagService
{
    event Action OnFavouritesChanged;
    List<Tag> FavouriteTags { get; }

    Task ToggleFavourite(Tag tag);
    Task UpdateTagsAsync(ICollection<Image> images, ICollection<string> tagsToAdd, ICollection<string> tagsToDelete, AppIdentityUser currentUser);
    Task SetExifFieldAsync(Image[] images, ExifOperation.ExifType exifType, string newValue, AppIdentityUser user = null);
}

public interface IRecentTagService
{
    List<string> RecentTags { get; }
}

public interface ITagSearchService
{
    Task<ICollection<string>> SearchTags(string filterText);
}
