using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.PaymentProcessing.Models.PayPal
{

    internal class OrderRequest
    {
        [JsonProperty("intent")]
        public string Intent { get; set; }

        [JsonProperty("purchase_units")]
        public List<PurchaseUnit> PurchaseUnits { get; set; }

        [JsonProperty("payment_source")]
        public PaymentSource PaymentSource { get; set; }
    }



}
