using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.DbModels.Models.API_Models
{
    public class BookingRequest
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public int NumberOfPeople { get; set; }
        public string SessionLength { get; set; }
        public string Occasion { get; set; }
        public string Location { get; set; }
        public string Questions { get; set; }
    }
}
