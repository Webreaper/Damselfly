using Damselfly.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.DbModels.Models.Entities
{
    public class AlbumImage
    {
        public Guid AlbumImageId { get; set; } = new Guid();
        public Guid AlbumId { get; set; }
        public Guid ImageId { get; set; }
        public Album Album { get; set; }
        public Image Image { get; set; }
    }
}
