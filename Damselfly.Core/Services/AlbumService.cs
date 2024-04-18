using AutoMapper;
using Damselfly.Core.Database;
using Damselfly.Core.DbModels.Models.API_Models;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.DbModels.Models.Entities;
using Damselfly.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.Services
{
    public class AlbumService(IMapper mapper, ImageContext imageContext, FileService fileService, IConfiguration configuration, IndexingService indexingService)
    {
        private readonly IMapper _mapper = mapper;
        private readonly ImageContext _context = imageContext;
        private readonly FileService _fileService = fileService;
        private readonly IConfiguration _configuration = configuration;
        private readonly IndexingService _indexingService = indexingService;

        public async Task<AlbumModel> CreateAlbum(AlbumModel albumModel)
        {
            var album = _mapper.Map<Album>(albumModel);
            var root = _configuration["DamselflyConfiguration:SourceDirectory"];
            var folderPath = Path.Combine(root, album.UrlName);
            var parentFolder = await _context.Folders.FirstOrDefaultAsync(f => f.ParentId == null);
            var folder = new Folder { Path = folderPath, ParentId = parentFolder!.FolderId };
            var newDirectory = Directory.CreateDirectory(folderPath);
            album.Folder = folder;
            _context.Folders.Add(folder);
            _context.Albums.Add(album);
            await _context.SaveChangesAsync();
            _indexingService.IndexFolder(newDirectory, parentFolder);
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

        public async Task<AlbumModel?> GetAlbum(int id, string? password)
        {
            var album = await AlbumWithImagesQuery().FirstOrDefaultAsync(a => a.AlbumId == id);
            if (album == null) return null;
            album = await CheckPassword(album, password);
            return _mapper.Map<AlbumModel>(album);
        }

        public async Task<AlbumModel?> GetByName(string urlName, string? password)
        {
            var album = await AlbumWithImagesQuery().FirstOrDefaultAsync(a => a.UrlName == urlName);
            if( album == null ) return null;
            album = await CheckPassword(album, password);
            return _mapper.Map<AlbumModel>(album);
        }

        public async Task<List<AlbumModel>> GetAlbums()
        {
            var albums = await _context.Albums.ToListAsync();
            return _mapper.Map<List<AlbumModel>>(albums);
        }

        public async Task<AlbumModel> UnlockAlbum(int id)
        {
            var album = await _context.Albums.FirstOrDefaultAsync(a => a.AlbumId == id);
            if( album == null ) throw new Exception("Album not found");
            album.InvalidPasswordAttempts = 0;
            await _context.SaveChangesAsync();
            return _mapper.Map<AlbumModel>(album);
        }

        private async Task<Album> CheckPassword(Album album, string? password)
        {
            if (album.IsPublic || album.Password == null) return album;
            if (password == null) return null;
            if (album.InvalidPasswordAttempts > 4) return null;
            if (album.Password == password)
            {
                album.InvalidPasswordAttempts = 0;
                await _context.SaveChangesAsync();
                return album;
            }
            album.InvalidPasswordAttempts++;
            await _context.SaveChangesAsync();
            return null;
        }

        private IQueryable<Album> AlbumWithImagesQuery()
        {
            return _context.Albums.Include(a => a.Images).ThenInclude(i => i.MetaData);
        }
    }
}
