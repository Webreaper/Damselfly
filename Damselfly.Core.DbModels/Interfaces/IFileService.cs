using System.Threading.Tasks;
using Damselfly.Core.DbModels.Models.APIModels;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IFileService
{
    public Task<bool> MoveImages( ImageMoveRequest req );
    public Task<bool> DeleteImages( MultiImageRequest req, bool actuallyDeleteImage = false );
}

