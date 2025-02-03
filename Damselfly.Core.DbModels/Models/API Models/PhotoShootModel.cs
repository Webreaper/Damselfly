using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.DbModels.Models.API_Models
{
    public class PhotoShootModel
    {
        public Guid PhotoShootId { get; set; }
        public string ResponsiblePartyName { get; set; }
        public string? ResponsiblePartyEmailAddress { get; set; }
        public string? NameOfShoot { get; set; }
        public string? Description {  get; set; }
        public DateTime DateTimeUtc { get; set; }
        public decimal Price { get; set; }
        public decimal Deposit { get; set; }
        public decimal? Discount { get; set; }
        public string? DiscountName { get; set; }
        public bool IsConfirmed { get; set; }
        public decimal? PaymentRemaining { get; set; }
        public bool PicturesDelivered { get; set; }
        public Guid? AlbumId { get; set; }
    }
}
