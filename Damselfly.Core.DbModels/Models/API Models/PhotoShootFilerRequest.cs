using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.DbModels.Models
{
    public class PhotoShootFilerRequest
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? ExcludePaidShoots { get; set; }
        public bool? ExcludeDeliveredShoots { get; set; }
    }
}
