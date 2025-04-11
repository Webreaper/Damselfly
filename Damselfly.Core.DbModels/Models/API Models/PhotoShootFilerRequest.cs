using Damselfly.Core.DbModels.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.DbModels.Models.API_Models
{
    public class PhotoShootFilerRequest : PaginationRequestModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<PhotoShootStatusEnum>? Statuses { get; set; }
        public PhotoShootTypeEnum? PhotoShootType { get; set; }
    }
}
