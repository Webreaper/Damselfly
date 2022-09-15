using System;
using System.Reflection;
using System.Threading.Tasks;
using Damselfly.Core.Utils;
using Microsoft.JSInterop;

namespace Damselfly.Web;

public static class JsMethods
{
    public const string JSGetDesktopVersion = "getDesktopVersion";

    /// <summary>
    ///     Called from the Javascript in the Desktop App. The Electron Node code
    ///     calls the checkDesktopUpgrade in _Host.cshtml, passing in its version
    ///     string. We parse that, compare versions with ourselves (based on the
    ///     assembly version) and then return the version to which they need to
    ///     upgrade, or an empty string if the versions match and no upgrade is
    ///     necessary.
    /// </summary>
    /// <param name="desktopVersionStr"></param>
    /// <returns></returns>
    [JSInvokable]
    public static Task<string> GetUpgradeVersion(string desktopVersionStr)
    {
        var serverVer = Assembly.GetExecutingAssembly().GetName().Version;

        var upgradeVer = string.Empty;

        if ( Version.TryParse(desktopVersionStr, out var desktopVersion) )
        {
            // Electron versions are 3-octet, so ignore the revision for the version comparison.
            var threeOctetServerVer = new Version(serverVer.Major, serverVer.Minor, serverVer.Build);

            Logging.Log($"Checking server version ({threeOctetServerVer}) against desktop ver: {desktopVersion}");
            if ( threeOctetServerVer > desktopVersion ) upgradeVer = serverVer.ToString();
        }

        return Task.FromResult(upgradeVer);
    }
}