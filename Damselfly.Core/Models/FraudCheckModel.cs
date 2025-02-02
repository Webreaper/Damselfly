using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.Models
{
    internal class FraudCheckModel
    {
        public string IpAddress { get; set; }
        public int AttemptCount { get; set; }
        public string Country { get; set; }
    }
}
