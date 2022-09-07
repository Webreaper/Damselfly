using Damselfly.Core.DbModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices;
using Damselfly.Core.Services;
using MailKit;
using MailKit.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Route = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace Damselfly.Web.Server.Controllers;

// TODO: WASM: [Authorize]
[ApiController]
[Route("/api/wordpress")]
public class WordpressController : ControllerBase
{
    private readonly WordpressService _service;

    private readonly ILogger<WordpressController> _logger;

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

