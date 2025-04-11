using Damselfly.Core.DbModels.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.DbModels.Models.API_Models
{
    public class CreatePhotoShootPaymentResponse
    {
        public string ReservationCode { get; set; }
        public string ProcessorOrderId { get; set; }
        public Guid InvoiceId { get; set; }
        public PaymentProcessorEnum ProcessorEnum { get; set; }
        public bool IsSuccess { get; set; }
    }
}
