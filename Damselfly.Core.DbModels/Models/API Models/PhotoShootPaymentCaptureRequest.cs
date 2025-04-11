using Damselfly.Core.DbModels.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.DbModels.Models.API_Models
{
    public class PhotoShootPaymentCaptureRequest
    {
        public string ReservationCode { get; set; }
        public string ExternalOrderId { get; set; }
        public decimal AmountToBeCharged { get; set; }
        public PaymentProcessorEnum PaymentProcessor { get; set; }
        public Guid InvoiceId { get; set; }
    }
}
