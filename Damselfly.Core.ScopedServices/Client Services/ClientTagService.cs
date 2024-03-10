using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.ScopedServices;

public class ClientTagService : ITagService, IRecentTagService, ITagSearchService
{
    private readonly NotificationsService _notifications;
    private readonly RestClient httpClient;
    private readonly ILogger<ClientTagService> _logger;

    private ICollection<Tag>? _favouriteTags;
    private ICollection<string>? _recentTags;

    public event Action OnFavouritesChanged;
    public event Action<ICollection<string>> OnUserTagsAdded;

    public ClientTagService(RestClient client, NotificationsService notifications, ILogger<ClientTagService> logger )
    {
        _logger = logger;
        httpClient = client;
        _notifications = notifications;

        _notifications.SubscribeToNotification(NotificationType.FavouritesAndRecentsChanged, FavouritesChanged);
    }


    public async Task<ICollection<string>> GetRecentTags()
    {
        if ( _recentTags == null )
            _recentTags = await httpClient.CustomGetFromJsonAsync<List<string>>("/api/tags/recents");

        return _recentTags;
    }

    public async Task<ICollection<Tag>> SearchTags(string filterText)
    {
        return await httpClient.CustomGetFromJsonAsync<List<Tag>>($"/api/tags/search/{filterText}");
    }

    public async Task<Tag> GetTag( int tagId )
    {
        return await httpClient.CustomGetFromJsonAsync<Tag>($"/api/tag/{tagId}");
    }

    public async Task<ICollection<Tag>> GetAllTags()
    {
        return await httpClient.CustomGetFromJsonAsync<List<Tag>>("/api/tags");
    }

    public async Task<ICollection<Tag>> GetFavouriteTags()
    {
        if (_favouriteTags == null)
            _favouriteTags = await httpClient.CustomGetFromJsonAsync<List<Tag>>("/api/tags/favourites");

        return _favouriteTags;
    }

    public async Task<bool> ToggleFavourite(Tag tag)
    {
        return await httpClient.CustomPostAsJsonAsync<Tag, bool>("/api/tags/togglefave", tag);
    }

    public async Task UpdateTagsAsync(ICollection<int> imageIds, ICollection<string>? tagsToAdd,
        ICollection<string>? tagsToDelete, int? userId)
    {
        var payload = new TagUpdateRequest
        {
            ImageIDs = imageIds,
            TagsToAdd = tagsToAdd,
            TagsToDelete = tagsToDelete,
            UserId = userId
        };

        await httpClient.CustomPostAsJsonAsync("/api/tags", payload);
    }

    public async Task SetExifFieldAsync(ICollection<int> imageIds, ExifOperation.ExifType exifType, string newValue,
        int? userId = null)
    {
        var payload = new ExifUpdateRequest
        {
            ImageIDs = imageIds,
            ExifType = exifType,
            NewValue = newValue,
            UserId = userId
        };

        await httpClient.CustomPostAsJsonAsync("/api/tags/exif", payload);
    }

    private async void FavouritesChanged()
    {
        // Clear the cache
        _favouriteTags = null;
        _recentTags = null;

        await GetRecentTags();
        await GetFavouriteTags();

        OnFavouritesChanged?.Invoke();
    }

}