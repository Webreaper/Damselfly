using System;
using System.Linq;
using System.Collections.Generic;
using Damselfly.Core.Services;
using Damselfly.Core.DbModels;
using Damselfly.Core.ScopedServices.Interfaces;
using System.Threading.Tasks;

namespace Damselfly.Core.ScopedServices;

public class UserTagFavouritesService : IRecentTagService, IDisposable
{
    private readonly ExifService _exifService;
    private readonly UserConfigService _configService;
    private readonly List<string> faveTags = new List<string>();

    public event Action OnRecentsChanged;

    public async Task<List<string>> GetRecentTags()
    {
        return faveTags;
    }

    public UserTagFavouritesService(ExifService exifService, UserConfigService configService)
    {
        _configService = configService;
        _exifService = exifService;

        _exifService.OnUserTagsAdded += AddRecentTags;

        string recents = configService.Get("FavouriteTags");

        if( ! string.IsNullOrEmpty( recents ) )
        {
            faveTags.AddRange(recents.Split(",").Select(x => x.Trim()).ToList());
        }
    }

    private void NotifyRecentsChanged()
    {
        OnRecentsChanged?.Invoke();
    }

    /// <summary>
    /// Add most-recent tags to the list
    /// </summary>
    /// <param name="recentTags"></param>
    private async void AddRecentTags(ICollection<string> recentTags)
    {
        // WASM: Sort out this mess
        Task.Run(() => { AddRecentTagsAsync(recentTags); });
    }

    private async Task AddRecentTagsAsync(ICollection<string> recentTags)
    {
        const int maxRecents = 5;

        var faves = await _exifService.GetFavouriteTags();

        var newRecent = recentTags.Concat(faveTags)
                                    .Except(faves.Select(x => x.Keyword))
                                    .Distinct()
                                    .Take(maxRecents).ToList();

        faveTags.Clear();
        faveTags.AddRange(newRecent);

        _configService.Set("FavouriteTags", string.Join(",", faveTags));
        NotifyRecentsChanged();
    }

    public void Dispose()
    {
        _exifService.OnUserTagsAdded -= AddRecentTags;
    }
}

