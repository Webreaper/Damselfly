using Damselfly.PaymentProcessing.Models;
using Damselfly.PaymentProcessing.PaymentProcessors;
using Microsoft.Extensions.Logging;

namespace Damselfly.PaymentProcessing
{
    public class PaymentService(
        IPaymentProcessorFactory paymentProcessorFactory,
        ILogger<PaymentService> logger)
    {
        private readonly IPaymentProcessorFactory _paymentProcessorFactory = paymentProcessorFactory;
        private readonly ILogger<PaymentService> _logger = logger;

        public async Task<CreateOrderResponse> CreateOrder(CreateOrderRequest orderRequest)
        {
            var paymentProcessor = _paymentProcessorFactory.CreatePaymentProcessor(orderRequest.PaymentProcessorEnum);
            var order = await paymentProcessor.CreateOrder(orderRequest);
            return order;
        }

        public async Task<CaptureOrderResponse> CaptureOrder(CaptureOrderRequest captureRequest)
        {
            var paymentProcessor = _paymentProcessorFactory.CreatePaymentProcessor(captureRequest.PaymentProcessor);
            return await paymentProcessor.CaptureOrder(captureRequest);
        }
    }
}
