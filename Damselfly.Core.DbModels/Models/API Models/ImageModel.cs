using Damselfly.Core.Models;
using Damselfly.Core.Models.SideCars;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.DbModels.Models.API_Models
{
    public class ImageModel
    {
        public Guid ImageId { get; set; }
        public string? FileName { get; set; }
        public DateTime SortDate { get; set; }

        public ImageMetaDataModel MetaData { get; set; } = new ImageMetaDataModel();
    }
}
