using System;

namespace Damselfly.Core.DbModels.Models.APIModels;

public class NewVersionResponse
{
    public Version CurrentVersion { get; set; }
    public Version? NewVersion { get; set; }
    public string? NewReleaseName { get; set; }
    public string? ReleaseUrl { get; set; }
    public bool? UpgradeAvailable()
    {
        if( NewVersion != null )
            return NewVersion > CurrentVersion;

        return null;
    }

}