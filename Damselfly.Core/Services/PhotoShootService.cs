using AutoMapper;
using Damselfly.Core.Database;
using Damselfly.Core.DbModels.Models;
using Damselfly.Core.DbModels.Models.API_Models;
using Damselfly.Core.DbModels.Models.Entities;
using Damselfly.Core.Interfaces;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Utils;
using Damselfly.PaymentProcessing;
using Damselfly.PaymentProcessing.Models;
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
        IIpOriginService ipOriginService)
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
                var baseUrl = _configuration["DamselflyConfiguration:AllowedOrigins"].Split(",")[0];
                var emailBody = EmailContent.FormatEmail("Appointment Created",
                    ["Hello!", $"Your photoshoot for {GetLocalTimeStringFromUtc(photoShoot.DateTimeUtc)} has been scheduled but is not confirmed until deposit is paid. You can do this with the button below."],
                    $"{baseUrl}/#/invoice?id={photoShoot.PhotoShootId}", "Pay Deposit");
                await _emailService.SendEmailAsync(photoShoot.ResponsiblePartyEmailAddress, "Photo shoot scheduled with Honey + Thyme", emailBody);
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

            var dbPhotoShoot = await _imageContext.PhotoShoots.FindAsync(photoShoot.PhotoShootId);
            var album = await _imageContext.Albums.FindAsync(photoShoot.AlbumId);

            if (!dbPhotoShoot.PicturesDelivered && photoShoot.PicturesDelivered && photoShoot.ResponsiblePartyEmailAddress != null && album != null )
            {
                var baseUrl = _configuration["DamselflyConfiguration:AllowedOrigins"].Split(",")[0];
                var emailBody = EmailContent.FormatEmail("Photos Delivered",
                    [
                        "Your photos are ready!", 
                        $"Thank you again for letting me take your photos! Your link and pin are below.",
                        "I highly recommend downloading rather than taking a screenshot as downloading will give you a higher quality and sharpness. There are options on download size - with higher quality it can take multiple minutes to prepare your download.",
                        "If there are any changes you would like made, please feel free to reach out and I am happy to adjust where I can. When posting online, I'd love for you to tag me (unless you've made any editing changes yourself as then it is no longer fully my work)! This helps my business grow and I always love seeing which photos you choose to share!",
                        "Enjoy!",
                        "",
                        $"Password: {album.Password}"
                    ],
                    $"{baseUrl}/#/albums/{album.UrlName}", "View Photos"
                );
                await _emailService.SendEmailAsync(photoShoot.ResponsiblePartyEmailAddress, "Photos Ready", emailBody);
            }

            dbPhotoShoot = _mapper.Map<PhotoShoot>(photoShoot);
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
                var questionsEmail = _configuration["ContactForm:ToAddress"];
                var emailBody = EmailContent.FormatEmail("Appointment cancelation",
                    ["Hello!", $"Your photoshoot appointment for {GetLocalTimeStringFromUtc(photoShoot.DateTimeUtc)} has been cancelled. If you have any questions please feel to email me at {questionsEmail}"]);
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
            if( !await ValidateNotFraud() )
            {
                return new CreatePhotoShootPaymentResponse
                {
                    IsSuccess = false,
                    PhotoShootId = photoShootPayment.PhotoShootId,
                    ProcessorEnum = photoShootPayment.PaymentProcessorEnum,
                };
            }

            _logger.LogInformation("Creating payment with amount {amount} for photoshoot {phtooshootId}", photoShootPayment.Amount, photoShootPayment.PhotoShootId);
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
            _logger.LogInformation("Creating payment for photoshoot {photoshootId} IsSuccess {isSuccess}", photoShootPayment.PhotoShootId, result.IsSuccess);
            return result;
        }

        private async Task<bool> ValidateNotFraud()
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

        public async Task<PhotoShootPaymentCaptureResponse> CapturePaymentForPhotoShoot(PhotoShootPaymentCaptureRequest photoShootCaptureRequest)
        {
            try
            {
                _logger.LogInformation("Capturing payment for photoshoot {photoShootId} with externalOrderId {externalOrderId}", photoShootCaptureRequest.PhotoShootId, photoShootCaptureRequest.ExternalOrderId);
                var photoShoot = await _imageContext.PhotoShoots.FindAsync(photoShootCaptureRequest.PhotoShootId);

                if(photoShoot == null)
                {
                    _logger.LogWarning("No photoshoot found with id {photoshootId}", photoShootCaptureRequest.PhotoShootId);
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
                    _logger.LogWarning("There was an unrecoverable problem capturing payment for photoshoot {phtoshootId} with externalOrderId {externalOrderId}", 
                        photoShootCaptureRequest.PhotoShootId, photoShootCaptureRequest.ExternalOrderId);
                    return new PhotoShootPaymentCaptureResponse
                    {
                        IsSuccess = false,
                        ShouldTryAgain = false,
                        PhotoShootId = photoShootCaptureRequest.PhotoShootId
                    };
                }

                if( !capture.WasSuccessful )
                {
                    _logger.LogWarning("There was a problem capturing payment for photoshoot {phtoshootId} with externalOrderId {externalOrderId}, but the user should try again",
                        photoShootCaptureRequest.PhotoShootId, photoShootCaptureRequest.ExternalOrderId);
                    return new PhotoShootPaymentCaptureResponse
                    {
                        IsSuccess = false,
                        ShouldTryAgain = true,
                        PhotoShootId = photoShootCaptureRequest.PhotoShootId
                    };
                }

                _logger.LogInformation("Capturing payment for photoshoot {phtoshootId} with externalOrderId {externalOrderId} was succesful",
                        photoShootCaptureRequest.PhotoShootId, photoShootCaptureRequest.ExternalOrderId);
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

                _logger.LogInformation("Payment for photoshoot {phtoshootId} with externalOrderId {externalOrderId} was saved to the database, sending applicable emails",
                        photoShootCaptureRequest.PhotoShootId, photoShootCaptureRequest.ExternalOrderId);
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
                    //var recieptInfo = $"Shoot name: {photoShoot.NameOfShoot} <br>" +
                    //    $"Deposit: ${photoShoot.Deposit}<br>" +
                    //    $"Total price: ${photoShoot.Price} <br>" +
                    //    $"Paid: ${totalPaid} <br>" +
                    //    $"Balance due: ${remainingBalance} <br>";
                    var emailBody = EmailContent.FormatEmail("Appintment confirmation",
                        [$"Congrats! Your photoshoot is booked! Any remaining balance will be due the day of your shoot."], tableElements: recieptInfo);
                    if( paidInFull )
                    {
                        emailBody = EmailContent.FormatEmail("Thank you for your payment",
                            [$"Thank you for your payment!"], tableElements: recieptInfo);
                    }
                    await _emailService.SendEmailAsync(photoShoot.ResponsiblePartyEmailAddress, "Honey+Thyme Reciept", emailBody);
                }

                var adminEmail = _configuration["ContactForm:ToAddress"];
                var primaryMessage = "";
                if( paidInFull ) primaryMessage = $"A payment of ${capture.PaymentTotal} has been recieved for the photo shoot appointment for {photoShoot.ResponsiblePartyName} {photoShoot.NameOfShoot} and is now marked as paid in full.";
                else primaryMessage = $"You got a booking a payment of ${capture.PaymentTotal} has been recieved for the photo shoot appointment for {photoShoot.ResponsiblePartyName} {photoShoot.NameOfShoot}";
                var adminEmailBody = EmailContent.FormatEmail("Payment Recieved",
                    ["Hello!", primaryMessage]);
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

        public async Task SendReminderEmails()
        {
            var localTimeNow = GetLocalDateTimeFromUtc(DateTime.UtcNow);
            var startOfDay = localTimeNow.Date;
            var endOfDay = startOfDay.AddDays(1);
            var photoShoots = await _imageContext.PhotoShoots
                .Where(x => x.DateTimeUtc >= startOfDay.ToUniversalTime() && x.DateTimeUtc < endOfDay)
                .Where(x => !x.ReminderSent)
                .Where(x => x.IsConfirmed)
                .Where(x => x.PaymentTransactions.Sum(x => x.Amount) < x.Price)
                .Include(x => x.PaymentTransactions)
                .ToListAsync();
            foreach( var photoShoot in photoShoots )
            {
                if (string.IsNullOrWhiteSpace( photoShoot.ResponsiblePartyEmailAddress)) continue;

                //var reminderText = $"This is a reminder that your photo shoot appointment for {GetLocalTimeStringFromUtc(photoShoot.DateTimeUtc)} is today! Look forward to seeing you soon!";
                //var reminderSubject = "Photo shoot reminder with Honey + Thyme";
                //if (photoShoot.DateTimeUtc < DateTime.UtcNow )
                //{
                //    reminderText = $"It was a pleasure to be your photographer today! Your photos should be ready in a few days, please take a moment to pay your balance at your earliest convience. Photos cannot be delivered to you prior to payment without prior disucssion.";
                //    reminderSubject = "Photo shoot payment reminder with Honey + Thyme";
                //}
                var baseUrl = _configuration["DamselflyConfiguration:AllowedOrigins"].Split(",")[0];
                var totalPaid = photoShoot.PaymentTransactions.Sum(x => x.Amount);
                var remainingBalance = photoShoot.Price - totalPaid;
                var emailBody = EmailContent.FormatEmail("Payment Reminder",
                    [$"Hey there! This is just a friendly reminder that your remaining balance of {remainingBalance} is due today after your shoot. Thanks again for your business!"],
                    $"{baseUrl}/#/invoice?id={photoShoot.PhotoShootId}", "Pay Balance");
                await _emailService.SendEmailAsync(photoShoot.ResponsiblePartyEmailAddress, "Photoshoot reminder", emailBody);
                photoShoot.ReminderSent = true;
                _imageContext.PhotoShoots.Update(photoShoot);
                await _imageContext.SaveChangesAsync();
            }
        }

        private PhotoShootModel MapPhotoShoot(PhotoShoot photoShoot, IMapper mapper)
        {
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
    }
}
