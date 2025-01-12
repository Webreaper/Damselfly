using Damselfly.Core.DbModels.Models.Enums;
using Damselfly.PaymentProcessing.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.PaymentProcessing.PaymentProcessors
{
    public interface IPaymentProcessor
    {
        bool CanHandle(PaymentProcessorEnum paymentProcessor);
        Task<CreateOrderResponse> CreateOrder(CreateOrderRequest orderRequest);
        Task<CaptureOrderResponse> CaptureOrder(CaptureOrderRequest captureRequest);
    }
}
