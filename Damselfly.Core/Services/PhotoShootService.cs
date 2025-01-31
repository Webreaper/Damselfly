using AutoMapper;
using Damselfly.Core.Database;
using Damselfly.Core.DbModels.Models;
using Damselfly.Core.DbModels.Models.API_Models;
using Damselfly.Core.DbModels.Models.Entities;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Utils;
using Damselfly.PaymentProcessing;
using Damselfly.PaymentProcessing.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        IConfiguration configuration)
    {
        private readonly ILogger<PhotoShootService> _logger = logger;
        private readonly ImageContext _imageContext = imageContext;
        private readonly IMapper _mapper = mapper;
        private readonly IAuthService _authService = authService;
        private readonly PaymentService _paymentService = paymentService;
        private readonly EmailMailGunService _emailService = emailService;
        private readonly IConfiguration _configuration = configuration;

        public async Task<PhotoShootModel> CreatePhotoShoot(PhotoShootModel photoShoot)
        {
            var currentUserEmail = await _authService.GetCurrentUserEmail();
            if (currentUserEmail == null)
            {
                _logger.LogError("Unable to get current user email to CreatePhotoShoot");
                return null;
            }
            _logger.LogInformation("Creating Photo Shoot for {responsibleParty} scheduled for {dateOfPhotoShoot} with {photoShootId} by request of {userEmail}", 
                photoShoot.ResponsiblePartyName, photoShoot.DateTimeUtc, photoShoot.PhotoShootId, currentUserEmail);
            var dbPhotoShoot = _mapper.Map<PhotoShoot>(photoShoot);
            _imageContext.PhotoShoots.Add(dbPhotoShoot);
            await _imageContext.SaveChangesAsync();
                
            if (photoShoot.ResponsiblePartyEmailAddress != null)
            {
                var emailBody = EmailContent.FormatEmail("Appointment Created",
                    ["Hello!", $"Your photo shoot appointment for {GetLocalTimeStringFromUtc(photoShoot.DateTimeUtc)} has been created. Your appointment will not be considered confirmed until your deposit is paid."],
                    "https://honeyandthymephotography.com", "Pay Deposit");
                await _emailService.SendEmailAsync(photoShoot.ResponsiblePartyEmailAddress, "Photo shoot confirmed with Honey + Thyme", emailBody);
            }

            return _mapper.Map<PhotoShootModel>(dbPhotoShoot);
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

            var dbPhotoShoot = _mapper.Map<PhotoShoot>(photoShoot);
            _imageContext.PhotoShoots.Update(dbPhotoShoot);
            await _imageContext.SaveChangesAsync();
            return _mapper.Map<PhotoShootModel>(dbPhotoShoot);
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
            photoShoot.IsDeleted = true;
            _imageContext.PhotoShoots.Update(photoShoot);
            await _imageContext.SaveChangesAsync();
            
            if (photoShoot.ResponsiblePartyEmailAddress != null )
            {
                var emailBody = EmailContent.FormatEmail("Appointment cancelation",
                    ["Hello!", $"Your photo shoot appointment for {GetLocalTimeStringFromUtc(photoShoot.DateTimeUtc)} has been cancelled."]);
                await _emailService.SendEmailAsync(photoShoot.ResponsiblePartyEmailAddress, "Cancelation Confirmed", emailBody);
            }

            return true;
        }

        public async Task<PhotoShootModel> GetPhotoShootById(Guid id)
        {
            var dbPhotoShoot = await _imageContext.PhotoShoots.Include(x => x.PaymentTransactions).FirstOrDefaultAsync(x => x.PhotoShootId == id);
            if ( dbPhotoShoot == null ) return null; 
            return MapPhotoShoot(dbPhotoShoot, _mapper);
        }

        public async Task<IEnumerable<PhotoShootModel>> GetPhotoShoots(PhotoShootFilerRequest filter)
        {
            var currentUserEmail = await _authService.GetCurrentUserEmail();
            if( currentUserEmail == null )
            {
                _logger.LogError("Unable to get current user email to GetPhotoShoots");
                return null;
            }
            _logger.LogInformation("Getting Photo Shoots for {userEmail}", currentUserEmail);

            var query = _imageContext.PhotoShoots.Where(x => !x.IsDeleted);

            if (filter.StartDate != null) query = query.Where(x => x.DateTimeUtc >= filter.StartDate.Value.ToUniversalTime());
            if (filter.EndDate != null) query = query.Where(x => x.DateTimeUtc < filter.EndDate.Value.ToUniversalTime());
            if (filter.ExcludePaidShoots == true)
            {
                query = query.Where(x => x.PaymentTransactions.Sum(x => x.Amount) < x.Price);
            }
            if (filter.ExcludeDeliveredShoots == true)
            {
                query = query.Where(x => x.PicturesDelivered == false);
            }

            var dbItems = await query.Include(x => x.PaymentTransactions).OrderBy(x => x.DateTimeUtc).ToListAsync();
            
            return dbItems.Select(x => MapPhotoShoot(x, _mapper)).ToList();
        }


        public async Task<CreatePhotoShootPaymentResponse> MakePaymentForPhotoShoot(CreatePhotoShootPaymentRequest photoShootPayment)
        {
            var photoShoot = await _imageContext.PhotoShoots.FindAsync(photoShootPayment.PhotoShootId);
            if( photoShoot == null )
            {
                return new CreatePhotoShootPaymentResponse
                {
                    IsSuccess = false,
                    PhotoShootId = photoShootPayment.PhotoShootId,
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
                PhotoShootId = photoShootPayment.PhotoShootId,
                ProcessorEnum = photoShootPayment.PaymentProcessorEnum,
                ProcessorOrderId = order.OrderId,
                InvoiceId = invoiceId,
            };
            return result;
        }

        public async Task<PhotoShootPaymentCaptureResponse> CapturePaymentForPhotoShoot(PhotoShootPaymentCaptureRequest photoShootCaptureRequest)
        {
            try
            {
                var photoShoot = await _imageContext.PhotoShoots.FindAsync(photoShootCaptureRequest.PhotoShootId);

                if(photoShoot == null)
                {
                    return new PhotoShootPaymentCaptureResponse
                    {
                        IsSuccess = false,
                        PhotoShootId = photoShootCaptureRequest.PhotoShootId,
                        ShouldTryAgain = false
                    };
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
                    return new PhotoShootPaymentCaptureResponse
                    {
                        IsSuccess = false,
                        ShouldTryAgain = false,
                        PhotoShootId = photoShootCaptureRequest.PhotoShootId
                    };
                }

                if( !capture.WasSuccessful )
                {
                    return new PhotoShootPaymentCaptureResponse
                    {
                        IsSuccess = false,
                        ShouldTryAgain = true,
                        PhotoShootId = photoShootCaptureRequest.PhotoShootId
                    };
                }

                // record transaction
                var paymentTransaction = new PaymentTransaction
                {
                    Amount = capture.PaymentTotal,
                    DateTimeUtc = DateTime.UtcNow,
                    Description = capture.Description,
                    PaymentTransactionId = photoShootCaptureRequest.InvoiceId,
                    PaymentProcessorType = photoShootCaptureRequest.PaymentProcessor,
                    PhotoShootId = photoShootCaptureRequest.PhotoShootId,
                    ExternalId = capture.ExternalOrderId,
                };

                _imageContext.PaymentTransactions.Add(paymentTransaction);
                await _imageContext.SaveChangesAsync();

                var totalPaid = await _imageContext.PaymentTransactions.Where(x => x.PhotoShootId == photoShoot.PhotoShootId).SumAsync(x => x.Amount);

                if (photoShoot.Deposit <= totalPaid)
                {
                    photoShoot.IsConfirmed = true;
                    _imageContext.PhotoShoots.Update(photoShoot);
                    await _imageContext.SaveChangesAsync();
                }


                // email recipet
                if( photoShoot.ResponsiblePartyEmailAddress != null )
                {
                    var emailBody = EmailContent.FormatEmail("Payment Recieved",
                        ["Hello!", $"Your payment of {capture.PaymentTotal} has been recieved for your photo shoot appointment for {GetLocalTimeStringFromUtc(photoShoot.DateTimeUtc)}."]);
                    await _emailService.SendEmailAsync(photoShoot.ResponsiblePartyEmailAddress, "Honey+Thyme Reciept", emailBody);
                }

                var adminEmail = _configuration["ContactForm:ToAddress"];
                var adminEmailBody = EmailContent.FormatEmail("Payment Recieved",
                    ["Hello!", $"A payment of {capture.PaymentTotal} has been recieved for the photo shoot appointment for {photoShoot.ResponsiblePartyName} {photoShoot.NameOfShoot}."]);
                await _emailService.SendEmailAsync(adminEmail, $"Payment Recieved For {photoShoot.ResponsiblePartyName} {photoShoot.NameOfShoot}", adminEmailBody);

                return new PhotoShootPaymentCaptureResponse
                {
                    IsSuccess = true,
                    ShouldTryAgain = false,
                    PhotoShootId = photoShootCaptureRequest.PhotoShootId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unrecoverable error trying to process payment for {phtoshootId}", photoShootCaptureRequest.PhotoShootId);
                return new PhotoShootPaymentCaptureResponse
                {
                    IsSuccess = false,
                    ShouldTryAgain = false,
                    PhotoShootId = photoShootCaptureRequest.PhotoShootId
                };
            }

        }

        private PhotoShootModel MapPhotoShoot(PhotoShoot photoShoot, IMapper mapper)
        {
            var ret = mapper.Map<PhotoShootModel>(photoShoot);
            var totalPaid = photoShoot.PaymentTransactions.Sum(x => x.Amount);
            ret.PaymentRemaining = photoShoot.Price - totalPaid - photoShoot.Discount;
            return ret;
        }

        private string GetLocalTimeStringFromUtc(DateTime dateTime)
        {
            var localTimeZoneName = _configuration["DamselflyConfiguration:Timezone"];
            var localTimeZone = TimeZoneInfo.FindSystemTimeZoneById(localTimeZoneName);
            return TimeZoneInfo.ConvertTimeFromUtc(dateTime, localTimeZone).ToString("MM/dd/yyyy hh:mm tt");
        }
    }
}
