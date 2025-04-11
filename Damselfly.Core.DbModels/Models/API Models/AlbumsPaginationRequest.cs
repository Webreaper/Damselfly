using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.DbModels.Models.API_Models
{
    public class AlbumsPaginationRequest : PaginationRequestModel
    {
        public string? Search { get; set; }
    }
} 