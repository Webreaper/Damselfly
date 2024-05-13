using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.PaymentProcessing.Models.PayPal
{
    internal class OrderResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("payment_source")]
        public PaymentSource PaymentSource { get; set; }

        [JsonProperty("links")]
        public List<Link> Links { get; set; }
    }
}
