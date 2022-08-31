using Damselfly.Core.DbModels;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Route = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace Damselfly.Web.Server.Controllers;

// [Authorize]
[ApiController]
[Route("/api/basket")]
public class BasketController : ControllerBase
{
    private readonly IBasketService _service;

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

    [HttpGet("/api/basket/default/{userId}")]
    public async Task<Basket> SwitchToDefaultUserBasket(int userId)
    {
        return await _service.SwitchToDefaultBasket(userId);
    }

    [HttpGet("/api/basket/default")]
    public async Task<Basket> SwitchToDefaultBasket()
    {
        return await _service.SwitchToDefaultBasket(-1);
    }

    [HttpGet("/api/baskets")]
    public async Task<ICollection<Basket>> GetUserBaskets()
    {
        return await _service.GetUserBaskets(null);
    }

    [HttpPost("/api/basket/state")]
    public async Task SetBasketState( BasketStateRequest req )
    {
        await _service.SetBasketState(req.ImageIds, req.NewState, req.BasketId );
    }
}

