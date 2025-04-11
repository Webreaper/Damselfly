using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.DbModels.Models.API_Models
{
    public class BookAppointmentResponse
    {
        public bool Success => Error == null;
        public PhotoShootModel? PhotoShoot { get; set; }
        public ErrorResponse? Error { get; set; }
    }
}
