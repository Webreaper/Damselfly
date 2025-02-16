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
            CreateMap<Album, AlbumModel>()
                .ForMember(am => am.IsLocked, c => c.MapFrom(a => a.InvalidPasswordAttempts > Album.MaxInvalidPasswordAttempts))
                .ForMember(am => am.Images, c => c.MapFrom(a => a.AlbumImages.Select(ai => ai.Image)));
            CreateMap<AlbumModel, Album>();
            CreateMap<EmailRecord, EmailRecordModel>();
            CreateMap<EmailRecordModel, EmailRecord>();
            CreateMap<ImageModel, Image>();
            CreateMap<Image, ImageModel>();
            CreateMap<ImageMetaData, ImageMetaDataModel>();
            CreateMap<ImageMetaDataModel, ImageMetaData>();
            CreateMap<PhotoShoot, PhotoShootModel>();
            CreateMap<PhotoShootModel, PhotoShoot>();
            CreateMap<Product, ProductModel>();
            CreateMap<ProductModel, Product>();
            CreateMap<PaymentTransaction, PaymentTransactionModel>();
            CreateMap<PaymentTransactionModel, PaymentTransaction>();
        }
    }
}
