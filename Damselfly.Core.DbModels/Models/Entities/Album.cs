using Damselfly.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.DbModels.Models.Entities
{
    public class Album
    {
        [Key]

        public Guid AlbumId { get; set; } = new Guid();
        public string Name { get; set; }
        public string UrlName { get; set; } 
        public string Description { get; set; }
        public int InvalidPasswordAttempts { get; set; }
        public bool IsPublic { get; set; }
        public string? Password { get; set; }
        public Guid? CoverImageId { get; set; }
        public Guid FolderId { get; set; }
        public DateTime CreatedDateTime { get; set; } = new DateTime();

        public virtual List<AlbumImage> AlbumImages { get; set; } = [];

        public virtual List<PhotoShoot>? PhotoShoots { get; set; } = [];
        public virtual Image? CoverImage { get; set; }
        public virtual Folder Folder { get; set; }

        public const int MaxInvalidPasswordAttempts = 4;
    }
}
