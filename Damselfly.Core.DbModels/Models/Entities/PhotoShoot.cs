using Damselfly.Core.DbModels.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Damselfly.Core.DbModels.Models.Entities
{
    public class PhotoShoot
    {
        [Key]
        public Guid PhotoShootId { get; set; }
        public string? ResponsiblePartyName { get; set; }
        public string? ResponsiblePartyEmailAddress { get; set; }
        public string? NameOfShoot { get; set; }
        public string? Description { get; set; }
        public DateTime DateTimeUtc { get; set; }
        public DateTime? EndDateTimeUtc { get; set; }
        public string? Location { get; set; }
        public string? ExternalCalendarId { get; set; }
        public decimal Price { get; set; }
        public decimal Deposit { get; set; }
        public decimal? Discount { get; set; }
        public string? DiscountName { get; set; }
        public bool ReminderSent { get; set; }
        public PhotoShootStatusEnum Status { get; set; } = PhotoShootStatusEnum.Unbooked;
        public PhotoShootTypeEnum PhotoShootType { get; set; } = PhotoShootTypeEnum.CustomBooking;
        public DateTime? RequestExpirationDateTime { get; set; }
        public string? ReservationCode { get; set; }
        

        public Guid? AlbumId { get; set; }
        public Album? Album { get; set; }

        public virtual List<PaymentTransaction> PaymentTransactions { get; set; }
    }
}
