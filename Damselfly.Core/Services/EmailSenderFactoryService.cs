using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Damselfly.Core.Services
{
    public class EmailSenderFactoryService : IEmailSender
    {
        private IEmailSender _senderInstance;

        public EmailSenderFactoryService( ConfigService configService )
        {
            // Construct the right type, based on config

            _senderInstance = null;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            if( _senderInstance != null )
                return _senderInstance.SendEmailAsync(email, subject, htmlMessage);

            // Nothing configured, so just return that we're complete.
            return Task.CompletedTask;
        }
    }
}
