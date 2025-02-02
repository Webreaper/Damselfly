using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.DbModels.Models.Entities
{
    public class PhotoShoot
    {
        [Key]
        public Guid PhotoShootId { get; set; }
        public string ResponsiblePartyName { get; set; }
        public string? ResponsiblePartyEmailAddress { get; set; }
        public string? NameOfShoot { get; set; }
        public string? Description { get; set; }
        public DateTime DateTimeUtc { get; set; }
        public decimal Price { get; set; }
        public decimal Deposit { get; set; }
        public decimal? Discount { get; set; }
        public string? DiscountName { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsConfirmed { get; set; }
        public bool PicturesDelivered { get; set; }
        public bool ReminderSent { get; set; }

        public Guid? AlbumId { get; set; }
        public Album? Album { get; set; }

        public virtual List<PaymentTransaction> PaymentTransactions { get; set; }
    }
}
