using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.PaymentProcessing.Models.PayPal
{
    public class Shipping
    {
        [JsonProperty("address")]
        public Address Address { get; set; }
    }
}
