using System;
using Damselfly.Core.Constants;
using Damselfly.Core.ScopedServices.Interfaces;

namespace Damselfly.Core.DbModels.Models;

public class SmtpSettings
{
    public string MailServer { get; set; }
    public int MailPort { get; set; }
    public string SenderName { get; set; }
    public string Sender { get; set; }
    public string Password { get; set; }

    public void Load(IConfigService configService)
    {
        MailServer = configService.Get(ConfigSettings.SmtpServer);
        MailPort = configService.GetInt(ConfigSettings.SmtpPort);
        Password = configService.Get(ConfigSettings.SmtpPassword);
        Sender = configService.Get(ConfigSettings.SmtpSenderEmail);
        SenderName = configService.Get(ConfigSettings.SmtpSenderName);
    }

    public void Save(IConfigService configService)
    {
        configService.Set(ConfigSettings.SmtpServer, MailServer);
        configService.Set(ConfigSettings.SmtpPort, MailPort.ToString());
        configService.Set(ConfigSettings.SmtpPassword, Password);
        configService.Set(ConfigSettings.SmtpSenderEmail, Sender);
        configService.Set(ConfigSettings.SmtpSenderName, SenderName);
    }
}

