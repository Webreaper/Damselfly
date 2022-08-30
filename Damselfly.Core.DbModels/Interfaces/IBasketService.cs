using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.DbModels;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IBasketService
{
    List<Image> BasketImages { get; }
    Basket CurrentBasket { get; }
    event Action OnBasketChanged;
    Task<Basket> Create(string name, int? userId);
    Task Save(Basket basket);
    Task SetBasketState(ICollection<Image> images, bool newState, Basket basket = null);
    Task<Basket> SwitchBasketById(int basketId);
    Task<Basket> SwitchToDefaultBasket(int? userId);
    Task<ICollection<Basket>> GetUserBaskets(int? userId);
    bool IsSelected(Image image);
    Task Clear( int basketId );
    Task DeleteBasket(int basketId);
}

