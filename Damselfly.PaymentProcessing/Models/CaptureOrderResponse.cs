using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.PaymentProcessing.Models
{
    public class CaptureOrderResponse
    {
        public bool WasSuccessful { get; set; }
        public bool ErrorDuringCharge { get; set; }
        public decimal PaymentTotal { get; set; }
        public string Description { get; set; }
        public string ExternalOrderId { get; set; }
    }
}
