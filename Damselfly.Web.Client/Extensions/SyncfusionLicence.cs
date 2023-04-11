using Syncfusion.Licensing;

namespace Damselfly.Web.Client.Extensions;

public static class SyncfusionLicence
{
    public static void RegisterSyncfusionLicence()
    {
        SyncfusionLicenseProvider.RegisterLicense(
            "MTY5MDQ5NUAzMjMxMmUzMTJlMzMzOUJBZk85WWJSZXRXK1NudHJWUExMeGk3Y0lUeFNRUXFUQm9ReGxtMmhYOXc9" );
    }
}