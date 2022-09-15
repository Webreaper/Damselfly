using System;
using System.Threading.Tasks;
using Damselfly.Core.DbModels.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Utils;
using Microsoft.AspNetCore.Identity.UI.Services;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Damselfly.Core.Services;

/// <summary>
///     IEmailSender implementation that uses SendGrid
/// </summary>
public class EmailSendGridService : IEmailSender
{
    private readonly SendGridSettings _options = new();

    public EmailSendGridService(IConfigService configService)
    {
        _options.Load(configService);
    }

    public bool IsValid => !string.IsNullOrEmpty(_options.SendGridFromAddress) &&
                           !string.IsNullOrEmpty(_options.SendGridKey);


    public async Task SendEmailAsync(string email, string subject, string message)
    {
        Logging.Log($"Sending email to {email} using SendGrid service.");

        try
        {
            var client = new SendGridClient(_options.SendGridKey);
            var msg = new SendGridMessage
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

            if ( response.IsSuccessStatusCode )
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