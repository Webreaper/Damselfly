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
    public class ImageService
    {
        private readonly ThumbnailService _thumbnailService;
        private readonly ImageContext _context;
        private readonly FileService _fileService;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;

        public ImageService(ThumbnailService thumbnailService, ImageContext imageContext, FileService fileService, IConfiguration configuration, IMapper mapper)
        {
            _thumbnailService = thumbnailService;
            _context = imageContext;
            _fileService = fileService;
            _configuration = configuration;
            _mapper = mapper;
        }

        public async Task<List<ImageModel>> CreateImages(UploadImageRequest uploadImageRequest)
        {
            var album = _context.Albums.FirstOrDefault(a => a.AlbumId == uploadImageRequest.AlbumId);
            if (album == null)
            {
                throw new Exception("Album not found");
            }
            var rootPath = _configuration["SourceDirectory"];
            var filePath = Path.Combine(rootPath, album.Name);
            var images = new List<Image>();
            foreach (var imageFile in uploadImageRequest.ImageFile)
            {
                using( var memoryStream = new MemoryStream() )
                {
                    await imageFile.OpenReadStream().CopyToAsync(memoryStream);
                    var imagePath = Path.Combine(rootPath, imageFile.FileName);
                    await File.WriteAllBytesAsync(imagePath, memoryStream.ToArray());
                }
                
                var image = new Image { FileName = imageFile.FileName, SortDate = DateTime.Now };
                album.Images.Add(image);
                _context.Images.Add(image);
            }
            await _context.SaveChangesAsync();
            var imageModels = new List<ImageModel>();
            foreach (var image in images)
            {
                _thumbnailService.CreateThumb(image.ImageId, Constants.ThumbSize.Large);
                _thumbnailService.CreateThumb(image.ImageId, Constants.ThumbSize.Medium);
                _thumbnailService.CreateThumb(image.ImageId, Constants.ThumbSize.Small);
                imageModels.Add(_mapper.Map<ImageModel>(image));
            }
            return imageModels;
            
        }

    }
}
