using Damselfly.PaymentProcessing.Models;
using Damselfly.PaymentProcessing.PaymentProcessors;

namespace Damselfly.PaymentProcessing
{
    public class PaymentService(IPaymentProcessorFactory paymentProcessorFactory)
    {
        private readonly IPaymentProcessorFactory _paymentProcessorFactory = paymentProcessorFactory;

        public async Task<CreateOrderResponse> CreateOrder(PaymentRequest paymentRequest)
        {
            var paymentProcessor = _paymentProcessorFactory.CreatePaymentProcessor(paymentRequest.PaymentProcessor);
            var order = await paymentProcessor.CreateOrder(paymentRequest.Amount);
            return order;
        }

        public async Task<CaptureOrderResponse> CaptureOrder(CaptureRequest captureRequest)
        {
            var paymentProcessor = _paymentProcessorFactory.CreatePaymentProcessor(captureRequest.PaymentProcessor);
            var wasSuccesful = await paymentProcessor.CaptureOrder(captureRequest.PaymentProcessorTransactionId);
            return wasSuccesful;
        }
    }
}
