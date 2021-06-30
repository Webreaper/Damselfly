using System;
using System.IO;
using System.Linq;
using System.IO.Compression;
using System.Threading.Tasks;
using Damselfly.Core.Utils;
using Damselfly.Core.Models;
using System.Collections.Generic;
using Damselfly.Core.Utils.Constants;
using Damselfly.Core.Interfaces;

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
        private static DirectoryInfo themesFolder;
        private long cacheBuster = 1;
        private readonly IConfigService _configService;

        public ThemeService( IConfigService configService )
        {
            _configService = configService;
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
                return _configService.Get(ConfigSettings.Theme, "green");
            }
            set
            {
                _configService.Set(ConfigSettings.Theme, value);
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
