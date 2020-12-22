using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Damselfly.Web
{
    public static class JsMethods
    {
        /// <summary>
        /// Called from the Javascript in the Desktop App. The Electron Node code
        /// calls the checkDesktopUpgrade in _Host.cshtml, passing in its version
        /// string. We parse that, compare versions with ourselves (based on the
        /// assembly version) and then return the version to which they need to
        /// upgrade, or an empty string if the versions match and no upgrade is
        /// necessary. 
        /// </summary>
        /// <param name="desktopVersionStr"></param>
        /// <returns></returns>
        [JSInvokable]
        public static Task<string> GetUpgradeVersion( string desktopVersionStr )
        {
            var serverVer = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

            var upgradeVer = string.Empty;

            if( Version.TryParse(desktopVersionStr, out var desktopVersion) )
            {
                // Electron versions are 3-octet, so ignore the revision for the version comparison.
                var threeOctetServerVer = new Version(serverVer.Major, serverVer.Minor, serverVer.Build);

                Logging.Log($"Checking server version ({threeOctetServerVer}) against desktop ver: {desktopVersion}");
                if (threeOctetServerVer > desktopVersion )
                {
                    upgradeVer = serverVer.ToString();
                }
            }

            return Task.FromResult( upgradeVer );
        }
    }
}
