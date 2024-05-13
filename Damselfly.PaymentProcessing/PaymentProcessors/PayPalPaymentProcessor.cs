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


namespace Damselfly.PaymentProcessing.PaymentProcessors
{
    public class PayPalPaymentProcessor : IPaymentProcessor
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PayPalPaymentProcessor> _logger;
        private string BaseUrl => _configuration["PayPal:BaseUrl"];
        private RestClient _restClient;

        public PayPalPaymentProcessor(IConfiguration configuration, ILogger<PayPalPaymentProcessor> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _restClient = new RestClient(new RestClientOptions(BaseUrl) { MaxTimeout = 30000 });
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

        public async Task<CreateOrderResponse> CreateOrder(decimal amount)
        {
            _logger.LogInformation("Creating Paypal order for {amount}", amount);
            
            //var returnUrl = _configuration["PayPal:ReturnUrl"];
            //var cancelUrl = _configuration["PayPal:CancelUrl"];
            var accessToken = await GenerateAccessToken();
            var request = new RestRequest("/v2/checkout/orders", Method.Post);
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Prefer", "return=representation");
            request.AddHeader("PayPal-Request-Id", "2601a2e1-e543-4646-bfbc-84b937f804fa");
            request.AddHeader("Authorization", $"Bearer {accessToken.AccessToken}");
            var order = new OrderRequest { 
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
                        ReferenceId = Guid.NewGuid().ToString(),
                        Amount = new Amount 
                        { 
                            CurrencyCode = "USD", 
                            Value = amount.ToString() 
                        } 
                    }
                } 
            };
            var jsonRequest = JsonConvert.SerializeObject(order);
            _logger.LogInformation("Creating Paypal order with request {jsonRequest}", jsonRequest);
            request.AddStringBody(jsonRequest, DataFormat.Json);
            var response = await _restClient.ExecuteAsync(request);
            var orderResponse = JsonConvert.DeserializeObject<OrderResponse>(response.Content);
            _logger.LogInformation("Paypal order created with id {orderId}", orderResponse.Id);
            return new CreateOrderResponse { OrderId = orderResponse.Id };
        }
        
        public async Task<CaptureOrderResponse> CaptureOrder(string orderId)
        {
            _logger.LogInformation("Capturing Paypal order {orderId}", orderId);
            var accessToken = await GenerateAccessToken();
            var request = new RestRequest("/v2/checkout/orders/{orderId}/capture", Method.Post);
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Prefer", "return=representation");
            request.AddHeader("PayPal-Request-Id", Guid.NewGuid().ToString());
            request.AddHeader("Authorization", $"Bearer {accessToken.AccessToken}");
            var response = await _restClient.ExecuteAsync(request);
            _logger.LogInformation("Paypal order {orderId} captured with response {response}", orderId, response.Content);
            return new CaptureOrderResponse{ WasSuccessful = response.IsSuccessful};
        }

        public bool CanHandle(PaymentProcessorEnum paymentProcessor)
        {
            return paymentProcessor == PaymentProcessorEnum.Paypal;
        }
    }
}
