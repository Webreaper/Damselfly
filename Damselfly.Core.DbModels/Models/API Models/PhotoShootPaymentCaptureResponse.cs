using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.DbModels.Models.API_Models
{
    public class PhotoShootPaymentCaptureResponse
    {
        public bool IsSuccess { get; set; }
        public bool ShouldTryAgain { get; set; }
        public Guid PhotoShootId { get; set; }
    }
}
