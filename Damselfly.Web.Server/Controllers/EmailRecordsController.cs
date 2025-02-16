using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Models.API_Models;
using Damselfly.Core.DbModels.Models.Enums;
using Damselfly.Core.Models;
using Damselfly.Core.Services;
using Damselfly.Core.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Damselfly.Web.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = PolicyDefinitions.s_FireBaseAdmin)]
    public class EmailRecordsController(EmailMailGunService emailService) : ControllerBase
    {
        private readonly EmailMailGunService _emailService = emailService;

        [HttpGet]
        [Route("{id}")]
        [ProducesResponseType(typeof(EmailRecordModel), 200)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _emailService.GetEmailRecordAsync(id);
            if( result == null )
            {
                return NotFound();
            }
            return Ok(result);
        }

        [HttpGet]
        [Route("getRecords")]
        [ProducesResponseType(typeof(PaginationResultModel<EmailRecordModel>), 200)]
        public async Task<IActionResult> GetPaginatedResults([FromQuery] int page, [FromQuery] int pageSize, [FromQuery] MessageObjectEnum? objectType, [FromQuery] string? messageObjectId)
        {
            if( page < 0 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest("Page cannot be less than 0 and pageSize must between 1 and 100");
            }
            var result = await _emailService.GetEmailRecordsAsync(page, pageSize, objectType, messageObjectId);
            if( result == null )
            {
                return NotFound();
            }
            return Ok(result);
        }

        [HttpPost]
        [Route("resend")]
        [ProducesResponseType(typeof(EmailRecordModel), 200)]
        public async Task<IActionResult> ReSendEmail(ResendEmailRequest reSendEmailRequest)
        {
            var result = await _emailService.ReSendEmailAsync(reSendEmailRequest.EmailRecordId);
            if( result == null )
            {
                return NotFound();
            }
            return Ok(result);
        }
    }

    public class ResendEmailRequest
    {
        public Guid EmailRecordId { get; set; }
    }
}
