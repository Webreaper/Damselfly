using System;
using System.IO;
using System.Threading.Tasks;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.Extensions.Logging;

namespace Damselfly.Core.Services;

public class FileService : IFileService
{
    private readonly ILogger<FileService> _logger;
    private readonly IStatusService _statusService;
    private readonly IImageCacheService _imageCache;
    private readonly IFolderService _folderService;

    public FileService( IStatusService statusService, IImageCacheService imageCacheService, IFolderService folderService, ILogger<FileService> logger )
    {
        _statusService = statusService;
        _folderService = folderService;
        _imageCache = imageCacheService;
        _logger = logger;
    }

    public async Task<bool> MoveImages(ImageMoveRequest req)
    {
        bool success = true;
        var images = await _imageCache.GetCachedImages( req.ImageIDs );

        foreach( var image in images )
        {
            var source = image.FullPath;
            var dest = Path.Combine( req.Destination.Path, image.FileName );

            if( File.Exists( source ) && !File.Exists( dest ) )
            {
                try
                {
                    // Note, we *never* overwrite.
                    if( req.Move )
                    {
                        File.Move( source, dest, false );
                        _statusService.UpdateStatus( $"Moved {image.FileName} to {req.Destination.Path}" );
                    }
                    else
                    {
                        File.Copy( source, dest, false );
                        _statusService.UpdateStatus( $"Copied {image.FileName} to {req.Destination.Path}" );
                    }
                }
                catch( Exception ex )
                {
                    _logger.LogError( $"Unable to move file to {dest}: {ex}" );
                    success = false;
                }
            }
        }

        return success;
    }
}

