using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity.UI.Services;
using MimeKit;
using System;
using System.Threading.Tasks;
using Damselfly.Core.Utils.Constants;
using Damselfly.Core.Utils;

namespace Damselfly.Core.Services;

/// <summary>
/// IEmailSender implementation that uses SMTP
/// </summary>
public class EmailSmtpService : IEmailSender
{
    public class SmtpSettings
    {
        public string MailServer { get; set; }
        public int MailPort { get; set; }
        public string SenderName { get; set; }
        public string Sender { get; set; }
        public string Password { get; set; }

        public void Load( ConfigService configService )
        {
            MailServer = configService.Get(ConfigSettings.SmtpServer);
            MailPort = configService.GetInt(ConfigSettings.SmtpPort);
            Password = configService.Get(ConfigSettings.SmtpPassword);
            Sender = configService.Get(ConfigSettings.SmtpSenderEmail);
            SenderName = configService.Get(ConfigSettings.SmtpSenderName);
        }

        public void Save(ConfigService configService)
        {
            configService.Set(ConfigSettings.SmtpServer, MailServer);
            configService.Set(ConfigSettings.SmtpPort, MailPort.ToString());
            configService.Set(ConfigSettings.SmtpPassword, Password);
            configService.Set(ConfigSettings.SmtpSenderEmail, Sender);
            configService.Set(ConfigSettings.SmtpSenderName, SenderName);
        }
    }

    public bool IsValid
    {
        get { return !string.IsNullOrEmpty(_emailSettings.MailServer) && !string.IsNullOrEmpty(_emailSettings.Password); }
    }

    private readonly SmtpSettings _emailSettings = new SmtpSettings();

    public EmailSmtpService( ConfigService configService )
    {
        _emailSettings.Load(configService);
    }

    public async Task SendEmailAsync(string email, string subject, string message)
    {
        Logging.Log($"Sending email to {email} using SMTP service.");

        try
        {
            var mimeMessage = new MimeMessage();

            mimeMessage.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.Sender));

            mimeMessage.To.Add(new MailboxAddress(_emailSettings.SenderName, email));

            mimeMessage.Subject = subject;

            mimeMessage.Body = new TextPart("html")
            {
                Text = message
            };

            using (var client = new SmtpClient())
            {
                // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                if (System.Diagnostics.Debugger.IsAttached)
                {
                    // The third parameter is useSSL (true if the client should make an SSL-wrapped
                    // connection to the server; otherwise, false).
                    await client.ConnectAsync(_emailSettings.MailServer, _emailSettings.MailPort, true);
                }
                else
                {
                    await client.ConnectAsync(_emailSettings.MailServer);
                }

                // Note: only needed if the SMTP server requires authentication
                await client.AuthenticateAsync(_emailSettings.Sender, _emailSettings.Password);

                await client.SendAsync(mimeMessage);

                await client.DisconnectAsync(true);

                Logging.Log($"Email send to {email} complete.");
            }

        }
        catch (Exception ex)
        {
            Logging.LogError($"SMTP send error: {ex}");
        }
    }

}

