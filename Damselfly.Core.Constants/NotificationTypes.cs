using System;
namespace Damselfly.Core.Constants;

/// <summary>
/// Types of Server-to-Client notifications
/// </summary>
public enum NotificationType
{
    StatusChanged = 1,
    FoldersChanged = 2,
    WorkStatusChanged = 3,
    CacheEvict = 4,
    FavouritesChanged = 5
}