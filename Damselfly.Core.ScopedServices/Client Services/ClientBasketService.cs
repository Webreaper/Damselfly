using System;
using Damselfly.Core.Constants;
using System.Net.Http.Json;
using Damselfly.Core.DbModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;
using Damselfly.Core.ScopedServices.ClientServices;
using Damselfly.Core.DbModels.Models.APIModels;

namespace Damselfly.Core.ScopedServices;

public class ClientBasketService : IBasketService
{
    protected ILogger<ClientBasketService> _logger;
    private readonly RestClient httpClient;
    private readonly NotificationsService _notifications;

    public ClientBasketService(RestClient client, NotificationsService notifications, ILogger<ClientBasketService> logger) 
    {
        httpClient = client;
        _notifications = notifications;
        _logger = logger;

        _notifications.SubscribeToNotification(NotificationType.BasketChanged, HandleServerBasketChange);
    }

    private void HandleServerBasketChange()
    {
        OnBasketChanged?.Invoke();
    }

    /// <summary>
    /// The list of selected images in the basket
    /// </summary>
    public List<Image> BasketImages { get; private set; } = new List<Image>();

    // WASM TODO
    public event Action OnBasketChanged;

    public Basket CurrentBasket { get; private set;  }

    public void ChangeBasket( Basket newBasket )
    {
        _logger.LogInformation($"Loaded basket {newBasket.Name}...");
        CurrentBasket = newBasket;

        BasketImages.Clear();
        if( newBasket.BasketEntries is not null && newBasket.BasketEntries.Any() )
        {
            BasketImages.AddRange(newBasket.BasketEntries.Select(x => x.Image));
            _logger.LogInformation($"Added {BasketImages.Count()} basket images.");
        }

        OnBasketChanged?.Invoke();
    }

    public bool IsSelected(Image image)
    {
        return BasketImages.Any(x => x.ImageId == image.ImageId);
    }

    public async Task Clear( int basketId )
    {
        await httpClient.CustomGetFromJsonAsync<Basket>($"/api/basket/clear/{basketId}");
    }

    public async Task DeleteBasket(int basketId)
    {
        await httpClient.CustomDeleteAsync($"/api/basket/{basketId}");
    }

    public async Task<Basket> SwitchBasketById(int basketId)
    {
        Console.WriteLine($"Calling SwitchBasketById: {basketId}");
        try
        {
            var basket = await httpClient.CustomGetFromJsonAsync<Basket>($"/api/basket/{basketId}");
            ChangeBasket(basket);
            return basket;
        }
        catch ( Exception ex )
        {
            _logger.LogError($"Exception: {ex}");
            throw;
        }
    }

    public async Task<Basket> SwitchToDefaultBasket(int? userId)
    {
        Basket basket;

        if( userId is null )
            basket = await httpClient.CustomGetFromJsonAsync<Basket>($"/api/basketdefault");
        else
            basket = await httpClient.CustomGetFromJsonAsync<Basket>($"/api/basketdefault/{userId}");

        ChangeBasket(basket);

        return basket;
    }

    public async Task SetBasketState( ICollection<int> images, bool newState, int? basketId = null)
	{
        if (basketId == null)
            basketId = CurrentBasket?.BasketId;

        if (basketId is null)
            throw new ArgumentException("A basket ID must be specified");

        var payload = new BasketStateRequest {
                    BasketId = basketId.Value,
                    ImageIds = images,
                    NewState = newState
                };
        await httpClient.CustomPostAsJsonAsync($"/api/basketimage/state",  payload );

        OnBasketChanged?.Invoke();
    }

    public async Task<Basket> Create(string name, int? userId)
    {
        var payload = new BasketCreateRequest { Name = name, UserId = userId };
        return await httpClient.CustomPostAsJsonAsync<BasketCreateRequest, Basket>($"/api/basket", payload);
    }

    public async Task Save(Basket basket)
    {
        var response = await httpClient.CustomPutAsJsonAsync<Basket>($"/api/basket", basket);
    }

    public async Task<ICollection<Basket>> GetUserBaskets(int? userId)
    {
        try
        {
            return await httpClient.CustomGetFromJsonAsync<ICollection<Basket>>("/api/baskets/");
        }
        catch( Exception ex )
        {
            _logger.LogError($"Error Retrieving Baskets: {ex}");
        }

        return new List<Basket>();
    }
}

