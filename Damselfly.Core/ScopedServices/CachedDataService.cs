using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Damselfly.Core.DbModels.Models;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Services;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Damselfly.Core.ScopedServices;

public class CachedDataService(
    MetaDataService _metaDataService,
    StatisticsService _stats,
    ILogger<CachedDataService> _logger) : ICachedDataService
{
    public string ImagesRootFolder => IndexingService.RootFolder;

    public string ExifToolVer => ExifService.ExifToolVer;

    public ICollection<Camera> Cameras => _metaDataService.Cameras;

    public ICollection<Lens> Lenses => _metaDataService.Lenses;

    public Task InitialiseData()
    {
        // Nothng to do here in the Blazor Server version
        return Task.CompletedTask;
    }

    public async Task<Statistics> GetStatistics()
    {
        return await _stats.GetStatistics();
    }

    public Task ClearCache()
    {
        // No-op
        return Task.CompletedTask;
    }

    private NewVersionResponse? newVersionState;

    /// <summary>
    /// Checks for a new version
    /// </summary>
    /// <returns></returns>
    public async Task<NewVersionResponse> CheckForNewVersion()
    {
        if( newVersionState == null )
        {
            newVersionState = new NewVersionResponse
            {
                CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version
            };

            try
            {
                var client = new GitHubClient(new ProductHeaderValue("Damselfly"));

                var newRelease = await client.Repository.Release.GetLatest("webreaper", "damselfly");
                if( newRelease != null && Version.TryParse(newRelease.TagName, out var newVersion) )
                {
                    newVersionState.NewVersion = newVersion;
                    newVersionState.NewReleaseName = newRelease.Name;
                    newVersionState.ReleaseUrl = newRelease.HtmlUrl;

                    _logger.LogInformation(
                        $"A new version of Damselfly is available: ({newRelease.Name})");
                }
            }
            catch( Exception ex )
            {
                _logger.LogWarning("Unable to check GitHub for latest version: {ex}", ex);
            }
        }

        return newVersionState;
    }
}