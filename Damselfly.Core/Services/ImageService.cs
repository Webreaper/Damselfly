using AutoMapper;
using Damselfly.Core.Database;
using Damselfly.Core.DbModels.Authentication;
using Damselfly.Core.DbModels.Models.API_Models;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.DbModels.Models.Entities;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.Services
{
    public class ImageService(ThumbnailService thumbnailService, 
        ImageContext imageContext, 
        FileService fileService, 
        IConfiguration configuration, 
        IMapper mapper, 
        MetaDataService metaDataService, 
        IAuthService authService)
    {
        private readonly ThumbnailService _thumbnailService = thumbnailService;
        private readonly ImageContext _context = imageContext;
        private readonly FileService _fileService = fileService;
        private readonly MetaDataService _metaDataService = metaDataService;
        private readonly IConfiguration _configuration = configuration;
        private readonly IMapper _mapper = mapper;
        private readonly IAuthService _authService = authService;


        public async Task<List<ImageModel>> CreateImages(UploadImageRequest uploadImageRequest)
        {
            try
            {
                var album = _context.Albums.Include(a => a.Folder).FirstOrDefault(a => a.AlbumId == uploadImageRequest.AlbumId);
                if( album == null )
                {
                    throw new Exception("Album not found");
                }
                var images = new List<Image>();
                foreach( var imageFile in uploadImageRequest.ImageFiles )
                {
                    try
                    {
                        var imagePath = Path.Combine(album.Folder.Path, imageFile.FileName);
                        using( var memoryStream = new MemoryStream() )
                        {
                            await imageFile.OpenReadStream().CopyToAsync(memoryStream);
                            await File.WriteAllBytesAsync(imagePath, memoryStream.ToArray());
                        }
                        var fileInfo = new FileInfo(imagePath);
                        var image = new Image { FileName = imageFile.FileName, SortDate = DateTime.UtcNow, FolderId = album.FolderId, FileCreationDate = DateTime.UtcNow, FileLastModDate = DateTime.UtcNow, FileSizeBytes = (int)fileInfo.Length };
                        album.Images.Add(image);
                        //image.Albums.Add(album);
                        _context.Images.Add(image);
                        _context.Albums.Update(album);
                        images.Add(image);
                    }
                    catch( Exception ex )
                    {
                        Logging.LogError("Error creating image {fileName}, {exception}", imageFile.FileName, ex.ToString());
                        await AttemptCleanUpForFailedUpload(imageFile, uploadImageRequest.AlbumId);
                    }
                }
                await _context.SaveChangesAsync();
                foreach( var image in images )
                {
                    try
                    {
                        await _metaDataService.ScanMetaData(image.ImageId);
                        await _thumbnailService.CreateThumb(image.ImageId, Constants.ThumbSize.Large);
                        await _thumbnailService.CreateThumb(image.ImageId, Constants.ThumbSize.Medium);
                        await _thumbnailService.CreateThumb(image.ImageId, Constants.ThumbSize.Small);
                    }
                    catch( Exception ex )
                    {
                        Logging.LogError("Error creating thumbs for image {imageId}, {exception}", image.ImageId, ex.ToString());
                    }
                }
                var imageIds = images.Select(i => i.ImageId).ToList();
                var dbImages = await _context.Images.Include(i => i.MetaData).Where(i => imageIds.Contains(i.ImageId)).ToListAsync();
                return dbImages.Select(_mapper.Map<ImageModel>).ToList();
            }
            catch(Exception ex)
            {
                throw new Exception("Error creating images", ex);
            }
            
        }

        private async Task AttemptCleanUpForFailedUpload(IFormFile file, Guid albumId)
        {
            try
            {
                var album = await _context.Albums.Include(a => a.Folder).FirstOrDefaultAsync(a => a.AlbumId == albumId);
                if( album == null )
                {
                    return;
                }
                var imagePath = Path.Combine(album.Folder.Path, file.FileName);
                var applicabaleImage = await _context.Images.FirstOrDefaultAsync(i => i.FileName == file.FileName && i.Albums.Any(a => a.AlbumId == albumId));
                if( applicabaleImage != null )
                {
                    _context.Images.Remove(applicabaleImage);
                }
                File.Delete(imagePath);
            }
            catch( Exception ex )
            {
                Logging.LogError("Error cleaning up failed upload {fileName}, {exception}", file.FileName, ex.ToString());
            }
        }

        public async Task<ImageModel> GetImageData(Guid id, string password)
        {
            var image = await _context.Images.Include(i => i.MetaData).FirstOrDefaultAsync(i => i.ImageId == id);

            if( image == null )
            {
                return null;
            }
            if(await CheckPassword(image.ImageId, password))
            {
                return _mapper.Map<ImageModel>(image);
            }
            return null;
        }

        public async Task<bool> DeleteImage(Guid id)
        {
            var image = await _context.Images.Include(i => i.Folder).FirstOrDefaultAsync(i => i.ImageId == id);
            if( image == null )
            {
                return false;
            }
            var result = await _fileService.DeleteImages(new MultiImageRequest { ImageIDs = new List<Guid> { id } });
            _context.Images.Remove(image);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CanDownload(Guid imageId, string password)
        {
            var image = await _context.Images.Include(i => i.Albums).FirstOrDefaultAsync(i => i.ImageId == imageId);
            if( image == null )
            {
                return false;
            }
            if( image.Albums.Any(a => a.Password != null && a.Password == password && a.InvalidPasswordAttempts < Album.MaxInvalidPasswordAttempts) )
            {
                return true;
            }
            var isAdmin = await _authService.CheckCurrentFirebaseUserIsInRole([RoleDefinitions.s_AdminRole]);
            if( isAdmin )
            {
                return true;
            }
            var albumIds = image.Albums.Select(a => a.AlbumId).ToList();
            await _context.Albums.Where(a => albumIds.Contains(a.AlbumId)).ExecuteUpdateAsync(a => a.SetProperty(a => a.InvalidPasswordAttempts, a => a.InvalidPasswordAttempts + 1));
            return false;
        }

        public async Task<bool> CheckPassword(Guid imageId, string password)
        {
            try
            {
                var image = await _context.Images.Include(i => i.Albums).FirstOrDefaultAsync(i => i.ImageId == imageId);
                Logging.LogTrace("Checking password for image {imageId}", imageId);
                if( image == null )
                {
                    Logging.Log("Image not found for {imageId}", imageId);
                    return false;
                }
                Logging.LogTrace("for posterity");
                Logging.LogTrace("Image found for {imageId}", imageId);
                Logging.LogTrace("{albumCount} Albums found for image {imageId}", image.Albums.Count, imageId);
                Logging.LogTrace("Applicable albums: {applicableAlbums}", image.Albums.Select(a => a.AlbumId));
                Logging.LogTrace("The password is {password}", image.Albums.First().Password);
                if( image.Albums.Any(a => (a.IsPublic || a.Password == null || a.Password == "" || a.Password == password) && a.InvalidPasswordAttempts < Album.MaxInvalidPasswordAttempts) )
                {
                    return true;
                }
                Logging.LogTrace("Password check failed for image {imageId}", imageId);
                var isAdmin = await _authService.CheckCurrentFirebaseUserIsInRole([RoleDefinitions.s_AdminRole]);
                if( isAdmin )
                {
                    return true;
                }
                var albumIds = image.Albums.Select(a => a.AlbumId).ToList();
                await _context.Albums.Where(a => albumIds.Contains(a.AlbumId)).ExecuteUpdateAsync(a => a.SetProperty(a => a.InvalidPasswordAttempts, a => a.InvalidPasswordAttempts + 1));
                return false;
            }
            catch( Exception ex )
            {
                Logging.LogError("Error checking password for image {imageId}, {exception}", imageId, ex.ToString());
                return false;
            }
        }

    }
}
