using System.Threading.Tasks;
using Damselfly.Core.Constants;
using Damselfly.Core.ScopedServices.Interfaces;

namespace Damselfly.Core.DbModels.Models;

public class SmtpSettings
{
    public string? MailServer { get; set; }
    public int MailPort { get; set; }
    public string? SenderName { get; set; }
    public string? Sender { get; set; }
    public string? Password { get; set; }

    public void Load(IConfigService configService)
    {
        MailServer = configService.Get(ConfigSettings.SmtpServer);
        MailPort = configService.GetInt(ConfigSettings.SmtpPort);
        Password = configService.Get(ConfigSettings.SmtpPassword);
        Sender = configService.Get(ConfigSettings.SmtpSenderEmail);
        SenderName = configService.Get(ConfigSettings.SmtpSenderName);
    }

    public async Task Save(IConfigService configService)
    {
        await configService.Set(ConfigSettings.SmtpServer, MailServer);
        await configService.Set(ConfigSettings.SmtpPort, MailPort.ToString());
        await configService.Set(ConfigSettings.SmtpPassword, Password);
        await configService.Set(ConfigSettings.SmtpSenderEmail, Sender);
        await configService.Set(ConfigSettings.SmtpSenderName, SenderName);
    }
}