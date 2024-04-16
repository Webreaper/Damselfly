using AutoMapper;
using Damselfly.Core.DbModels.Models.API_Models;
using Damselfly.Core.DbModels.Models.Entities;
using Damselfly.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.AutoMapper
{
    public class AutoMapperProfile : Profile
    {

        public AutoMapperProfile()
        {
            CreateMap<Album, AlbumModel>();
            CreateMap<AlbumModel, Album>();
            CreateMap<ImageModel, Image>();
            CreateMap<Image, ImageModel>();
            CreateMap<ImageMetaData, ImageMetaDataModel>();
            CreateMap<ImageMetaDataModel, ImageMetaData>();
        }
    }
}
