using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Damselfly.Core.Database;
using Damselfly.Core.DbModels.Models;
using Damselfly.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.Services;

public class StatisticsService
{
    private readonly ILogger<StatisticsService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ImageContext db;

    public StatisticsService(IServiceScopeFactory scopeFactory, ILogger<StatisticsService> logger, ImageContext imageContext)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        db = imageContext;
    }

    private static string ArchString => $"{Environment.OSVersion} ({RuntimeInformation.ProcessArchitecture})";

    public async Task<Statistics> GetStatistics()
    {
        using var scope = _scopeFactory.CreateScope();

        var stats = new Statistics
        {
            OperatingSystem = ArchString,
            TotalImages = await db.Images.CountAsync(),
            TotalTags = await db.Tags.CountAsync(),
            TotalFolders = await db.Folders.CountAsync(),
            TotalImagesSizeBytes = await db.Images.SumAsync(x => (long)x.FileSizeBytes),
            PeopleFound = await db.People.CountAsync(),
            PeopleIdentified = await db.People.Where(x => x.Name != "Unknown").CountAsync(),
            ObjectsRecognised = await db.ImageObjects.CountAsync(),
            PendingAIScans = await db.ImageMetaData.Where(x => !x.AILastUpdated.HasValue).CountAsync(),
            PendingThumbs = await db.ImageMetaData.Where(x => !x.ThumbLastUpdated.HasValue).CountAsync(),
            PendingImages = await db.Images.Where(x => x.MetaData == null || x.LastUpdated > x.MetaData.LastUpdated)
                .Include(x => x.MetaData).CountAsync(),
            PendingKeywordOps = await db.KeywordOperations.Where(x => x.State == ExifOperation.FileWriteState.Pending)
                .CountAsync(),
            PendingKeywordImages = await db.KeywordOperations
                .Where(x => x.State == ExifOperation.FileWriteState.Pending)
                .Select(x => x.ImageId)
                .Distinct().CountAsync()
        };

        return stats;
    }
}