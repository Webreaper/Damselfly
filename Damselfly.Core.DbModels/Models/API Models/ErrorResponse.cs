using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.DbModels.Models.API_Models
{
    public class ErrorResponse
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
    }
}
