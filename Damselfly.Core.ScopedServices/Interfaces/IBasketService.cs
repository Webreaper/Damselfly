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
    Task<Basket> CreateNewBasket(string name, int? userId);
    Task ModifyBasket(Basket basket, string name, int? userId);
    Task SetBasketState(ICollection<Image> images, bool newState, Basket basket = null);
    Task<Basket> SwitchBasketById(int basketId);
    Task<Basket> SwitchToDefaultBasket(AppIdentityUser user);
    Task<ICollection<Basket>> GetUserBaskets(AppIdentityUser user);
    bool IsSelected(Image image);
    Task Clear();
}

