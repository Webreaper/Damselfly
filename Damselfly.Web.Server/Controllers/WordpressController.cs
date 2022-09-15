using Damselfly.Core.Models;
using Damselfly.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Route = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace Damselfly.Web.Server.Controllers;

// TODO: WASM: [Authorize]
[ApiController]
[Microsoft.AspNetCore.Mvc.Route("/api/wordpress")]
public class WordpressController : ControllerBase
{
    private readonly ILogger<WordpressController> _logger;
    private readonly WordpressService _service;

    public WordpressController(WordpressService service, ILogger<WordpressController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("/api/wordpress/")]
    public async Task Put(List<Image> images)
    {
        await _service.UploadImagesToWordpress(images);
    }
}