using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Damselfly.Core.Utils.Constants;

namespace Damselfly.Core.ScopedServices
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
        private readonly UserConfigService _configService;
        public event Action<string> OnChangeTheme;
        private string _currentTheme;

        public ThemeService( UserConfigService configService )
        {
            _configService = configService;

            _currentTheme = _configService.Get(ConfigSettings.Theme, "green");
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

        public string CurrentTheme
        {
            get
            {
                return _currentTheme;
            }
            set
            {
                if(_currentTheme != value )
                {
                    _currentTheme = value;
                    _configService.Set(ConfigSettings.Theme, _currentTheme);
                    OnChangeTheme?.Invoke(Theme);
                }
            }
        }

        public string Theme
        {
            get { return _currentTheme; }
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
