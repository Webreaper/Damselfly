using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.DbModels.Models.API_Models
{
    public class ImageModel
    {
        public int ImageId { get; set; }
        public string? FileName { get; set; }
        public DateTime SortDate { get; set; }
    }
}
