using System;
using System.Linq;
using System.Collections.Generic;
using Damselfly.Core.Services;
using Damselfly.Core.DbModels;
using Damselfly.Core.ScopedServices.Interfaces;
using System.Threading.Tasks;
using Damselfly.Core.Interfaces;

namespace Damselfly.Core.ScopedServices;

public class UserTagRecentsService : IRecentTagService, IDisposable
{
    private readonly ExifService _exifService;
    private readonly IConfigService _configService;
    private readonly List<string> recentTags = new List<string>();

    public event Action OnRecentsChanged;

    public async Task<ICollection<string>> GetRecentTags()
    {
        return recentTags;
    }

    public UserTagRecentsService(ExifService exifService, IConfigService configService)
    {
        _configService = configService;
        _exifService = exifService;

        _exifService.OnUserTagsAdded += AddRecentTags;

        string recents = configService.Get("RecentTags");

        if( ! string.IsNullOrEmpty( recents ) )
        {
            recentTags.AddRange(recents.Split(",").Select(x => x.Trim()).ToList());
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
    private async void AddRecentTags(ICollection<string> newRecents)
    {
        const int maxRecents = 5;

        var faves = await _exifService.GetFavouriteTags();

        var newRecent = recentTags.Concat(newRecents)
                                    .Except(faves.Select(x => x.Keyword))
                                    .Distinct()
                                    .Take(maxRecents).ToList();
        recentTags.Clear();
        recentTags.AddRange(newRecent);

        _configService.Set("RecentTags", string.Join(",", recentTags));
        NotifyRecentsChanged();
    }

    public void Dispose()
    {
        _exifService.OnUserTagsAdded -= AddRecentTags;
    }
}

