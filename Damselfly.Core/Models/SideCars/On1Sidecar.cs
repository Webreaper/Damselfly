using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using Damselfly.Core.Utils;

namespace Damselfly.Core.Models.SideCars;

public class MetaData
{
    public List<string> Keywords { get; set; }
    public int Rating { get; set; }
}

public class Photo
{
    public bool guid_locked { get; set; }
    public MetaData metadata { get; set; }
}

/// <summary>
/// Class to represent, and read, the On1 Sidecar data, which is json
/// serialised in a .on1 file.
/// </summary>
public class On1Sidecar
{
    public Dictionary<string, Photo> photos { get; set; } = new Dictionary<string, Photo>();

    /// <summary>
    /// Load the on1 sidecar metadata for the image - if it exists.
    /// </summary>
    /// <param name="image"></param>
    /// <returns>Metadata, with keywords etc</returns>
    public static MetaData LoadMetadata(FileInfo sidecarPath)
    {
        MetaData result = null;

        try
        {
            string json = File.ReadAllText( sidecarPath.FullName );

            // Deserialize.
            var sideCar = JsonSerializer.Deserialize<On1Sidecar>(json);

            if( sideCar != null )
            { 
                Logging.LogVerbose($"Successfully loaded on1 sidecar for {sidecarPath.FullName}");
                var photo = sideCar.photos.Values.FirstOrDefault();

                if( photo != null )
                    result = photo.metadata;
            }
        }
        catch( Exception ex )
        {
            Logging.LogWarning($"Unable to load On1 Sidecar data from {sidecarPath.FullName}: {ex.Message}");
        }
        return result;
    }
}
