using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using Damselfly.Core.Utils;

namespace Damselfly.Core.Models.SideCars
{
    public class MetaData
    {
        public List<string> Keywords { get; set; }
    }

    public class Guid
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
                var list = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                if (list.TryGetValue("photos", out var photos))
                {
                    // Unfortunately, On1 uses the slightly crazy method of a GUID as the field identifier,
                    // which means we have to deserialise as a dictionary, and then just pick the first kvp. <sigh>
                    var guid = JsonSerializer.Deserialize<Dictionary<string, object>>(photos.ToString()).First();

                    // Now we can deserialise the actual object, and get the metadata.
                    var data = JsonSerializer.Deserialize<Guid>(guid.Value.ToString());

                    Logging.LogVerbose($"Successfully loaded on1 sidecar for {sidecarPath.FullName}");
                    result = data.metadata;
                }
            }
            catch( Exception ex )
            {
                Logging.LogWarning($"Unable to load On1 Sidecar data from {sidecarPath.FullName}: {ex.Message}");
            }
            return result;
        }
    }
}
