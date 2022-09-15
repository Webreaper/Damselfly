using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Route = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace Damselfly.Web.Server.Controllers;

// [Authorize]
[ApiController]
[Microsoft.AspNetCore.Mvc.Route("/api/basket")]
public class BasketController : ControllerBase
{
    private readonly ILogger<BasketController> _logger;
    private readonly IBasketService _service;

    public BasketController(BasketService service, ILogger<BasketController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpDelete("/api/basket/{basketId}")]
    public async Task DeleteBasket(int basketId)
    {
        await _service.Delete(basketId);
    }

    [HttpPost("/api/basket")]
    public async Task<Basket> SetBasketState(BasketCreateRequest req)
    {
        return await _service.Create(req.Name, req.UserId);
    }

    [HttpGet("/api/basket/entries/{basketId}")]
    public async Task<ICollection<BasketEntry>> GetBasketEntries(int basketId)
    {
        var basket = await _service.GetBasketById(basketId);

        var entries = basket.BasketEntries.ToList();

        return entries;
    }

    [HttpPost("/api/basket/clear/{basketId}")]
    public async Task ClearBasket(int basketId)
    {
        await _service.Clear(basketId);
    }

    [HttpGet("/api/basket/{basketId}")]
    public async Task<Basket> GetBasketById(int basketId)
    {
        var basket = await _service.GetBasketById(basketId);

        if ( basket == null )
            throw new ArgumentException("No such basket!");

        foreach ( var be in basket.BasketEntries )
            be.Image = null;

        return basket;
    }

    [HttpGet("/api/baskets/{userId}")]
    public async Task<ICollection<Basket>> GetUserBaskets(int userId)
    {
        return await _service.GetUserBaskets(userId);
    }

    [HttpGet("/api/basketdefault/{userId}")]
    public async Task<Basket> GetDefaultUserBasket(int userId)
    {
        return await _service.GetDefaultBasket(userId);
    }

    [HttpGet("/api/basketdefault")]
    public async Task<Basket> GetDefaultUserBasket()
    {
        return await _service.GetDefaultBasket(null);
    }


    [HttpGet("/api/baskets")]
    public async Task<ICollection<Basket>> GetUserBaskets()
    {
        return await _service.GetUserBaskets(null);
    }

    [HttpPost("/api/basketimage/state")]
    public async Task SetBasketState(BasketStateRequest req)
    {
        await _service.SetImageBasketState(req.BasketId, req.NewState, req.ImageIds);
    }
}