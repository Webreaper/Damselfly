using Damselfly.PaymentProcessing.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.PaymentProcessing.PaymentProcessors
{
    public interface IPaymentProcessorFactory
    {
        IPaymentProcessor CreatePaymentProcessor(PaymentProcessorEnum paymentProcessorName);
    }
}
