using Damselfly.Core.DbModels.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.DbModels.Models.API_Models
{
    public class EmailRecordModel
    {
        public Guid EmailRecordId { get; set; }
        public string? Email { get; set; }
        public string? Subject { get; set; }
        public string? HtmlMessage { get; set; }
        public DateTime? DateSent { get; set; }
        public MessageStatusEnum? Status { get; set; }
        public MessageObjectEnum? MessageObject { get; set; }
        public string? MessageObjectId { get; set; }
    }
}
