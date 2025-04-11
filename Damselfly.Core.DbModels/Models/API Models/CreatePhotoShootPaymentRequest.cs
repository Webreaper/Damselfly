using Damselfly.Core.DbModels.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.DbModels.Models.API_Models
{
    public class CreatePhotoShootPaymentRequest
    {
        public string ReservationCode { get; set; }
        public decimal Amount { get; set; }
        public PaymentProcessorEnum PaymentProcessorEnum { get; set; }
        public string Description { get; set; }
    }
}
