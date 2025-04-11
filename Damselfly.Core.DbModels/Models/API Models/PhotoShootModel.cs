using Damselfly.Core.DbModels.Models.Enums;
using System;

namespace Damselfly.Core.DbModels.Models.API_Models
{
    public class PhotoShootModel
    {
        public Guid PhotoShootId { get; set; }
        public string? ResponsiblePartyName { get; set; }
        public string? ResponsiblePartyEmailAddress { get; set; }
        public string? NameOfShoot { get; set; }
        public string? Description {  get; set; }
        public DateTime DateTimeUtc { get; set; }
        public DateTime? EndDateTimeUtc { get; set; }
        public string? Location { get; set; }
        public decimal Price { get; set; }
        public decimal Deposit { get; set; }
        public decimal? Discount { get; set; }
        public string? DiscountName { get; set; }
        public decimal? PaymentRemaining { get; set; }
        public PhotoShootStatusEnum Status { get; set; }
        public PhotoShootTypeEnum PhotoShootType { get; set; }
        public string? ReservationCode { get; set; }
        public Guid? AlbumId { get; set; }
    }
}
