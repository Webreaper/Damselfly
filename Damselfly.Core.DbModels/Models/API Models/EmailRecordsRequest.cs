using Damselfly.Core.DbModels.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.DbModels.Models.API_Models
{
    public class EmailRecordsRequest : PaginationRequestModel
    {
        public MessageObjectEnum? MessageObject { get; set; }
        public string? MessageObjectId { get; set; }
    }
}
