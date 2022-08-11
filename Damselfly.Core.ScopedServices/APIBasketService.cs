using System;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices;

public class APIBasketService
{
    /// <summary>
    /// The list of selected images in the basket
    /// </summary>
    public List<Image> BasketImages { get; private set; } = new List<Image>();

	public async Task SetBasketState( ICollection<Image> images, bool newState)
	{
		
	}

	public bool IsSelected( Image image )
	{
		return false;
	}
}

