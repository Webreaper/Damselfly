using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Models.API_Models;
using Damselfly.Core.DbModels.Models.Entities;
using Damselfly.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> CreateAlbum(AlbumModel albumModel)
        {

            var result = await _albumService.CreateAlbum(albumModel);

            return Ok(result);
        }

        [HttpPost]
        [Route("update")]
        public async Task<IActionResult> UpdateAlbum(AlbumModel albumModel)
        {

            var result = await _albumService.UpdateAlbum(albumModel);

            return Ok(result);
        }

        [HttpGet]
        [Route("all")]
        public async Task<IActionResult> GetAllAlbums()
        {

            var result = await _albumService.GetAlbums();

            return Ok(result);
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetAlbum(string id, [FromQuery] string? password)
        {
            if (int.TryParse(id, out var numId))
            {
                var album = await _albumService.GetAlbum(numId, password);
                if(album == null) return NotFound();
                return Ok(album);
            }
            var result = await _albumService.GetByName(id, password);
            if( result == null ) return NotFound();
            return Ok(result);
        }
    }
}
