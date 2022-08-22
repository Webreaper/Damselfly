using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.Constants;
using Damselfly.Core.DbModels;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface ITagService
{
    event Action OnFavouritesChanged;
    Task<ICollection<Tag>> GetFavouriteTags();
    Task ToggleFavourite(Tag tag);
    Task UpdateTagsAsync(ICollection<Image> images, ICollection<string> tagsToAdd, ICollection<string> tagsToDelete, AppIdentityUser currentUser);
    Task SetExifFieldAsync(Image[] images, ExifOperation.ExifType exifType, string newValue, AppIdentityUser user = null);
}

public interface IRecentTagService
{
    Task<ICollection<string>> GetRecentTags();
}

public interface ITagSearchService
{
    Task<ICollection<string>> SearchTags(string filterText);
}
