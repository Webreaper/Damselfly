using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Authentication;
using Damselfly.Core.DbModels.Models;
using Damselfly.Core.DbModels.Models.API_Models;
using Damselfly.Core.DbModels.Models.Enums;
using Damselfly.Core.Models.Exceptions;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Damselfly.Core.Models;

namespace Damselfly.Web.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PhotoShootController(PhotoShootService photoShootService, IAuthService authService) : Controller
    {
        private readonly PhotoShootService _photoShootService = photoShootService;
        private readonly IAuthService _authService = authService;

        [HttpPost]
        [Route("create")]
        [Authorize(Policy = PolicyDefinitions.s_FireBaseAdmin)]
        [ProducesResponseType(typeof(PhotoShootModel), 200)]
        public async Task<IActionResult> CreatePhotoShoot(PhotoShootModel photoShootModel)
        {
            var created = await _photoShootService.CreatePhotoShoot(photoShootModel);
            return Ok(created);
        }

        [HttpPost]
        [Route("create-many")]
        [Authorize(Policy = PolicyDefinitions.s_FireBaseAdmin)]
        [ProducesResponseType(typeof(List<PhotoShootModel>), 200)]
        public async Task<IActionResult> CreatePhotoShoots(List<PhotoShootModel> photoShootModels)
        {
            var created = await _photoShootService.CreatePhotoShoots(photoShootModels);
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

        /// <summary>
        /// Get paginated photo shoots
        /// </summary>
        /// <param name="request">Pagination request containing PageIndex, PageSize, and filtering options</param>
        /// <returns>Paginated result containing photo shoots and pagination metadata</returns>
        [HttpPost]
        [Route("paginated")]
        [Authorize(Policy = PolicyDefinitions.s_FireBaseAdmin)]
        [ProducesResponseType(typeof(PaginationResultModel<PhotoShootModel>), 200)]
        public async Task<ActionResult<PaginationResultModel<PhotoShootModel>>> GetPaginatedPhotoShoots(PhotoShootFilerRequest request)
        {
            if (request.PageIndex < 0 || request.PageSize < 1 || request.PageSize > 100)
            {
                return BadRequest("PageIndex cannot be less than 0 and PageSize must be between 1 and 100");
            }

            var result = await _photoShootService.GetPhotoShootsPaginated(request);
            return Ok(result);
        }

        [HttpGet]
        [Route("{id}")]
        [ProducesResponseType(typeof(PhotoShootModel), 200)]
        public async Task<ActionResult<PaginationResultModel<PhotoShootModel>>> GetPhotoShootById(string id)
        {
            if( !Guid.TryParse(id, out var photoShootId) )
                return BadRequest("id is required");
            var result = await _photoShootService.GetPhotoShootById(photoShootId);
            if( result == null )
                return NotFound();
            return Ok(result);
        }

        [HttpGet]
        [Route("by-reservation-code/{reservationCode}")]
        [ProducesResponseType(typeof(PhotoShootModel), 200)]
        public async Task<IActionResult> GetPhotoShootByReservationCode(string reservationCode)
        {
            if (string.IsNullOrWhiteSpace(reservationCode))
                return BadRequest("Reservation code is required");
            var result = await _photoShootService.GetPhotoShootByReservationCode(reservationCode);
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

        [HttpGet]
        [Route("upcoming-appointments")]
        [ProducesResponseType(typeof(List<PhotoShootModel>), 200)]
        public async Task<IActionResult> GetUpcomingAppointments(DateTime? startDateUtc, DateTime? endDateUtc)
        {
            if (!startDateUtc.HasValue || startDateUtc < DateTime.UtcNow)
                startDateUtc = DateTime.UtcNow;
            if (!endDateUtc.HasValue || endDateUtc.Value > DateTime.UtcNow.AddDays(90))
                endDateUtc = startDateUtc.Value.AddDays(90);
            
            var filterRequest = new PhotoShootFilerRequest
            {
                StartDate = startDateUtc,
                EndDate = endDateUtc,
                PhotoShootType = PhotoShootTypeEnum.CalendarBooking,
                PageIndex = 0,
                PageSize = 1000 // Large page size to get all upcoming appointments
            };
            
            var result = await _photoShootService.GetPhotoShootsPaginated(filterRequest);
            
            // strip sensitive data
            foreach( var shoot in result.Results )
            {
                shoot.PaymentRemaining = null;
                shoot.ResponsiblePartyEmailAddress = null;
                shoot.ResponsiblePartyName = null;
                shoot.ReservationCode = null;
            }
            return Ok(result.Results);
        }

        [HttpPost]
        [Route("schedule-appointment")]
        [ProducesResponseType(typeof(BookAppointmentResponse), 200)]
        public async Task<ActionResult<BookAppointmentResponse>> ScheduleAppointment(ScheduleAppointmentRequest bookRequest)
        {
            try
            {
                var result = await _photoShootService.SchedulePhotoShoot(bookRequest);
                var response = new BookAppointmentResponse { PhotoShoot = result };
                return Ok(response);
            }
            catch (NotFoundException)
            {
                return Ok(
                    new BookAppointmentResponse
                    {
                        Error =
                            new ErrorResponse
                            {
                                Code = "APPOINTMENT_NOT_FOUND",
                                Details = "This appointment no longer exists",
                                Message = "Please try another time."
                            }
                    });
            }
            catch (AlreadyScheduledException)
            {
                return Ok(
                    new BookAppointmentResponse
                    {
                        Error =
                            new ErrorResponse
                            {
                                Code = "APPOINTMENT_ALREADY_BOOKED",
                                Details = "This appointment has already been booked by another user.",
                                Message = "Please try another time."
                            }
                    });
            }
            catch (Exception ex)
            {
                return Ok(
                    new BookAppointmentResponse
                    {
                        Error =
                            new ErrorResponse
                            {
                                Code = "UNKNOWN_ERROR",
                                Details = ex.Message,
                                Message = "An unknown error occurred while booking the appointment."
                            }
                    });
            }
        }
    }
}
