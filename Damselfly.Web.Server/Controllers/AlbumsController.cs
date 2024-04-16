using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Models.API_Models;
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
        [Route("get/{id}/{password}")]
        public async Task<IActionResult> GetAlbumWithPassword(int id, string? password)
        {

            var result = await _albumService.GetAlbum(id, password);

            return Ok(result);
        }

        [HttpGet]
        [Route("get/{id}")]
        public async Task<IActionResult> GetAlbum(int id)
        {

            var result = await _albumService.GetAlbum(id, null);

            return Ok(result);
        }
    }
}
