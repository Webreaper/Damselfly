using Syncfusion.Licensing;

namespace Damselfly.Web.Client.Extensions;

public static class SyncfusionLicence
{
    public static void RegisterSyncfusionLicence()
    {
        SyncfusionLicenseProvider.RegisterLicense(
            "MjUxMDA2OEAzMjMyMmUzMDJlMzBJd1EzZDZXdElNbGJURU9OT2FxYURPenhjZDhWQWJNKzY0YzJHVmdZTjhFPQ==" );
    }
}