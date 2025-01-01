using System;
using System.IO;
using System.Threading.Tasks;
using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.Services;

public class FileService : IFileService
{
    private readonly ILogger<FileService> _logger;
    private readonly IStatusService _statusService;
    private readonly IImageCacheService _imageCache;
    private readonly IConfigService _configService;
    private readonly ICachedDataService _cachedDataService;

    public FileService( IStatusService statusService, IImageCacheService imageCacheService,
        ICachedDataService cachedDataService, IConfigService configService,
        ILogger<FileService> logger )
    {
        _statusService = statusService;
        _imageCache = imageCacheService;
        _cachedDataService = cachedDataService;
        _configService = configService;
        _logger = logger;
    }

    /// <summary>
    /// Delete images
    /// TODO: Based on a config setting we should either have
    /// 1. Move to Damselfly Trashcan
    /// 2. Move to OS trashcan
    /// 3. Actually delete the actual file
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    public async Task<bool> DeleteImages( MultiImageRequest req )
    {
        var trashFolder = _configService.Get( ConfigSettings.TrashcanFolderName, "DamselflyTrashcan" );

        // TODO - allow users to configure the delete folder name
        var destfolder = Path.Combine( _cachedDataService.ImagesRootFolder, trashFolder );
        var success = true;
        var images = await _imageCache.GetCachedImages( req.ImageIDs );

        if( !Directory.Exists( destfolder ) )
            try
            {
                // Store the setting
                await _configService.Set( ConfigSettings.TrashcanFolderName, trashFolder );

                var dir = Directory.CreateDirectory( destfolder );
                // Hide this here?
                // dir.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
                _logger.LogInformation( $"Created trashcan folder: {destfolder}" );
            }
            catch( Exception ex )
            {
                _logger.LogError( $"Unable to create folder {destfolder}: {ex}" );
                return false;
            }

        foreach( var image in images )
        {
            var dest = Path.Combine( destfolder, image.FileName );

            if( File.Exists( dest ) )
                // If there's a collision, create a unique filename
                dest = GetUniqueFilename( dest );

            if( !SafeCopyOrMove( image, dest, true ) )
                success = false;
        }

        return success;
    }


    /// <summary>
    /// Create a unique filename for the given filename
    /// </summary>
    /// <param name="filename">A full filename, e.g., C:\temp\myfile.tmp</param>
    /// <returns>A filename like C:\temp\myfile_633822247336197902.tmp</returns>
    public string GetUniqueFilename( string filename )
    {
        var basename = Path.Combine( Path.GetDirectoryName( filename ),
            Path.GetFileNameWithoutExtension( filename ) );
        var uniquefilename = string.Format( "{0}_{1}{2}",
            basename,
            DateTime.Now.Ticks,
            Path.GetExtension( filename ) );
        return uniquefilename;
    }

    /// <summary>
    /// Move or copy images to a different location
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    public async Task<bool> MoveImages(ImageMoveRequest req)
    {
        var success = true;
        var images = await _imageCache.GetCachedImages( req.ImageIDs );

        foreach( var image in images )
        {
            var dest = Path.Combine(req.Destination.Path, image.FileName);

            if( !SafeCopyOrMove( image, dest, req.Move ) )
                success = false;
        }

        return success;
    }

    private bool SafeCopyOrMove(Image image, string destFilename, bool move)
    {
        var source = image.FullPath;

        if( File.Exists(source) && !File.Exists( destFilename ) )
            try
            {
                // Note, we *never* overwrite.
                if( move )
                {
                    File.Move(source, destFilename, false);
                    _statusService.UpdateStatus($"Moved {image.FileName} to {destFilename}");
                }
                else
                {
                    File.Copy(source, destFilename, false);
                    _statusService.UpdateStatus($"Copied {image.FileName} to {destFilename}");
                }

                return true;
            }
            catch( Exception ex )
            {
                _logger.LogError($"Unable to move file to {destFilename}: {ex}");
            }

        return false;
    }
}