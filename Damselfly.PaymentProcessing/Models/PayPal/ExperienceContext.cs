using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.PaymentProcessing.Models.PayPal
{
    internal class ExperienceContext
    {
        [JsonProperty("payment_method_preference")]
        public string PaymentMethodPreference { get; set; }

        [JsonProperty("brand_name")]
        public string BrandName { get; set; }

        [JsonProperty("locale")]
        public string Locale { get; set; }

        [JsonProperty("landing_page")]
        public string LandingPage { get; set; }

        [JsonProperty("shipping_preference")]
        public string ShippingPreference { get; set; }

        [JsonProperty("user_action")]
        public string UserAction { get; set; }

        [JsonProperty("return_url")]
        public string ReturnUrl { get; set; }

        [JsonProperty("cancel_url")]
        public string CancelUrl { get; set; }
    }
}
