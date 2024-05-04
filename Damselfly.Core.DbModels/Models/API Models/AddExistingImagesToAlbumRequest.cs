using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.DbModels.Models.API_Models
{
    public class AddExistingImagesToAlbumRequest
    {
        public Guid AlbumId { get; set; }
        public ICollection<Guid> ImageIds { get; set; }
    }
}
