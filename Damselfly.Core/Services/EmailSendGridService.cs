using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using Damselfly.Core.Utils.Constants;
using Damselfly.Core.Utils;

namespace Damselfly.Core.Services;

/// <summary>
/// IEmailSender implementation that uses SendGrid
/// </summary>
public class EmailSendGridService : IEmailSender
{
    public class SendGridSettings
    {
        public string SendGridFromAddress { get; set; }
        public string SendGridKey { get; set; }

        public void Load(ConfigService configService)
        {
            SendGridKey = configService.Get(ConfigSettings.SendGridKey);
            SendGridFromAddress = configService.Get(ConfigSettings.SendGridFromAddress);
        }

        public void Save(ConfigService configService)
        {
            configService.Set(ConfigSettings.SendGridKey, SendGridKey);
            configService.Set(ConfigSettings.SendGridFromAddress, SendGridFromAddress);
        }
    }

    public EmailSendGridService(ConfigService configService)
    {
        _options.Load(configService);
    }

    public bool IsValid
    {
        get { return !string.IsNullOrEmpty(_options.SendGridFromAddress) && !string.IsNullOrEmpty(_options.SendGridKey); }
    }

    private readonly SendGridSettings _options = new SendGridSettings();


    public async Task SendEmailAsync(string email, string subject, string message)
    {
        Logging.Log($"Sending email to {email} using SendGrid service.");

        try
        {
            var client = new SendGridClient(_options.SendGridKey);
            var msg = new SendGridMessage()
            {
                From = new EmailAddress(_options.SendGridFromAddress, "Damselfly"),
                Subject = subject,
                PlainTextContent = message,
                HtmlContent = message
            };
            msg.AddTo(new EmailAddress(email));

            // Disable click tracking.
            // See https://sendgrid.com/docs/User_Guide/Settings/tracking.html
            msg.SetClickTracking(false, false);

            var response = await client.SendEmailAsync(msg);

            if( response.IsSuccessStatusCode )
                Logging.Log($"Email send to {email} completed.");
            else
                Logging.Log($"Email send to {email} failed with status {response.StatusCode}.");
        }
        catch ( Exception ex )
        {
            Logging.LogError($"SendGrid error: {ex}");
        }
    }
}

