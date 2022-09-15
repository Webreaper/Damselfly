using Damselfly.Core.Constants;
using Damselfly.Core.ScopedServices.Interfaces;

namespace Damselfly.Core.DbModels.Models;

public class SendGridSettings
{
    public string SendGridFromAddress { get; set; }
    public string SendGridKey { get; set; }

    public void Load(IConfigService configService)
    {
        SendGridKey = configService.Get(ConfigSettings.SendGridKey);
        SendGridFromAddress = configService.Get(ConfigSettings.SendGridFromAddress);
    }

    public void Save(IConfigService configService)
    {
        configService.Set(ConfigSettings.SendGridKey, SendGridKey);
        configService.Set(ConfigSettings.SendGridFromAddress, SendGridFromAddress);
    }
}