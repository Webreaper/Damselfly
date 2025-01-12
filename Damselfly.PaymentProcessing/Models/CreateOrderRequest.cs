using Damselfly.Core.DbModels.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.PaymentProcessing.Models
{
    public class CreateOrderRequest
    {
        public string InvoiceId { get; set; }
        public string Description { get; set; }
        public string ShortDescription { get; set; }
        public decimal Amount { get; set; }
        public PaymentProcessorEnum PaymentProcessorEnum { get; set; }
    }
}
