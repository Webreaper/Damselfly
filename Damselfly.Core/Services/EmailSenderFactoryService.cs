using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Damselfly.Core.Services
{
    /// <summary>
    /// The email sender factory service delegates out to an actual IEmailSender implementation
    /// based on whether one, or the other of the SendGrid and SMTP services have config.
    /// Note that this service is registered as a transient service, so a new instance will be
    /// created every time it's requested; this is to ensure that if the user changes the config
    /// they don't have to restart the entire app for the changes to take effect. 
    /// </summary>
    public class EmailSenderFactoryService : IEmailSender
    {
        private IEmailSender _senderInstance;

        public EmailSenderFactoryService( ConfigService configService )
        {
            _senderInstance = null;

            var sendGrid = new EmailSendGridService(configService);

            if (sendGrid.IsValid)
            {
                _senderInstance = sendGrid;
            }
            else
            {
                var smtp = new EmailSmtpService( configService );

                if( smtp.IsValid )
                {
                    _senderInstance = smtp;
                }
            }
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            if( _senderInstance != null )
                await _senderInstance.SendEmailAsync(email, subject, htmlMessage);
        }
    }
}
