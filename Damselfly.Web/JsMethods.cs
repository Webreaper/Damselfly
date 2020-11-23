using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Damselfly.Web
{
    public static class JsMethods
    {
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
