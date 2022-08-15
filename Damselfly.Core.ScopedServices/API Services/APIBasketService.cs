using System;
using Damselfly.Core.DbModels;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;

namespace Damselfly.Core.ScopedServices;

public class APIBasketService : BaseClientService, IBasketService
{
	public APIBasketService(HttpClient client) : base(client) { }

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

        throw new NotImplementedException();
	}

	public Basket CurrentBasket { get; }

	public bool IsSelected( Image image )
	{
		return false;
	}

    public async Task<Basket> Create(string name, int? userId)
    {
        throw new NotImplementedException();
    }

    public async Task Save(Basket basket, string name, int? userId)
    {
        throw new NotImplementedException();
    }

    public async Task<ICollection<Basket>> GetUserBaskets(AppIdentityUser user)
    {
        throw new NotImplementedException();
    }
}

