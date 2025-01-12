using Damselfly.Core.DbModels.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.PaymentProcessing.Models
{
    public class PaymentRequest
    {
        public PaymentProcessorEnum PaymentProcessor { get; set; }
        public decimal Amount { get; set; }
        public Guid PhotoShootId { get; set; }
    }
}
