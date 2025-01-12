using Damselfly.Core.DbModels.Models.Enums;
using Damselfly.PaymentProcessing.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.PaymentProcessing.PaymentProcessors
{
    public class ExternalPaymentProcessor : IPaymentProcessor
    {
        public bool CanHandle(PaymentProcessorEnum paymentProcessor)
        {
            return paymentProcessor == PaymentProcessorEnum.External;
        }

        public async Task<CaptureOrderResponse> CaptureOrder(CaptureOrderRequest captureRequest)
        {
            return new CaptureOrderResponse
            {
                Description = "Paid from external processor",
                ErrorDuringCharge = false,
                PaymentTotal = captureRequest.Amount,
                WasSuccessful = true,
                ExternalOrderId = captureRequest.PaymentProcessorOrderId,
            };
        }


        public async Task<CreateOrderResponse> CreateOrder(CreateOrderRequest orderRequest)
        {
            return new CreateOrderResponse
            {
                IsSuccess = true,
                OrderId = orderRequest.InvoiceId,
            };
        }
    }
}
