using System;
using System.Linq;
using System.Collections.Generic;
using Damselfly.Core.Services;
using Damselfly.Core.DbModels;

namespace Damselfly.Core.ScopedServices;

public class UserTagFavouritesService : IDisposable
{
    private readonly ExifService _exifService;
    private readonly UserConfigService _configService;

    public event Action OnRecentsChanged;
    public List<string> RecentTags { get; private set; } = new List<string>();

    public UserTagFavouritesService(ExifService exifService, UserConfigService configService)
    {
        _configService = configService;
        _exifService = exifService;

        _exifService.OnUserTagsAdded += AddRecentTags;

        string recents = configService.Get("FavouriteTags");

        if( ! string.IsNullOrEmpty( recents ) )
        {
            RecentTags.AddRange(recents.Split(",").Select(x => x.Trim()).ToList());
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
    private void AddRecentTags(ICollection<string> recentTags )
    {
        const int maxRecents = 5;

        var newRecent = recentTags.Concat(RecentTags)
                                    .Except(_exifService.FavouriteTags.Select(x => x.Keyword))
                                    .Distinct()
                                    .Take(maxRecents).ToList();

        RecentTags.Clear();
        RecentTags.AddRange(newRecent);

        _configService.Set("FavouriteTags", string.Join(",",RecentTags));
        NotifyRecentsChanged();
    }

    public void Dispose()
    {
        _exifService.OnUserTagsAdded -= AddRecentTags;
    }
}

