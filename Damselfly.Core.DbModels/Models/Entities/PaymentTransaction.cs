using Damselfly.Core.DbModels.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.DbModels.Models.Entities
{
    public class PaymentTransaction
    {
        [Key]
        public Guid PaymentTransactionId { get; set; }
        public Guid PhotoShootId { get; set; }
        public PhotoShoot PhotoShoot { get; set; }
        public DateTime DateTimeUtc { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public PaymentProcessorEnum PaymentProcessorType { get; set; }
        public string ExternalId { get; set; }
    }
}
