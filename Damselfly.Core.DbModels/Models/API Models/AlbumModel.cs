using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.DbModels.Models.API_Models
{
    public class AlbumModel
    {
        public Guid? AlbumId { get; set; }
        public string Name { get; set; }
        public string UrlName { get; set; }
        public string Description { get; set; }
        public bool IsPublic { get; set; }
        public string? Password { get; set; }
        public Guid? CoverImageId { get; set; }
        public bool? IsLocked { get; set; }
        public DateTime CreatedDateTime { get; set; }

        public List<ImageModel> Images { get; set; } = [];
    }
}
