using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Damselfly.Core.Constants;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Services;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.ScopedServices;

public class UserTagRecentsService : IRecentTagService, IDisposable
{
    private readonly IConfigService _configService;
    private readonly ExifService _exifService;
    private readonly List<string> recentTags = new();
    private readonly ILogger<UserTagRecentsService> _logger;

    public UserTagRecentsService(ExifService exifService, IConfigService configService, ILogger<UserTagRecentsService> logger)
    {
        _configService = configService;
        _exifService = exifService;
        _logger = logger;

        _exifService.OnUserTagsAdded += AddRecentTags;

        var recents = configService.Get(ConfigSettings.RecentTags);

        if (!string.IsNullOrEmpty(recents))
        {
            recentTags.AddRange(recents.Split(",")
                                       .Select(x => x.Trim())
                                       .ToList());
        }
    }

    public void Dispose()
    {
        _exifService.OnUserTagsAdded -= AddRecentTags;
    }

    public Task<ICollection<string>> GetRecentTags()
    {
        ICollection<string> result = recentTags;
        return Task.FromResult(result);
    }

    public event Action OnRecentsChanged;

    private void NotifyRecentsChanged()
    {
        OnRecentsChanged?.Invoke();
    }

    /// <summary>
    ///     Add most-recent tags to the list
    /// </summary>
    /// <param name="recentTags"></param>
    public async void AddRecentTags( ICollection<string> newRecents)
    {
        try
        {
            if( newRecents == null || !newRecents.Any() )
                return;

            const int maxRecents = 5;

            var faves = await _exifService.GetFavouriteTags();
            var recents = await GetRecentTags();

            if( recents != null && recents.Any() )
            {
                var newRecent = newRecents.Concat( recentTags )
                    .Except( faves.Select( x => x.Keyword ) )
                    .Distinct()
                    .Take( maxRecents ).ToList();
                recentTags.Clear();
                recentTags.AddRange( newRecent );

                _configService.Set( ConfigSettings.RecentTags, string.Join( ",", recentTags ) );
                NotifyRecentsChanged();
            }
        }
        catch( Exception ex )
        {
            _logger.LogError( $"Unable to add items to recent tags list: {ex.Message}." );
        }
    }
}