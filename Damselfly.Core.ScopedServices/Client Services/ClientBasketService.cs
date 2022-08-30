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

namespace Damselfly.Core.ScopedServices;

public class ClientBasketService : IBasketService
{
    protected ILogger<ClientBasketService> _logger;
    private readonly RestClient httpClient;

    public ClientBasketService(RestClient client, ILogger<ClientBasketService> logger) 
    {
        httpClient = client;
        _logger = logger;
    }

    /// <summary>
    /// The list of selected images in the basket
    /// </summary>
    public List<Image> BasketImages { get; private set; } = new List<Image>();

    // WASM TODO
    public event Action OnBasketChanged;

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
        return await httpClient.CustomGetFromJsonAsync<Basket>($"/api/basket/{basketId}");
    }

    public async Task<Basket> SwitchToDefaultBasket(int? userId)
    {
        return await httpClient.CustomGetFromJsonAsync<Basket>($"/api/basket/default/{userId}");
    }

    public async Task SetBasketState( ICollection<Image> images, bool newState, Basket basket = null)
	{
        if (basket == null)
            basket = CurrentBasket;

        await httpClient.CustomGetFromJsonAsync<ServiceStatus>($"/api/basket/state/{newState}");
    }

    public Basket CurrentBasket { get; }

	public bool IsSelected( Image image )
	{
		return false;
	}

    public async Task<Basket> Create(string name, int? userId)
    {
        var basket = new Basket { Name = name, UserId = userId };
        return await httpClient.CustomPostAsJsonAsync<Basket, Basket>($"/api/basket", basket);
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

