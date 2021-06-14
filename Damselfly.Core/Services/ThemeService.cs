using System;
using System.IO;
using System.Linq;
using System.IO.Compression;
using System.Threading.Tasks;
using Damselfly.Core.Utils;
using Damselfly.Core.Models;
using System.Collections.Generic;

namespace Damselfly.Core.Services
{
    /// <summary>
    /// Service to generate download files for exporting images from the system. Zip files
    /// are built from the basket or other selection sets, and then created on disk in the
    /// wwwroot folder. We can then pass them back to the browser as a URL to trigger a 
    /// download. The service can also perform transforms on the images before they're
    /// zipped for download, such as resizing, rotations, watermarking etc.
    /// </summary>
    public class ThemeService
    {
        public static ThemeService Instance { get; private set; }
        private static DirectoryInfo themesFolder;
        private long cacheBuster = 1;

        public ThemeService()
        {
            Instance = this;
        }

        /// <summary>
        /// Initialise the service with the download file path - which will usually
        /// be a subfolder of the wwwroot content folder.
        /// </summary>
        /// <param name="contentRootPath"></param>
        public void SetContentPath(string contentRootPath)
        {
            themesFolder = new DirectoryInfo(Path.Combine(contentRootPath, "themes"));
        }

        public string CurrentTheme {
            get
            {
                return ConfigService.Instance.Get(ConfigSettings.Theme, "green");
            }
            set
            {
                ConfigService.Instance.Set(ConfigSettings.Theme, value);
                cacheBuster++;
            }
        }

        public string ThemeCSS
        {
            get { return $"{CurrentTheme}.css?j={cacheBuster}"; }
        }

        public List<string> Themes
        {
            get
            {
                return themesFolder.GetFiles("*.css")
                                             .Select(x => Path.GetFileNameWithoutExtension(x.Name))
                                             .ToList();
            }
        }
    }
}
