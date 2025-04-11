using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.DbModels.Models.API_Models
{
    public class ScheduleAppointmentRequest
    {
        public Guid PhotoShootId { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
    }
}
