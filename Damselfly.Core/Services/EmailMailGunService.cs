using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RestSharp;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.Services
{
    public class EmailMailGunService(IConfiguration configuration, ILogger<EmailMailGunService> logger) : IEmailSender
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly ILogger<EmailMailGunService> _logger = logger;

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var options = new RestClientOptions("https://api.mailgun.net")
            {
                MaxTimeout = 30000,
            };
            var apiKey = _configuration["MailGun:ApiKey"];
            var domain = _configuration["MailGun:Domain"];
            var fromAddress = _configuration["MailGun:FromAddress"];
            var base64ApiKey = Convert.ToBase64String(Encoding.UTF8.GetBytes($"api:{apiKey}"));
            var client = new RestClient(options);
            var request = new RestRequest($"/v3/{domain}/messages", Method.Post);
            request.AddHeader("Authorization", $"Basic {base64ApiKey}");
            request.AlwaysMultipartFormData = true;
            request.AddParameter("from", $"Damselfly <{fromAddress}>");
            request.AddParameter("to", email);
            request.AddParameter("subject", subject);
            request.AddParameter("html", htmlMessage);
            var response = await client.ExecuteAsync(request);
            _logger.LogInformation($"Email sent to {email} with subject {subject}, recieved response {response.Content}");
        }
    }
}
