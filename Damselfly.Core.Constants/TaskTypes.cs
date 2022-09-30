using System;
namespace Damselfly.Core.Constants;

public enum TaskType
{
    FullIndex,
    IncrementalIndex,
    FullThumbnailGen,
    IncrementalThumbnailGen,
    CleanupDownloads,
    CleanupKeywordOps,
    ThumbnailCleanup,
    MetadataScan,
    FlushDBWriteCache,
    DumpPerformance,
    CleanupThumbs,
    FreeTextIndex
}