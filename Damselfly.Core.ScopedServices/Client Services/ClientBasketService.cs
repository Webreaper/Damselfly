using System;
using Damselfly.Core.Constants;
using System.Net.Http.Json;
using Damselfly.Core.DbModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace Damselfly.Core.ScopedServices;

public class ClientBasketService : BaseClientService, IBasketService
{
    protected ILogger<ClientBasketService> _logger;

    public ClientBasketService(HttpClient client, ILogger<ClientBasketService> logger) : base( client )
    {
        httpClient = client;
        _logger = logger;
    }

    /// <summary>
    /// The list of selected images in the basket
    /// </summary>
    public List<Image> BasketImages { get; private set; } = new List<Image>();

    public event Action OnBasketChanged;

    public async Task Clear( int basketId )
    {
    }

    public async Task DeleteBasket(int basketId)
    {
    }

    public async Task<Basket> SwitchBasketById(int basketId)
    {
        return null;
    }

    public async Task<Basket> SwitchToDefaultBasket(AppIdentityUser user)
    {
        return null;
    }

    public async Task SetBasketState( ICollection<Image> images, bool newState, Basket basket = null)
	{
        if (basket == null)
            basket = CurrentBasket;

        await httpClient.GetFromJsonAsync<ServiceStatus>($"/api/basket/state/{newState}");
    }

    public Basket CurrentBasket { get; }

	public bool IsSelected( Image image )
	{
		return false;
	}

    public async Task<Basket> Create(string name, int? userId)
    {
        var basket = new Basket { Name = name, UserId = userId };
        var response = await httpClient.PutAsJsonAsync<Basket>($"/api/basket", basket);
        return await response.Content.ReadFromJsonAsync<Basket>();
    }

    public async Task Save(Basket basket)
    {
        var response = await httpClient.PutAsJsonAsync<Basket>($"/api/basket", basket);
    }

    public async Task<ICollection<Basket>> GetUserBaskets(int? userId)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<ICollection<Basket>>("/api/baskets/");
        }
        catch( Exception ex )
        {
            _logger.LogError($"Error Retrieving Baskets: {ex}");
        }

        return new List<Basket>();
    }
}

