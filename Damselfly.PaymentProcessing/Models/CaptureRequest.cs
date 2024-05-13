using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.PaymentProcessing.Models
{
    public class CaptureRequest
    {
        public PaymentProcessorEnum PaymentProcessor { get; set; }
        public string PaymentProcessorTransactionId { get; set; }
    }
}
