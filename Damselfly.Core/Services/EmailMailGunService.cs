using AutoMapper;
using Damselfly.Core.Database;
using Damselfly.Core.DbModels.Models.API_Models;
using Damselfly.Core.DbModels.Models.Entities;
using Damselfly.Core.DbModels.Models.Enums;
using Damselfly.Core.Models;
using Damselfly.Core.Utils;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RestSharp;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.Services
{
    public class EmailMailGunService(IConfiguration configuration, ILogger<EmailMailGunService> logger, ImageContext imageContext, IMapper mapper) : IEmailSender
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly ILogger<EmailMailGunService> _logger = logger;
        private readonly ImageContext _context = imageContext;
        private readonly IMapper _mapper = mapper;

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var emailRecord = new EmailRecord
            {
                Email = email,
                Subject = subject,
                HtmlMessage = htmlMessage,
                DateSent = null,
                Status = MessageStatusEnum.Pending
            };
            try
            {
                var response = await SendEmailToMailGunAsync(email, subject, htmlMessage);
                emailRecord.Status = response.IsSuccessful ? MessageStatusEnum.Sent : MessageStatusEnum.Failed;
                emailRecord.DateSent = DateTime.UtcNow;
            }
            catch( Exception ex )
            {
                emailRecord.Status = MessageStatusEnum.Failed;
                _logger.LogError(ex, "Error sending email");
            }
            finally
            {
                _context.EmailRecords.Add(emailRecord);
                await _context.SaveChangesAsync();
            }
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage, string objectId, MessageObjectEnum messageObjectEnum)
        {
            var emailRecord = new EmailRecord
            {
                Email = email,
                Subject = subject,
                HtmlMessage = htmlMessage,
                DateSent = null,
                Status = MessageStatusEnum.Pending,
                MessageObject = messageObjectEnum,
                MessageObjectId = objectId
            };
            try
            {
                var response = await SendEmailToMailGunAsync(email, subject, htmlMessage);
                emailRecord.Status = response.IsSuccessful ? MessageStatusEnum.Sent : MessageStatusEnum.Failed;
                emailRecord.DateSent = DateTime.UtcNow;
            }
            catch( Exception ex )
            {
                emailRecord.Status = MessageStatusEnum.Failed;
                _logger.LogError(ex, "Error sending email");
            }
            finally
            {
                _context.EmailRecords.Add(emailRecord);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<EmailRecord?> ReSendEmailAsync(Guid emailRecordId)
        {
            var emailRecord = _context.EmailRecords.Find(emailRecordId);
            if( emailRecord == null )
            {
                _logger.LogError("Email record not found for id {emailRecordId}", emailRecordId);
                return null;
            }
            try
            {
                _logger.LogInformation("Resending email with {id} to {email} with subject {subject}", emailRecord.EmailRecordId, emailRecord.Email, emailRecord.Subject);
                var response = await SendEmailToMailGunAsync(emailRecord.Email, emailRecord.Subject, emailRecord.HtmlMessage);
                emailRecord.Status = response.IsSuccessful ? MessageStatusEnum.Sent : MessageStatusEnum.Failed;
                emailRecord.DateSent = DateTime.UtcNow;
            }
            catch( Exception ex )
            {
                emailRecord.Status = MessageStatusEnum.Failed;
                _logger.LogError(ex, "Error resending email");
            }
            finally
            {
                _context.EmailRecords.Update(emailRecord);
                await _context.SaveChangesAsync();
            }
            
            return emailRecord;
        }

        public async Task<PaginationResultModel<EmailRecordModel>> GetEmailRecordsAsync(int pageNumber, int pageSize, MessageObjectEnum? objectType = null, string? objectId = null)
        {
            var query = _context.EmailRecords
                .Where(q => objectType == null || q.MessageObject == objectType)
                .Where(q => objectId == null || q.MessageObjectId == objectId)
                .OrderByDescending(e => e.DateSent);
            return await Pagination.PaginateQuery(query, pageNumber, pageSize, _mapper.Map<EmailRecordModel>);
        }

        public async Task<EmailRecordModel?> GetEmailRecordAsync(Guid emailRecordId)
        {
            var emailRecord = await _context.EmailRecords.FindAsync(emailRecordId);
            return emailRecord == null ? null : _mapper.Map<EmailRecordModel>(emailRecord);
        }

        private async Task<RestResponse> SendEmailToMailGunAsync(string email, string subject, string htmlMessage)
        {
            var options = new RestClientOptions("https://api.mailgun.net")
            {
                Timeout = new TimeSpan(0, 0, 30),
            };
            var apiKey = _configuration["MailGun:ApiKey"];
            var domain = _configuration["MailGun:Domain"];
            var fromAddress = _configuration["MailGun:FromAddress"];
            var base64ApiKey = Convert.ToBase64String(Encoding.UTF8.GetBytes($"api:{apiKey}"));
            var client = new RestClient(options);
            var request = new RestRequest($"/v3/{domain}/messages", Method.Post);
            request.AddHeader("Authorization", $"Basic {base64ApiKey}");
            request.AlwaysMultipartFormData = true;
            request.AddParameter("from", $"Honey+Thyme <{fromAddress}>");
            request.AddParameter("to", email);
            request.AddParameter("subject", subject);
            request.AddParameter("html", htmlMessage);
            var response = await client.ExecuteAsync(request);
            _logger.LogInformation("Email sent to {email} with subject {subject}, recieved response {responseContent}", email, subject, response.Content);
            return response;
        }
    }
}
