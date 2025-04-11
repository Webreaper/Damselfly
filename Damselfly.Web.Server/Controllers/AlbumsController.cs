using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Authentication;
using Damselfly.Core.DbModels.Models.API_Models;
using Damselfly.Core.DbModels.Models.Entities;
using Damselfly.Core.Services;
using Damselfly.Web.Server.CustomAttributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Damselfly.Core.Models;

namespace Damselfly.Web.Server.Controllers
{
    // [Authorize(Policy = PolicyDefinitions.s_IsAdmin)]
    [ApiController]
    [Route("albums")]
    public class AlbumsController(AlbumService albumService) : ControllerBase
    {

        private readonly AlbumService _albumService = albumService;

        [HttpPost]
        [Route("create")]
        [Authorize(Policy = PolicyDefinitions.s_FireBaseAdmin)]
        public async Task<IActionResult> CreateAlbum(AlbumModel albumModel)
        {

            var result = await _albumService.CreateAlbum(albumModel);

            return Ok(result);
        }

        [HttpPost]
        [Route("update")]
        [Authorize(Policy = PolicyDefinitions.s_FireBaseAdmin)]
        public async Task<IActionResult> UpdateAlbum(AlbumModel albumModel)
        {

            var result = await _albumService.UpdateAlbum(albumModel);

            return Ok(result);
        }

        [HttpPost]
        [Route("unlock")]
        [Authorize(Policy = PolicyDefinitions.s_FireBaseAdmin)]
        public async Task<IActionResult> UnlockAlbum(AlbumModel albumModel)
        {
            if (albumModel.AlbumId == null) return BadRequest();
            var result = await _albumService.UnlockAlbum(albumModel.AlbumId.Value);

            return Ok(result);
        }

        [HttpPost]
        [Route("AddExistingImages")]
        [Authorize(Policy = PolicyDefinitions.s_FireBaseAdmin)]
        public async Task<IActionResult> AddExistingImages(AddExistingImagesToAlbumRequest model)
        {
            var result = await _albumService.AddImagesToAlbum(model);

            return Ok(result);
        }

        [HttpGet]
        [Route("all")]
        [Authorize(Policy = PolicyDefinitions.s_FireBaseAdmin)]
        public async Task<IActionResult> GetAllAlbums()
        {

            var result = await _albumService.GetAlbums();

            return Ok(result);
        }

        /// <summary>
        /// Get paginated albums
        /// </summary>
        /// <param name="request">Pagination request containing PageIndex and PageSize</param>
        /// <returns>Paginated result containing albums and pagination metadata</returns>
        [HttpGet]
        [Route("paginated")]
        [Authorize(Policy = PolicyDefinitions.s_FireBaseAdmin)]
        public async Task<ActionResult<PaginationResultModel<AlbumModel>>> GetPaginatedAlbums([FromQuery] AlbumsPaginationRequest request)
        {
            if (request.PageIndex < 0 || request.PageSize < 1 || request.PageSize > 100)
            {
                return BadRequest("PageIndex cannot be less than 0 and PageSize must be between 1 and 100");
            }

            var result = await _albumService.GetAlbumsPaginated(request);
            return Ok(result);
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetAlbum(string id, [FromQuery] string? password)
        {
            if (Guid.TryParse(id, out var guidId))
            {
                var album = await _albumService.GetAlbum(guidId, password);
                if(album == null) return NotFound();
                return Ok(album);
            }
            var result = await _albumService.GetByName(id, password);
            if( result == null ) return NotFound();
            return Ok(result);
        }

        [HttpDelete]
        [Route("{id}")]
        [Authorize(Policy = PolicyDefinitions.s_FireBaseAdmin)]
        public async Task<IActionResult> DeleteAlbum(string id)
        {
            if (Guid.TryParse(id, out var guidId))
            {
                var result = await _albumService.DeleteAlbum(guidId);
                if (result)
                    return Ok(new BooleanResultModel { Result = true });
                return NotFound();
            }
            return BadRequest();
        }

        [HttpPost]
        [Route("QueueScan")]
        [Authorize(Policy = PolicyDefinitions.s_FireBaseAdmin)]
        public IActionResult QueueScan(AlbumModel albumModel)
        {
            if( albumModel.AlbumId == null ) return BadRequest("AlbumId is required");
            var result = AlbumService.QueueAlbumScan(albumModel.AlbumId.Value);

            return Ok(new BooleanResultModel { Result = result });
        }
    }
}
