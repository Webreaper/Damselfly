using Damselfly.Core.DbModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices;
using Damselfly.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using MudBlazor;
using Route = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace Damselfly.Web.Server.Controllers;

// TODO: WASM: [Authorize]
[ApiController]
[Route("/api/basket")]
public class BasketController : ControllerBase
{
    private readonly BasketService _service;

    private readonly ILogger<BasketController> _logger;

    public BasketController(BasketService service, ILogger<BasketController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpDelete("/api/basket/{basketId}")]
    public async Task DeleteBasket( int basketId)
    {
        _service.DeleteBasket(basketId);
    }

    [HttpGet("/api/baskets/{userId}")]
    public async Task<ICollection<Basket>> GetUserBaskets(int userId)
    {
        return await _service.GetUserBaskets(userId);
    }

    [HttpGet("/api/baskets")]
    public async Task<ICollection<Basket>> GetUserBaskets()
    {
        return await _service.GetUserBaskets(null);
    }
}

