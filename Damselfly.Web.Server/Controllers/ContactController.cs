using Damselfly.Core.DbModels.Models.API_Models;
using Damselfly.Core.Services;
using Microsoft.AspNetCore.Mvc;

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
            
            // encode input
            var htmlMessage = System.Net.WebUtility.HtmlEncode(request.Message);
            var htmlEmail = System.Net.WebUtility.HtmlEncode(request.Email);
            var fullHtml = $"<p>From: {htmlEmail}</p>" +
                $"<p>{htmlMessage}</p>";
            await _emailService.SendEmailAsync(toEmail, $"Contact Request from {request.Email}", fullHtml);

            return Ok(new BooleanResultModel { Result = true});
        }
    }
}
