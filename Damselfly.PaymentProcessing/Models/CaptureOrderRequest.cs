using Damselfly.Core.DbModels.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.PaymentProcessing.Models
{
    public class CaptureOrderRequest
    {
        public string InvoiceId { get; set; }
        public string PaymentProcessorOrderId { get; set; }
        public PaymentProcessorEnum PaymentProcessor { get; set; }
        public decimal Amount { get; set; }
    }
}
