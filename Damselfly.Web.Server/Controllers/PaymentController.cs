using Damselfly.Core.DbModels.Models.API_Models;
using Damselfly.PaymentProcessing;
using Damselfly.PaymentProcessing.Models;
using Microsoft.AspNetCore.Mvc;


namespace Damselfly.Web.Server.Controllers
{
    [ApiController]
    [Route("api/payment")]
    public class PaymentController(PaymentService paymentProcessor, ILogger<PaymentController> logger) : ControllerBase
    {
        private readonly ILogger<PaymentController> _logger = logger;
        private readonly PaymentService _paymentProcessor = paymentProcessor;

        [HttpPost("createOrder")]
        [ProducesDefaultResponseType(typeof(CreateOrderResponse))]
        public async Task<IActionResult> CreateOrder(PaymentRequest paymentRequest)
        {
            _logger.LogInformation("Creating order for payment request: {paymentRequest}", paymentRequest.Amount);
            var createOrderResponse = await _paymentProcessor.CreateOrder(paymentRequest);
            return Ok(createOrderResponse);
        }

        [HttpPost("captureOrder")]
        [ProducesDefaultResponseType(typeof(CaptureOrderResponse))]
        public async Task<IActionResult> CaptureOrder(CaptureRequest paymentCaptureRequest)
        {
            try
            {
                _logger.LogInformation("Capturing order for payment request: {paymentCaptureRequest}", paymentCaptureRequest.PaymentProcessorTransactionId);
                var captureOrderResponse = await _paymentProcessor.CaptureOrder(paymentCaptureRequest);
                return Ok(captureOrderResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing order for payment request: {paymentCaptureRequest}", paymentCaptureRequest.PaymentProcessorTransactionId);
                return Ok(false);
            }
        }
    }
}
