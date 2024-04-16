using Damselfly.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.DbModels.Models.Entities
{
    public class Album
    {
        public int AlbumId { get; set; }
        public string Name { get; set; }
        public string UrlName { get; set; } 
        public string Description { get; set; }
        public bool IsPublic { get; set; }
        public string? Password { get; set; }
        public int? CoverImageId { get; set; }
        public int FolderId { get; set; }

        public virtual List<Image> Images { get; set; } = [];

        public virtual Image? CoverImage { get; set; }
        public virtual Folder Folder { get; set; }

    }
}
