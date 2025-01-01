using System.Threading.Tasks;
using Damselfly.Core.Constants;
using Damselfly.Core.ScopedServices.Interfaces;

namespace Damselfly.Core.DbModels.Models;

public class SendGridSettings
{
    public string? SendGridFromAddress { get; set; }
    public string? SendGridKey { get; set; }

    public void Load(IConfigService configService)
    {
        SendGridKey = configService.Get(ConfigSettings.SendGridKey);
        SendGridFromAddress = configService.Get(ConfigSettings.SendGridFromAddress);
    }

    public async Task Save(IConfigService configService)
    {
        await configService.Set(ConfigSettings.SendGridKey, SendGridKey);
        await configService.Set(ConfigSettings.SendGridFromAddress, SendGridFromAddress);
    }
}