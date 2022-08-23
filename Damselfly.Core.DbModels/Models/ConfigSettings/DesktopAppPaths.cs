using System;
namespace Damselfly.Core.DbModels.Models;

public class DesktopAppPaths
{
    public string MacOSApp { get; set; }
    public string MacOSArmApp { get; set; }
    public string WindowsApp { get; set; }
    public string LinuxApp { get; set; }

    public bool AppsAvailable
    {
        get
        {
            return MacOSApp != null ||
                   MacOSArmApp != null ||
                   WindowsApp != null ||
                   LinuxApp != null;
        }
    }
}

