using AutoMapper;
using Damselfly.Core.Database;
using Damselfly.Core.DbModels.Authentication;
using Damselfly.Core.DbModels.Models;
using Damselfly.Core.DbModels.Models.API_Models;
using Damselfly.Core.DbModels.Models.Entities;
using Damselfly.Core.DbModels.Models.Enums;
using Damselfly.Core.Interfaces;
using Damselfly.Core.Models;
using Damselfly.Core.Models.Exceptions;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Utils;
using Damselfly.PaymentProcessing;
using Damselfly.PaymentProcessing.Models;
using Damselfly.Shared.Utils;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Damselfly.Core.Services
{
    public class PhotoShootService(ILogger<PhotoShootService> logger,
        ImageContext imageContext,
        IMapper mapper,
        IAuthService authService,
        PaymentService paymentService,
        EmailMailGunService emailService,
        IConfiguration configuration,
        ICacheService cacheService,
        IIpOriginService ipOriginService,
        IHubContext<BookingHub> bookingHubContext,
        GoogleCalendarService googleCalendarService)
    {
        private readonly ILogger<PhotoShootService> _logger = logger;
        private readonly ImageContext _imageContext = imageContext;
        private readonly IMapper _mapper = mapper;
        private readonly IAuthService _authService = authService;
        private readonly PaymentService _paymentService = paymentService;
        private readonly EmailMailGunService _emailService = emailService;
        private readonly IConfiguration _configuration = configuration;
        private readonly ICacheService _cacheService = cacheService;
        private readonly IIpOriginService _ipOriginService = ipOriginService;
        private readonly IHubContext<BookingHub> _bookingHubContext = bookingHubContext;
        private readonly GoogleCalendarService _googleCalendarService = googleCalendarService;

        public async Task<IEnumerable<PhotoShootModel>> CreatePhotoShoots(IEnumerable<PhotoShootModel> photoShoots)
        {
            var currentUserEmail = await _authService.GetCurrentUserEmail();
            if( currentUserEmail == null )
            {
                _logger.LogError("Unable to get current user email to CreatePhotoShoots");
                return null;
            }
            _logger.LogInformation("Creating Photo Shoots by request of {userEmail}", currentUserEmail);
            var dbPhotoShoots = new List<PhotoShoot>();
            foreach( var photoShoot in photoShoots )
            {
                var dbPhotoShoot = _mapper.Map<PhotoShoot>(photoShoot);
                dbPhotoShoots.Add(dbPhotoShoot);
                _imageContext.PhotoShoots.Add(dbPhotoShoot);
                if (dbPhotoShoot.ResponsiblePartyEmailAddress != null)
                {
                    dbPhotoShoot.ReservationCode = GenerateScheduleCode();
                    await NotifyResponsbilePartyOfScheduling(photoShoot);
                }
            }
            await _imageContext.SaveChangesAsync();
            return [.. dbPhotoShoots.Select(_mapper.Map<PhotoShootModel>)];
        }

        public async Task<PhotoShootModel> CreatePhotoShoot(PhotoShootModel photoShoot)
        {
            var currentUserEmail = await _authService.GetCurrentUserEmail();
            if (currentUserEmail == null)
            {
                _logger.LogError("Unable to get current user email to CreatePhotoShoot");
                return null;
            }
            photoShoot.ReservationCode ??= GenerateScheduleCode();
            _logger.LogInformation("Creating Photo Shoot for {responsibleParty} scheduled for {dateOfPhotoShoot} with {photoShootId} by request of {userEmail}", 
                photoShoot.ResponsiblePartyName, photoShoot.DateTimeUtc, photoShoot.PhotoShootId, currentUserEmail);
            var dbPhotoShoot = _mapper.Map<PhotoShoot>(photoShoot);
            _imageContext.PhotoShoots.Add(dbPhotoShoot);
            await _imageContext.SaveChangesAsync();
            
            if (photoShoot.ResponsiblePartyEmailAddress != null)
            {
                await NotifyResponsbilePartyOfScheduling(photoShoot);
            }

            return _mapper.Map<PhotoShootModel>(dbPhotoShoot);
        }

        public async Task<PhotoShootModel?> SchedulePhotoShoot(ScheduleAppointmentRequest scheduleAppointmentRequest)
        {
            _logger.LogInformation("Scheduled Photo Shoot {photoShootId} by request of {userEmail}", scheduleAppointmentRequest.PhotoShootId, scheduleAppointmentRequest.Email);
            var photoShoot = await _imageContext.PhotoShoots.FindAsync(scheduleAppointmentRequest.PhotoShootId);
            if( photoShoot == null )
            {
                _logger.LogError("Unable to find Photo Shoot {photoShootId} to book", scheduleAppointmentRequest.PhotoShootId);
                throw new NotFoundException();
            }
            if (photoShoot.Status != PhotoShootStatusEnum.Unbooked)
            {
                _logger.LogInformation("Photo Shoot {photoShootId} is already scheduled", scheduleAppointmentRequest.PhotoShootId);
                throw new AlreadyScheduledException();
            }
            photoShoot.ResponsiblePartyName = scheduleAppointmentRequest.Name;
            photoShoot.ResponsiblePartyEmailAddress = scheduleAppointmentRequest.Email;
            photoShoot.Status = PhotoShootStatusEnum.Scheduled;
            photoShoot.RequestExpirationDateTime = DateTime.UtcNow.AddMinutes(30); // 30 minutes to pay deposit
            photoShoot.ReservationCode = GenerateScheduleCode();
            _imageContext.PhotoShoots.Update(photoShoot);
            await _imageContext.SaveChangesAsync();
            await NotifyResponsbilePartyOfScheduling(_mapper.Map<PhotoShootModel>(photoShoot));
            await _bookingHubContext.Clients.All.SendAsync("PhotoShootScheduled", photoShoot.PhotoShootId.ToString());
            return _mapper.Map<PhotoShootModel>(photoShoot);
        }

        private static string GenerateScheduleCode()
        {
            // Generate a random 6 character alphanumeric code
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public async Task<PhotoShootModel> UpdatePhotoShoot(PhotoShootModel photoShoot)
        {
            var currentUserEmail = await _authService.GetCurrentUserEmail();
            if( currentUserEmail == null )
            {
                _logger.LogError("Unable to get current user email to UpdatePhotoShoot {photoShootId}", photoShoot.PhotoShootId);
                return null;
            }
            _logger.LogInformation("Updating Photo Shoot {photoShootId} by request of {userEmail}", photoShoot.PhotoShootId, currentUserEmail);

            var dbPhotoShoot = await _imageContext.PhotoShoots.Include(p => p.PaymentTransactions).FirstAsync(p => p.PhotoShootId == photoShoot.PhotoShootId);
            

            // extra logic associated with an unbooking, so we need to just short circuit
            if (dbPhotoShoot.Status != photoShoot.Status && photoShoot.Status == PhotoShootStatusEnum.Unbooked)
            {
                return await PhotoShootUnscheduled(dbPhotoShoot);
            }

            if (dbPhotoShoot.Status != photoShoot.Status && dbPhotoShoot.Status == PhotoShootStatusEnum.Unbooked)
            {
                await _bookingHubContext.Clients.All.SendAsync("PhotoShootScheduled", photoShoot.PhotoShootId.ToString());
            }

            if (dbPhotoShoot.Status != PhotoShootStatusEnum.Delivered && photoShoot.Status == PhotoShootStatusEnum.Delivered )
            {
                await PhotoShootDelivered(photoShoot);
            }

            if (string.IsNullOrWhiteSpace(dbPhotoShoot.ResponsiblePartyName) && !string.IsNullOrWhiteSpace(photoShoot.ResponsiblePartyName) 
                && !string.IsNullOrWhiteSpace(photoShoot.ResponsiblePartyEmailAddress))
            {
                dbPhotoShoot.ReservationCode ??= GenerateScheduleCode();
                await NotifyResponsbilePartyOfScheduling(photoShoot);
            }

            if (dbPhotoShoot.DateTimeUtc != photoShoot.DateTimeUtc || dbPhotoShoot.EndDateTimeUtc != photoShoot.EndDateTimeUtc)
            {
                await UpdateCalendarEvent(dbPhotoShoot, await GetPhotographer());
            }

            dbPhotoShoot = _mapper.Map<PhotoShoot>(photoShoot);
            _imageContext.PhotoShoots.Update(dbPhotoShoot);
            await _imageContext.SaveChangesAsync();
            return _mapper.Map<PhotoShootModel>(dbPhotoShoot);
        }

        private async Task<PhotoShootModel> PhotoShootUnscheduled(PhotoShoot dbPhotoShoot)
        {
            _logger.LogInformation("Photo Shoot {photoShootId} is being marked as unbooked", dbPhotoShoot.PhotoShootId);
            await NotifiyResponsbilePartyOfCancelation(dbPhotoShoot);
            await DeleteCalendarEvent(dbPhotoShoot);
            foreach( var transaction in dbPhotoShoot.PaymentTransactions )
            {
                transaction.IsCancelled = true;
                _imageContext.PaymentTransactions.Update(transaction);
            }

            dbPhotoShoot.RequestExpirationDateTime = null;
            dbPhotoShoot.ReservationCode = null;
            dbPhotoShoot.ExternalCalendarId = null;
            dbPhotoShoot.Status = PhotoShootStatusEnum.Unbooked;
            dbPhotoShoot.ResponsiblePartyEmailAddress = null;
            dbPhotoShoot.ResponsiblePartyName = null;
            dbPhotoShoot.ReminderSent = false;
            _imageContext.PhotoShoots.Update(dbPhotoShoot);
            await _imageContext.SaveChangesAsync();
            await _bookingHubContext.Clients.All.SendAsync("PhotoShootUnscheduled", dbPhotoShoot.PhotoShootId.ToString());
            // refetch the photoshoot 
            return await GetPhotoShootById(dbPhotoShoot.PhotoShootId);
        }

        private async Task PhotoShootDelivered(PhotoShootModel photoShoot)
        {
            if( photoShoot.ResponsiblePartyEmailAddress == null) return;

            var album = await _imageContext.Albums.FindAsync(photoShoot.AlbumId);
            if( album == null )
            {
                _logger.LogError("Unable to find album for photo shoot {photoShootId} when sending delivery email", photoShoot.PhotoShootId);
                return;
            }

            var baseUrl = _configuration["DamselflyConfiguration:AllowedOrigins"].Split(",")[0];
            var emailBody = EmailContent.FormatEmail("Photos Delivered",
                [
                    "Your photos are ready!",
                        $"Thank you again for letting me take your photos! Your link and pin are below.",
                        "I highly recommend downloading rather than taking a screenshot as downloading will give you a higher quality and sharpness. There are options on download size - with higher quality it can take multiple minutes to prepare your download.",
                        "If there are any changes you would like made, please feel free to reach out and I am happy to adjust where I can. When posting online, I'd love for you to tag me (unless you've made any editing changes yourself as then it is no longer fully my work)! This helps my business grow and I always love seeing which photos you choose to share!",
                        "You can tag me on Instagram at @honeyandthymephotography or on Facebook at @Honey+Thyme Photography",
                        "Enjoy!",
                        "",
                        $"Password: {album.Password}"
                ],
                $"{baseUrl}/#/albums/{album.UrlName}", "View Photos"
            );
            await _emailService.SendEmailAsync(photoShoot.ResponsiblePartyEmailAddress, "Photos Ready", emailBody, photoShoot.PhotoShootId.ToString(), MessageObjectEnum.PhotoShoot);
        }

        private async Task NotifiyResponsbilePartyOfCancelation(PhotoShoot photoShoot)
        {
            if( photoShoot.ResponsiblePartyEmailAddress == null ) return;

            var questionsEmail = _configuration["ContactForm:ToAddress"];
            var emailBody = EmailContent.FormatEmail("Appointment cancelation",
                ["Hello!", $"Your photoshoot appointment for {GetLocalTimeStringFromUtc(photoShoot.DateTimeUtc)} has been cancelled. If you have any questions please feel to email me at {questionsEmail}"]);
            await _emailService.SendEmailAsync(photoShoot.ResponsiblePartyEmailAddress, "Cancelation Confirmed", emailBody, photoShoot.PhotoShootId.ToString(), MessageObjectEnum.PhotoShoot);
        }

        private async Task NotifyResponsbilePartyOfScheduling(PhotoShootModel photoShoot)
        {
            if( photoShoot.ResponsiblePartyEmailAddress != null )
            {
                var baseUrl = _configuration["DamselflyConfiguration:AllowedOrigins"].Split(",")[0];
                var emailBody = EmailContent.FormatEmail("Appointment Created",
                    ["Hello!", $"Your photoshoot for {GetLocalTimeStringFromUtc(photoShoot.DateTimeUtc)} has been scheduled but is not confirmed until deposit is paid. You can do this with the button below."],
                    $"{baseUrl}/#/invoice?id={photoShoot.ReservationCode}", "Pay Deposit");
                await _emailService.SendEmailAsync(photoShoot.ResponsiblePartyEmailAddress, "Photo shoot scheduled with Honey + Thyme", emailBody, photoShoot.PhotoShootId.ToString(), MessageObjectEnum.PhotoShoot);
            }
        }

        private async Task DeleteCalendarEvent(PhotoShoot photoShoot)
        {
            if( photoShoot.ExternalCalendarId == null ) return;

            _logger.LogInformation("Deleting calendar event for photo shoot {photoShootId} with externalCalendarId {externalCalendarId}",
                photoShoot.PhotoShootId, photoShoot.ExternalCalendarId);

            var photographer = await GetPhotographer();
            await _googleCalendarService.DeleteCalendarEventAsync(photographer.Id, photoShoot.ExternalCalendarId, photographer.PreferredCalendarId);
        }

        public async Task<bool> DeletePhotoShoot(Guid id)
        {
            var currentUserEmail = await _authService.GetCurrentUserEmail();
            if( currentUserEmail == null )
            {
                _logger.LogError("Unable to get current user email to DeletePhotoShoot {photoShootId}", id);
                return false;
            }
            _logger.LogInformation("Deleting Photo Shoot {photoShootId} by request of {userEmail}", id, currentUserEmail);
            var photoShoot = await _imageContext.PhotoShoots.FindAsync(id);
            if( photoShoot == null ) return false;
            photoShoot.Status = PhotoShootStatusEnum.Deleted;
            _imageContext.PhotoShoots.Update(photoShoot);
            await _imageContext.SaveChangesAsync();


            await DeleteCalendarEvent(photoShoot);

            await NotifiyResponsbilePartyOfCancelation(photoShoot);

            return true;
        }

        public async Task<PhotoShootModel> GetPhotoShootById(Guid id)
        {
            var dbPhotoShoot = await _imageContext.PhotoShoots
                .Include(x => x.PaymentTransactions.Where(t => !t.IsCancelled))
                .FirstOrDefaultAsync(x => x.PhotoShootId == id);
            if ( dbPhotoShoot == null ) return null; 
            return MapPhotoShoot(dbPhotoShoot, _mapper);
        }

        private IOrderedQueryable<PhotoShoot> BuildPhotoShootQuery(PhotoShootFilerRequest filter)
        {
            var query = _imageContext.PhotoShoots.Where(x => 1 == 1);

            if (filter?.StartDate.HasValue == true)
                query = query.Where(x => x.DateTimeUtc >= filter.StartDate.Value.ToUniversalTime());

            if (filter?.EndDate.HasValue == true)
                query = query.Where(x => x.DateTimeUtc < filter.EndDate.Value.ToUniversalTime());

            if (filter?.Statuses != null)
                query = query.Where(x => filter.Statuses.Contains(x.Status));
            else
                query = query.Where(x => x.Status != PhotoShootStatusEnum.Deleted);

            if( filter?.PhotoShootType.HasValue == true )
                query = query.Where(x => x.PhotoShootType == filter.PhotoShootType.Value);

            return query.Include(x => x.PaymentTransactions
                .Where(t => !t.IsCancelled))
                .OrderBy(x => x.DateTimeUtc);
        }

        /// <summary>
        /// Get paginated photo shoots using the existing pagination utility
        /// </summary>
        /// <param name="request">Pagination request containing PageIndex, PageSize, and filtering options</param>
        /// <returns>Paginated result containing photo shoots and pagination metadata</returns>
        public async Task<PaginationResultModel<PhotoShootModel>> GetPhotoShootsPaginated(PhotoShootFilerRequest request)
        {
            var currentUserEmail = await _authService.GetCurrentUserEmail();
            _logger.LogInformation("Getting paginated Photo Shoots for {userEmail}", currentUserEmail);
            var query = BuildPhotoShootQuery(request);
            return await Pagination.PaginateQuery(query, request.PageIndex, request.PageSize, x => MapPhotoShoot(x, _mapper));
        }

        public async Task<CreatePhotoShootPaymentResponse> MakePaymentForPhotoShoot(CreatePhotoShootPaymentRequest photoShootPayment)
        {
            if( !await ValidateNotFraud() )
            {
                return new CreatePhotoShootPaymentResponse
                {
                    IsSuccess = false,
                    ReservationCode = photoShootPayment.ReservationCode,
                    ProcessorEnum = photoShootPayment.PaymentProcessorEnum,
                };
            }

            _logger.LogInformation("Creating payment with amount {amount} for photoshoot with reservationCode {reservationCode}", photoShootPayment.Amount, photoShootPayment.ReservationCode);
            var photoShoot = await _imageContext.PhotoShoots.FirstOrDefaultAsync(p => p.ReservationCode == photoShootPayment.ReservationCode);
            if( photoShoot == null )
            {
                return new CreatePhotoShootPaymentResponse
                {
                    IsSuccess = false,
                    ReservationCode = photoShootPayment.ReservationCode,
                    ProcessorEnum = photoShootPayment.PaymentProcessorEnum,
                };
            }

            var invoiceId = Guid.NewGuid();

            var orderRequest = new CreateOrderRequest
            {
                Amount = photoShootPayment.Amount,
                Description = photoShootPayment.Description,
                InvoiceId = invoiceId.ToString(),
                PaymentProcessorEnum = photoShootPayment.PaymentProcessorEnum,
                ShortDescription = "HoneyThymePhotos",
                
            };
            var order = await _paymentService.CreateOrder(orderRequest);

            var result = new CreatePhotoShootPaymentResponse
            {
                IsSuccess = order.IsSuccess,
                ReservationCode = photoShootPayment.ReservationCode,
                ProcessorEnum = photoShootPayment.PaymentProcessorEnum,
                ProcessorOrderId = order.OrderId,
                InvoiceId = invoiceId,
            };
            _logger.LogInformation("Creating payment for photoshoot {photoshootId} IsSuccess {isSuccess}", photoShoot.PhotoShootId, result.IsSuccess);
            return result;
        }

        public async Task<bool> ValidateNotFraud()
        {
            var userIp = await _authService.GetCurrentUserIp();
            if( userIp == null )
            {
                return false;
            }
            _logger.LogInformation("Doing fraud check for {ip}", userIp);
            var cacheKey = $"FraudCheck-{userIp}";
            var cacheValue = await _cacheService.GetAsync(cacheKey);
            FraudCheckModel fraudCheckModel;
            if (cacheValue != null)
            {
                var cachedResult = JsonConvert.DeserializeObject<FraudCheckModel>(cacheValue);
                fraudCheckModel = cachedResult;
            }
            else
            {
                var countryOfOrigin = await _ipOriginService.GetIpOrigin(userIp);
                fraudCheckModel = new FraudCheckModel
                {
                    Country = countryOfOrigin,
                    IpAddress = userIp,
                    AttemptCount = 0,
                };
            }

            fraudCheckModel.AttemptCount++;
            await _cacheService.SetAsync(cacheKey, JsonConvert.SerializeObject(fraudCheckModel), TimeSpan.FromMinutes(60));

            if( fraudCheckModel.Country != "US" )
            {
                _logger.LogWarning("Country of origin is not US for {ip}", userIp);
                return false;
            }

            if( fraudCheckModel.AttemptCount > 5 )
            {
                _logger.LogWarning("IP address has attempted to make payment to many times for {ip}", userIp);
                return false;
            }
            _logger.LogInformation("No issues detected for {ip}", userIp);
            return true;
        }

        public async Task ResetUnpaidShoots()
        {
            _logger.LogInformation("Resetting unpaid photo shoots");
            var localTimeNow = GetLocalDateTimeFromUtc(DateTime.UtcNow);
            var startOfDay = localTimeNow.Date;
            var endOfDay = startOfDay.AddDays(1);
            var photoShoots = await _imageContext.PhotoShoots
                .Where(x => x.RequestExpirationDateTime < DateTime.UtcNow)
                .Where(x => x.Status == PhotoShootStatusEnum.Scheduled)
                .ToListAsync();
            foreach( var photoShoot in photoShoots )
            {
                photoShoot.Status = PhotoShootStatusEnum.Unbooked;
                photoShoot.RequestExpirationDateTime = null;
                photoShoot.ReservationCode = null;
                _imageContext.PhotoShoots.Update(photoShoot);
                await _bookingHubContext.Clients.All.SendAsync("PhotoShootUnscheduled", photoShoot.PhotoShootId.ToString());
            }
            await _imageContext.SaveChangesAsync();
        }

        public async Task<PhotoShootPaymentCaptureResponse> CapturePaymentForPhotoShoot(PhotoShootPaymentCaptureRequest photoShootCaptureRequest)
        {
            try
            {
                _logger.LogInformation("Capturing payment for photoshoot with reservationCode {reservationCode} and externalOrderId {externalOrderId}", photoShootCaptureRequest.ReservationCode, photoShootCaptureRequest.ExternalOrderId);
                var photoShoot = await _imageContext.PhotoShoots.FirstOrDefaultAsync(p => p.ReservationCode == photoShootCaptureRequest.ReservationCode);

                if(photoShoot == null)
                {
                    _logger.LogWarning("No photoshoot found with reservationCode {reservationCode}", photoShootCaptureRequest.ReservationCode);
                    return new PhotoShootPaymentCaptureResponse
                    {
                        IsSuccess = false,
                        ReservationCode = photoShootCaptureRequest.ReservationCode,
                        ShouldTryAgain = false
                    };
                }

                if (photoShoot.Status == PhotoShootStatusEnum.Scheduled)
                {
                    photoShoot.Status = PhotoShootStatusEnum.Booked;
                    _imageContext.PhotoShoots.Update(photoShoot);
                    await _imageContext.SaveChangesAsync();
                }

                var captureRequest = new CaptureOrderRequest
                {
                    InvoiceId = photoShootCaptureRequest.InvoiceId.ToString(),
                    PaymentProcessorOrderId = photoShootCaptureRequest.ExternalOrderId,
                    PaymentProcessor = photoShootCaptureRequest.PaymentProcessor,
                    Amount = photoShootCaptureRequest.AmountToBeCharged,
                };

                var capture = await _paymentService.CaptureOrder(captureRequest);

                if( capture.ErrorDuringCharge )
                {
                    _logger.LogWarning("There was an unrecoverable problem capturing payment for photoshoot {phtoshootId} with externalOrderId {externalOrderId}",
                        photoShoot.PhotoShootId, photoShootCaptureRequest.ExternalOrderId);
                    return new PhotoShootPaymentCaptureResponse
                    {
                        IsSuccess = false,
                        ShouldTryAgain = false,
                        ReservationCode = photoShootCaptureRequest.ReservationCode
                    };
                }

                if( !capture.WasSuccessful )
                {
                    _logger.LogWarning("There was a problem capturing payment for photoshoot {phtoshootId} with externalOrderId {externalOrderId}, but the user should try again",
                        photoShoot.PhotoShootId, photoShootCaptureRequest.ExternalOrderId);
                    return new PhotoShootPaymentCaptureResponse
                    {
                        IsSuccess = false,
                        ShouldTryAgain = true,
                        ReservationCode = photoShootCaptureRequest.ReservationCode
                    };
                }

                _logger.LogInformation("Capturing payment for photoshoot {phtoshootId} with externalOrderId {externalOrderId} was succesful",
                        photoShoot.PhotoShootId, photoShootCaptureRequest.ExternalOrderId);
                // record transaction
                var paymentTransaction = new PaymentTransaction
                {
                    Amount = capture.PaymentTotal,
                    DateTimeUtc = DateTime.UtcNow,
                    Description = capture.Description,
                    PaymentTransactionId = photoShootCaptureRequest.InvoiceId,
                    PaymentProcessorType = photoShootCaptureRequest.PaymentProcessor,
                    PhotoShootId = photoShoot.PhotoShootId,
                    ExternalId = capture.ExternalOrderId,
                };

                _imageContext.PaymentTransactions.Add(paymentTransaction);
                await _imageContext.SaveChangesAsync();

                var totalPaid = await _imageContext.PaymentTransactions
                    .Where(x => x.PhotoShootId == photoShoot.PhotoShootId)
                    .Where(t => !t.IsCancelled)
                    .SumAsync(x => x.Amount);
                var photographer = await GetPhotographer();

                if (photoShoot.Deposit <= totalPaid)
                {
                    if (photoShoot.Status != PhotoShootStatusEnum.Confirmed && photoShoot.ExternalCalendarId == null)
                    {
                        await CreateCalendarEvent(photoShoot, photographer);
                    }
                    photoShoot.Status = PhotoShootStatusEnum.Confirmed;
                    _imageContext.PhotoShoots.Update(photoShoot);
                    await _imageContext.SaveChangesAsync();
                }

                _logger.LogInformation("Payment for photoshoot {phtoshootId} with externalOrderId {externalOrderId} was saved to the database, sending applicable emails",
                        photoShoot.PhotoShootId, photoShootCaptureRequest.ExternalOrderId);
                var paidInFull = totalPaid >= photoShoot.Price;
                // email recipet
                if( photoShoot.ResponsiblePartyEmailAddress != null )
                {
                    
                    var remainingBalance = photoShoot.Price - totalPaid;
                    if( remainingBalance < 0 ) remainingBalance = 0;
                    string[][] recieptInfo =
                    [
                        ["Shoot name", photoShoot.NameOfShoot],
                        ["Deposit", $"${photoShoot.Deposit}"],
                        ["Total price", $"${photoShoot.Price}"],
                        ["Paid", $"${totalPaid}"],
                        ["Balance Due", $"${remainingBalance}"]
                    ];
                    var emailBody = EmailContent.FormatEmail("Appintment confirmation",
                        [$"Congrats! Your photoshoot is booked! Any remaining balance will be due the day of your shoot."], tableElements: recieptInfo);
                    if( paidInFull )
                    {
                        emailBody = EmailContent.FormatEmail("Thank you for your payment",
                            [$"Thank you for your payment!"], tableElements: recieptInfo);
                    }
                    await _emailService.SendEmailAsync(photoShoot.ResponsiblePartyEmailAddress, "Honey+Thyme Reciept", emailBody, photoShoot.PhotoShootId.ToString(), MessageObjectEnum.PhotoShoot);
                }

                var primaryMessage = "";
                if( paidInFull ) primaryMessage = $"A payment of ${capture.PaymentTotal} has been recieved for the photo shoot appointment for {photoShoot.ResponsiblePartyName} {photoShoot.NameOfShoot} and is now marked as paid in full.";
                else primaryMessage = $"You got a booking a payment of ${capture.PaymentTotal} has been recieved for the photo shoot appointment for {photoShoot.ResponsiblePartyName} {photoShoot.NameOfShoot}";
                var adminEmailBody = EmailContent.FormatEmail("Payment Recieved",
                    ["Hello!", primaryMessage]);
                await _emailService.SendEmailAsync(photographer.Email, $"Payment Recieved For {photoShoot.ResponsiblePartyName} {photoShoot.NameOfShoot}", adminEmailBody, photoShoot.PhotoShootId.ToString(), MessageObjectEnum.PhotoShoot);

                return new PhotoShootPaymentCaptureResponse
                {
                    IsSuccess = true,
                    ShouldTryAgain = false,
                    ReservationCode = photoShootCaptureRequest.ReservationCode
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unrecoverable error trying to process payment for photoShoot with reservationCode {reservationCode}", photoShootCaptureRequest.ReservationCode);
                return new PhotoShootPaymentCaptureResponse
                {
                    IsSuccess = false,
                    ShouldTryAgain = false,
                    ReservationCode = photoShootCaptureRequest.ReservationCode
                };
            }

        }

        internal async Task<AppIdentityUser> GetPhotographer()
        {
            var adminEmail = _configuration["ContactForm:ToAddress"];
            return await _imageContext.Users.FirstAsync(u => u.NormalizedEmail == adminEmail.ToUpper());
        }

        internal async Task CreateCalendarEvent(PhotoShoot photoShoot, AppIdentityUser photographer)
        {
            var calendarEvent = new CreateCalendarEventRequest
            {
                Summary = $"{photoShoot.NameOfShoot} - {photoShoot.ResponsiblePartyEmailAddress}",
                Description = photoShoot.Description ?? "",
                StartTime = photoShoot.DateTimeUtc,
                EndTime = photoShoot.EndDateTimeUtc ?? photoShoot.DateTimeUtc.AddHours(1), // Default to 1 hour if no end time
                Attendees = [photoShoot.ResponsiblePartyEmailAddress],
                CalendarId = photographer.PreferredCalendarId
            };
            var calendarResult = await _googleCalendarService.CreateCalendarEventAsync(photographer.Id, calendarEvent);
            photoShoot.ExternalCalendarId = calendarResult?.Id;
        }

        internal async Task UpdateCalendarEvent(PhotoShoot photoShoot, AppIdentityUser photographer)
        {
            var calendarEvent = new CreateCalendarEventRequest
            {
                Summary = $"{photoShoot.NameOfShoot} - {photoShoot.ResponsiblePartyEmailAddress}",
                Description = photoShoot.Description ?? "",
                StartTime = photoShoot.DateTimeUtc,
                EndTime = photoShoot.EndDateTimeUtc ?? photoShoot.DateTimeUtc.AddHours(1), // Default to 1 hour if no end time
                Attendees = [photoShoot.ResponsiblePartyEmailAddress],
                CalendarId = photographer.PreferredCalendarId
            };
            var _ = await _googleCalendarService.UpdateCalendarEventAsync(photographer.Id, photoShoot.ExternalCalendarId, calendarEvent, photographer.PreferredCalendarId);

        }

        public async Task SendReminderEmails()
        {
            var localTimeNow = GetLocalDateTimeFromUtc(DateTime.UtcNow);
            var startOfDay = localTimeNow.Date;
            var endOfDay = startOfDay.AddDays(1);
            var photoShoots = await _imageContext.PhotoShoots
                .Where(x => x.DateTimeUtc >= startOfDay.ToUniversalTime() && x.DateTimeUtc < endOfDay.ToUniversalTime())
                .Where(x => !x.ReminderSent)
                .Where(x => x.Status == PhotoShootStatusEnum.Confirmed || x.Status == PhotoShootStatusEnum.Paid)
                .Where(x => x.PaymentTransactions.Where(t => !t.IsCancelled).Sum(x => x.Amount) < x.Price)
                .Include(x => x.PaymentTransactions.Where(t => !t.IsCancelled))
                .ToListAsync();
            foreach( var photoShoot in photoShoots )
            {
                if (string.IsNullOrWhiteSpace( photoShoot.ResponsiblePartyEmailAddress)) continue;

                var baseUrl = _configuration["DamselflyConfiguration:AllowedOrigins"].Split(",")[0];
                var totalPaid = photoShoot.PaymentTransactions.Sum(x => x.Amount);
                var remainingBalance = photoShoot.Price - totalPaid;
                var emailBody = EmailContent.FormatEmail("Payment Reminder",
                    [$"Hey there! This is just a friendly reminder that your remaining balance of ${remainingBalance} is due today after your shoot. Thanks again for your business!"],
                    $"{baseUrl}/#/invoice?id={photoShoot.ReservationCode}", "Pay Balance");
                await _emailService.SendEmailAsync(photoShoot.ResponsiblePartyEmailAddress, "Photoshoot reminder", emailBody, photoShoot.PhotoShootId.ToString(), MessageObjectEnum.PhotoShoot);
                photoShoot.ReminderSent = true;
                _imageContext.PhotoShoots.Update(photoShoot);
                await _imageContext.SaveChangesAsync();
            }
        }

        private PhotoShootModel MapPhotoShoot(PhotoShoot photoShoot, IMapper mapper)
        {
            if (photoShoot == null) return null;
            var ret = mapper.Map<PhotoShootModel>(photoShoot);
            var totalPaid = photoShoot.PaymentTransactions.Sum(x => x.Amount);
            ret.PaymentRemaining = photoShoot.Price - totalPaid - photoShoot.Discount;
            return ret;
        }

        private DateTime GetLocalDateTimeFromUtc(DateTime dateTime)
        {
            var localTimeZoneName = _configuration["DamselflyConfiguration:Timezone"];
            var localTimeZone = TimeZoneInfo.FindSystemTimeZoneById(localTimeZoneName);
            return TimeZoneInfo.ConvertTimeFromUtc(dateTime, localTimeZone);
        }

        private string GetLocalTimeStringFromUtc(DateTime dateTime)
        {
            return GetLocalDateTimeFromUtc(dateTime).ToString("MM/dd/yyyy hh:mm tt");
        }

        public async Task<PhotoShootModel> GetPhotoShootByReservationCode(string reservationCode)
        {
            _logger.LogInformation("Getting Photo Shoot by reservation code {reservationCode}", reservationCode);
            var photoShoot = await _imageContext.PhotoShoots
                .Include(x => x.PaymentTransactions.Where(t => !t.IsCancelled))
                .FirstOrDefaultAsync(x => x.ReservationCode == reservationCode);
            return MapPhotoShoot(photoShoot, _mapper);
        }
    }
}
