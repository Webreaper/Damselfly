using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Authentication;
using Damselfly.Core.DbModels.Models;
using Damselfly.Core.DbModels.Models.API_Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Damselfly.Web.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PhotoShootController(PhotoShootService photoShootService, IAuthService authService) : Controller
    {
        private readonly PhotoShootService _photoShootService = photoShootService;
        private readonly IAuthService _authService =authService;

        [HttpPost]
        [Route("create")]
        [Authorize(Policy = PolicyDefinitions.s_FireBaseAdmin)]
        [ProducesResponseType(typeof(PhotoShootModel), 200)]
        public async Task<IActionResult> CreatePhotoShoot(PhotoShootModel photoShootModel)
        {
            var created = await _photoShootService.CreatePhotoShoot(photoShootModel);
            return Ok(created);
        }

        [HttpDelete]
        [Route("{id}")]
        [Authorize(Policy = PolicyDefinitions.s_FireBaseAdmin)]
        public async Task<IActionResult> DeletePhotoShoot(string id)
        {
            if( !Guid.TryParse(id, out var photoShootId) )
                return BadRequest("id is required");
            var success = await _photoShootService.DeletePhotoShoot(photoShootId);
            if (!success)
            {
                return BadRequest();
            }

            return Ok(new BooleanResultModel { Result = true });
        }

        [HttpPost]
        [Route("update")]
        [Authorize(Policy = PolicyDefinitions.s_FireBaseAdmin)]
        [ProducesResponseType(typeof(PhotoShootModel), 200)]
        public async Task<IActionResult> UpdatePhotoShoot(PhotoShootModel photoShootModel)
        {
            var result = await _photoShootService.UpdatePhotoShoot(photoShootModel);
            return Ok(result);
        }

        [HttpPost]
        [Route("list")]
        [Authorize(Policy = PolicyDefinitions.s_FireBaseAdmin)]
        [ProducesResponseType(typeof(List<PhotoShootModel>), 200)]
        public async Task<IActionResult> GetPhotoShoots(PhotoShootFilerRequest? photoShootFilerRequest)
        {
            var result = await _photoShootService.GetPhotoShoots(photoShootFilerRequest);
            return Ok(result);
        }


        [HttpGet]
        [Route("{id}")]
        [ProducesResponseType(typeof(PhotoShootModel), 200)]
        public async Task<IActionResult> GetPhotoShootById(string id)
        {
            if( !Guid.TryParse(id, out var photoShootId) )
                return BadRequest("id is required");
            var result = await _photoShootService.GetPhotoShootById(photoShootId);
            if( result == null )
                return NotFound();
            return Ok(result);
        }

        [HttpPost]
        [Route("createPayment")]
        [ProducesResponseType(typeof(CreatePhotoShootPaymentResponse), 200)]
        public async Task<IActionResult> CreatePaymentForShoot(CreatePhotoShootPaymentRequest request)
        {
            if( request.Amount < 1 ) return BadRequest("Cannot charge less than a dollar.");
            var result = await _photoShootService.MakePaymentForPhotoShoot(request);
            return Ok(result);
        }

        [HttpPost]
        [Route("capturePayment")]
        [ProducesResponseType(typeof(PhotoShootPaymentCaptureResponse), 200)]
        public async Task<IActionResult> CapturePaymentForShoot(PhotoShootPaymentCaptureRequest request)
        {
            if (request == null )
                return BadRequest();
            if( request.PaymentProcessor == Core.DbModels.Models.Enums.PaymentProcessorEnum.External
                && !await _authService.CheckCurrentFirebaseUserIsInRole([RoleDefinitions.s_AdminRole]) )
                return Unauthorized();
            var result = await _photoShootService.CapturePaymentForPhotoShoot(request);
            return Ok(result);
        }
    }
}
