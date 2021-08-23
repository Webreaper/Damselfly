using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Threading.Tasks;
using Damselfly.Core.Utils.Constants;
using Damselfly.Core.Utils;

namespace Damselfly.Core.Services
{
    /// <summary>
    /// IEmailSender implementation that uses SendGrid
    /// </summary>
    public class EmailSendGridService : IEmailSender
    {
        public class AuthMessageSenderOptions
        {
            public string SendGridUser { get; set; }
            public string SendGridKey { get; set; }

            public void Load(ConfigService configService)
            {
                SendGridUser = configService.Get(ConfigSettings.SendGridUser);
                SendGridKey = configService.Get(ConfigSettings.SendGridKey);
            }

            public void Save(ConfigService configService)
            {
                configService.Set(ConfigSettings.SendGridUser, SendGridUser);
                configService.Set(ConfigSettings.SendGridKey, SendGridKey);
            }
        }

        public EmailSendGridService(ConfigService configService)
        {
            _options.Load(configService);
        }

        public bool IsValid
        {
            get { return !string.IsNullOrEmpty(_options.SendGridUser) && !string.IsNullOrEmpty(_options.SendGridKey); }
        }

        private readonly AuthMessageSenderOptions _options = new AuthMessageSenderOptions();


        public Task SendEmailAsync(string email, string subject, string message)
        {
            Logging.Log($"Sending email to {email} using SendGrid service.");

            return Execute(_options.SendGridKey, subject, message, email);
        }

        public Task Execute(string apiKey, string subject, string message, string email)
        {
            var client = new SendGridClient(apiKey);
            var msg = new SendGridMessage()
            {
                From = new EmailAddress("mark@otway.com", _options.SendGridUser),
                Subject = subject,
                PlainTextContent = message,
                HtmlContent = message
            };
            msg.AddTo(new EmailAddress(email));

            // Disable click tracking.
            // See https://sendgrid.com/docs/User_Guide/Settings/tracking.html
            msg.SetClickTracking(false, false);

            return client.SendEmailAsync(msg);
        }
    }

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
                MailPort = configService.GetInt(ConfigSettings.SmtpServer);
                Password = configService.Get(ConfigSettings.SmtpPassword);
                Sender = configService.Get(ConfigSettings.SmtpSenderEmail);
                SenderName = configService.Get(ConfigSettings.SmtpSenderName);
            }

            public void Save(ConfigService configService)
            {
                configService.Set(ConfigSettings.SmtpServer, MailServer);
                configService.Set(ConfigSettings.SmtpServer, MailPort.ToString());
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
                }

            }
            catch (Exception ex)
            {
                Logging.LogError($"SMTP send error: {ex}");
            }
        }

    }
}

