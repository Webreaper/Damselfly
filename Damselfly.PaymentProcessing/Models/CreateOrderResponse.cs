using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.PaymentProcessing.Models
{
    public class CreateOrderResponse
    {
        public string OrderId { get; set; }
        public bool IsSuccess { get; set; }
    }
}
