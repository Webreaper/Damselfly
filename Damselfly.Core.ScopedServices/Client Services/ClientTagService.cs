using System;
using Damselfly.Core.DbModels;
using System.Net.Http;
using Damselfly.Core.Models;
using System.Net.Http.Json;
using Damselfly.Core.Constants;
using Damselfly.Core.ScopedServices.Interfaces;

namespace Damselfly.Core.ScopedServices;

public class ClientTagService : BaseClientService, ITagService, IRecentTagService, ITagSearchService
{
    public ClientTagService(HttpClient client) : base(client) { }

    private List<Tag> _favouriteTags;
    private List<string> _recentTags;

    // WASM: TODO:
    public event Action OnFavouritesChanged;

    public async Task<List<Tag>> GetFavouriteTags()
    {
        if (_favouriteTags == null)
        {
            _favouriteTags = await httpClient.GetFromJsonAsync<List<Tag>>("/api/tags/favourites");
        }

        return _favouriteTags;
    }

    public async Task<List<string>> GetRecentTags()
    {
        if (_recentTags == null)
        {
            _recentTags = await httpClient.GetFromJsonAsync<List<string>>("/api/tags/recents");
        }

        return _recentTags;
    }

    public async Task ToggleFavourite(Tag tag)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateTagsAsync(ICollection<Image> images, ICollection<string> tagsToAdd, ICollection<string> tagsToDelete, AppIdentityUser currentUser)
    {
        throw new NotImplementedException();
    }

    public async Task<ICollection<string>> SearchTags(string filterText)
    {
        throw new NotImplementedException();
    }

    public Task SetExifFieldAsync(Image[] images, ExifOperation.ExifType exifType, string newValue, AppIdentityUser user = null)
    {
        throw new NotImplementedException();
    }
}

