using Damselfly.PaymentProcessing.Models.PayPal;
using Microsoft.Extensions.Configuration;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Damselfly.PaymentProcessing.Models;
using Damselfly.Core.DbModels.Models.Enums;


namespace Damselfly.PaymentProcessing.PaymentProcessors
{
    public class PayPalPaymentProcessor : IPaymentProcessor
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PayPalPaymentProcessor> _logger;
        private string BaseUrl => _configuration["PayPal:BaseUrl"];
        private readonly RestClient _restClient;

        public PayPalPaymentProcessor(IConfiguration configuration, ILogger<PayPalPaymentProcessor> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _restClient = new RestClient(new RestClientOptions(BaseUrl) { Timeout = new TimeSpan(0, 3, 0) });
        }

        private async Task<Token> GenerateAccessToken()
        {
            
            var request = new RestRequest("/v1/oauth2/token", Method.Post);
            request.AddParameter("grant_type", "client_credentials");
            request.AddParameter("ignoreCache", "true");
            request.AddParameter("return_authn_schemes", "true");
            request.AddParameter("return_client_metadata", "true");
            request.AddParameter("return_unconsented_scopes", "true");
            var basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes(_configuration["PayPal:ClientId"] + ":" + _configuration["PayPal:Secret"]));
            request.AddHeader("Authorization", $"Basic {basicAuth}");
            var response = await _restClient.ExecuteAsync(request);
            return JsonConvert.DeserializeObject<Token>(response.Content);
        }

        public async Task<CreateOrderResponse> CreateOrder(CreateOrderRequest orderRequest)
        {
            try
            {
                _logger.LogInformation("Creating Paypal order for invoice {invoiceId} for {amount}", orderRequest.InvoiceId, orderRequest.Amount);

                //var returnUrl = _configuration["PayPal:ReturnUrl"];
                //var cancelUrl = _configuration["PayPal:CancelUrl"];
                var accessToken = await GenerateAccessToken();
                var request = new RestRequest("/v2/checkout/orders", Method.Post);
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Prefer", "return=representation");
                request.AddHeader("PayPal-Request-Id", Guid.NewGuid().ToString());
                request.AddHeader("Authorization", $"Bearer {accessToken.AccessToken}");
                var order = new OrderRequest
                {
                    Intent = "CAPTURE",
                    //PaymentSource = new PaymentSource
                    //{
                    //    Paypal = new Paypal
                    //    {
                    //        ExperienceContext = new ExperienceContext
                    //        {
                    //            BrandName = "Honey and Thyme Photography",
                    //            PaymentMethodPreference = "IMMEDIATE_PAYMENT_REQUIRED",
                    //            Locale = "en-US",
                    //            UserAction = "PAY_NOW"

                    //        }
                    //    }
                    //},
                    PurchaseUnits = new List<PurchaseUnit>
                {
                    new PurchaseUnit
                    {
                        SoftDescriptor =orderRequest.ShortDescription,
                        Description = orderRequest.Description,
                        InvoiceId = orderRequest.InvoiceId,
                        ReferenceId = Guid.NewGuid().ToString(),
                        Amount = new Amount
                        {
                            CurrencyCode = "USD",
                            Value = orderRequest.Amount.ToString()
                        }
                    }
                },

                };
                var jsonRequest = JsonConvert.SerializeObject(order);
                _logger.LogInformation("Creating Paypal order with request {jsonRequest}", jsonRequest);
                request.AddStringBody(jsonRequest, DataFormat.Json);
                var response = await _restClient.ExecuteAsync(request);
                var orderResponse = JsonConvert.DeserializeObject<OrderResponse>(response.Content);
                _logger.LogInformation("Paypal order created with id {orderId} invoiceId {invoiceId}", orderResponse.Id, orderRequest.InvoiceId);
                return new CreateOrderResponse { OrderId = orderResponse.Id, IsSuccess = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Paypal order for {invoiceId}", orderRequest.InvoiceId);
                return new CreateOrderResponse
                {
                    IsSuccess = false,
                };
            }
        }
        
        public async Task<CaptureOrderResponse> CaptureOrder(CaptureOrderRequest captureRequest)
        {
            try
            {
                _logger.LogInformation("Capturing Paypal order {orderId} with invoiceId {invoiceId}", captureRequest.PaymentProcessorOrderId, captureRequest.InvoiceId);
                var accessToken = await GenerateAccessToken();
                var request = new RestRequest($"/v2/checkout/orders/{captureRequest.PaymentProcessorOrderId}/capture", Method.Post);
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Prefer", "return=representation");
                request.AddHeader("PayPal-Request-Id", Guid.NewGuid().ToString());
                request.AddHeader("Authorization", $"Bearer {accessToken.AccessToken}");
                var response = await _restClient.ExecuteAsync(request);
                if ( response.ResponseStatus != ResponseStatus.Completed )
                {
                    _logger.LogError("Request to Paypal could not be completed for {orderId} for invoice {invoiceId}", captureRequest.PaymentProcessorOrderId, captureRequest.InvoiceId);
                    return new CaptureOrderResponse { WasSuccessful = false, ErrorDuringCharge = true, PaymentTotal = 0 };
                };
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Paypal order {orderID} was not charged succesfully for invoice {invoiceId} {response}", captureRequest.PaymentProcessorOrderId, captureRequest.InvoiceId, response.Content);
                    return new CaptureOrderResponse { WasSuccessful = false, ErrorDuringCharge = false, PaymentTotal = 0 };
                }

                _logger.LogInformation("Paypal request was succesful for order {orderID} invoice {invoiceId} {resposne}", captureRequest.PaymentProcessorOrderId, captureRequest.InvoiceId, response.Content);
                var capture = JsonConvert.DeserializeObject<CaptureResponse>(response.Content);

                var total = capture.PurchaseUnits.Sum(x => x.Payments.Captures.Sum(y => decimal.Parse(y.Amount.Value)));
                var description = capture.PurchaseUnits.First().Description;

                return new CaptureOrderResponse 
                { 
                    WasSuccessful = true, 
                    ErrorDuringCharge = false, 
                    PaymentTotal = total, 
                    Description = description ,
                    ExternalOrderId = capture.Id,
                };
                
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Paypal order {orderId} failed during capture", captureRequest.PaymentProcessorOrderId);
                return new CaptureOrderResponse
                {
                    WasSuccessful = false,
                    ErrorDuringCharge = true
                };
            }
        }

        public bool CanHandle(PaymentProcessorEnum paymentProcessor)
        {
            return paymentProcessor == PaymentProcessorEnum.Paypal;
        }
    }
}
