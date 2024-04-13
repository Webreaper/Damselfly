using AutoMapper;
using Damselfly.Core.Database;
using Damselfly.Core.DbModels.Models.API_Models;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.DbModels.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.Services
{
    public class AlbumService
    {
        private readonly IMapper _mapper;
        private readonly ImageContext _context;
        private readonly FileService _fileService;

        public AlbumService(IMapper mapper, ImageContext imageContext, FileService fileService)
        {
            _mapper = mapper;
            _context = imageContext;
            _fileService = fileService;
        }

        public async Task<AlbumModel> CreateAlbum(AlbumModel albumModel)
        {
            var album = _mapper.Map<Album>(albumModel);
            _context.Albums.Add(album);
            await _context.SaveChangesAsync();
            return _mapper.Map<AlbumModel>(album);
        }

        public async Task<AlbumModel> UpdateAlbum(AlbumModel albumModel )
        {
            var album = _mapper.Map<Album>(albumModel);
            _context.Albums.Update(album);
            await _context.SaveChangesAsync();
            return _mapper.Map<AlbumModel>(album);
        }

        public async Task<bool> DeleteAlbum(int id) 
        {
            var affectedImages = _context.Images.Where(i => i.Albums.Any(a => a.AlbumId == id));
            var deleteImages = affectedImages.Where(i => i.Albums.Count == 1).ToList(); 
            var updateImages = affectedImages.Where(i => i.Albums.Count > 1).ToList();
            foreach(var image in updateImages)
            {                 
                image.Albums.Remove(image.Albums.First(a => a.AlbumId == id));
            }
            var deleteResult = await _fileService.DeleteImages(new MultiImageRequest { ImageIDs = deleteImages.Select(i => i.ImageId).ToList() });
            if(deleteResult)
            {
                _context.Albums.Remove(_context.Albums.First(a => a.AlbumId == id));
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }


    }
}
