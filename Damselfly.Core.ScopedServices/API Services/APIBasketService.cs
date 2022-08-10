using System;
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

    public async Task SetBasketState( ICollection<Image> images, bool newState)
	{
		
	}

	public bool IsSelected( Image image )
	{
		return false;
	}
}

