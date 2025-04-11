using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.DbModels.Models.Enums
{
    public enum PhotoShootStatusEnum
    {
        Unbooked = 0,
        Scheduled = 1,
        Booked = 2,
        Confirmed = 3,
        Paid = 4,
        Delivered = 5,
        Deleted = 6,
    }
}
