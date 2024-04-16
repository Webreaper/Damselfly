using AutoMapper;
using Damselfly.Core.Database;
using Damselfly.Core.DbModels.Models.API_Models;
using Damselfly.Core.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.Services
{
    public class ImageService(ThumbnailService thumbnailService, ImageContext imageContext, FileService fileService, IConfiguration configuration, IMapper mapper, MetaDataService metaDataService)
    {
        private readonly ThumbnailService _thumbnailService = thumbnailService;
        private readonly ImageContext _context = imageContext;
        private readonly FileService _fileService = fileService;
        private readonly MetaDataService _metaDataService = metaDataService;
        private readonly IConfiguration _configuration = configuration;
        private readonly IMapper _mapper = mapper;

        public async Task<List<ImageModel>> CreateImages(UploadImageRequest uploadImageRequest)
        {
            var album = _context.Albums.FirstOrDefault(a => a.AlbumId == uploadImageRequest.AlbumId);
            if( album == null )
            {
                throw new Exception("Album not found");
            }
            var rootPath = _configuration["DamselflyConfiguration:SourceDirectory"];
            var filePath = Path.Combine(rootPath, album.Name);
            var images = new List<Image>();
            foreach (var imageFile in uploadImageRequest.ImageFiles)
            {
                using( var memoryStream = new MemoryStream() )
                {
                    await imageFile.OpenReadStream().CopyToAsync(memoryStream);
                    var imagePath = Path.Combine(filePath, imageFile.FileName);
                    await File.WriteAllBytesAsync(imagePath, memoryStream.ToArray());
                }
                
                var image = new Image { FileName = imageFile.FileName, SortDate = DateTime.Now, FolderId = album.FolderId };
                album.Images.Add(image);
                //image.Albums.Add(album);
                _context.Images.Add(image);
                _context.Albums.Update(album);
                images.Add(image);
            }
            await _context.SaveChangesAsync();
            var imageModels = new List<ImageModel>();
            foreach (var image in images)
            {
                await _metaDataService.ScanMetaData(image.ImageId);
                await _thumbnailService.CreateThumb(image.ImageId, Constants.ThumbSize.Large);
                await _thumbnailService.CreateThumb(image.ImageId, Constants.ThumbSize.Medium);
                await _thumbnailService.CreateThumb(image.ImageId, Constants.ThumbSize.Small);
                imageModels.Add(_mapper.Map<ImageModel>(image));
            }
            return imageModels;
            
        }

    }
}
