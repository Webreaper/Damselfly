using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.DbModels.Models.API_Models
{
    public class UploadImageRequest
    {
        public required int AlbumId { get; set; }
        public required string FileName { get; set; }
        public List<IFormFile> ImageFile { get; set; }
    }
}
