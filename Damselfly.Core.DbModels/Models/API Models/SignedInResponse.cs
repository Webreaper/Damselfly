using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.DbModels.Models.API_Models
{
    public class SignedInResponse
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
    }
}
