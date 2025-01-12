using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.PaymentProcessing.Models.PayPal
{
    public class SellerReceivableBreakdown
    {
        [JsonProperty("gross_amount")]
        public GrossAmount GrossAmount { get; set; }

        [JsonProperty("paypal_fee")]
        public PaypalFee PaypalFee { get; set; }

        [JsonProperty("net_amount")]
        public NetAmount NetAmount { get; set; }
    }
}
