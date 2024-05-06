using Damselfly.Core.DbModels.Models.API_Models;
using Damselfly.Core.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Damselfly.Web.Server.Controllers
{
    [ApiController]
    public class ContactController(
        EmailMailGunService emailService, 
        ILogger<ContactController> logger,
        IConfiguration configuration
        ) : ControllerBase
    {

        private readonly EmailMailGunService _emailService = emailService;
        private readonly ILogger<ContactController> _logger = logger;
        private readonly IConfiguration _configuration = configuration;

        [HttpPost]
        [Route("api/contact")]
        [ProducesResponseType(typeof(BooleanResultModel), 200)]
        public async Task<IActionResult> Contact(ContactRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var toEmail = _configuration["ContactForm:ToAddress"];
            _logger.LogInformation($"Contact request recieved from {request.Email} with message {request.Message}");
            

            var htmlMessage = System.Net.WebUtility.HtmlEncode(request.Message);
            var htmlEmail = System.Net.WebUtility.HtmlEncode(request.Email);
            var fullHtml = $"<p>From: {htmlEmail}</p>" +
                $"<p>{htmlMessage}</p>";
            await _emailService.SendEmailAsync(toEmail, $"Contact Request from {request.Email}", fullHtml);

            return Ok(new BooleanResultModel { Result = true});
        }

        [HttpPost]
        [Route("api/booking")]
        [ProducesResponseType(typeof(BooleanResultModel), 200)]
        public async Task<IActionResult> Booking(BookingRequest request)
        {
            if( !ModelState.IsValid )
            {
                return BadRequest(ModelState);
            }
            var toEmail = _configuration["ContactForm:ToAddress"];
            _logger.LogInformation($"Booking request recieved from {request.Email}");

            var message = new StringBuilder();
            message.Append("<ul>");
            message.Append(GetHtmlLineItem("Name", request.Name));
            message.Append(GetHtmlLineItem("Email", request.Email));
            message.Append(GetHtmlLineItem("Number of Guests", request.NumberOfPeople.ToString()));
            message.Append(GetHtmlLineItem("Session Length", request.SessionLength));
            message.Append(GetHtmlLineItem("Occasion", request.Occasion));
            message.Append(GetHtmlLineItem("Location Preferences", request.Location));
            message.Append(GetHtmlLineItem("Other Info", request.Questions));
            message.Append("</ul>");
            await _emailService.SendEmailAsync(toEmail, $"Booking Request from {request.Email}", message.ToString());

            return Ok(new BooleanResultModel { Result = true });
        }

        private static string GetHtmlLineItem(string label, string value)
        {
            return $"<li><strong>{label}</strong>: {System.Net.WebUtility.HtmlEncode(value)}</li>";
        }
    }
}
