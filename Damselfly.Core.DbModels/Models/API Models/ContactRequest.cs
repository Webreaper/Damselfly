using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.DbModels.Models.API_Models
{
    public class ContactRequest
    {
        [MaxLength(254)]
        public string Email { get; set; }
        [MaxLength(1000)]
        public string Message { get; set; }
    }
}
