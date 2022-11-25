using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IBasketService
{
    Task<Basket> Create(string name, int? userId);
    Task Delete(int basketId);
    Task Clear(int basketId);
    Task Save(Basket basket);

    Task SetImageBasketState(int basketId, bool newState, ICollection<int> imageIds);

    Task<ICollection<Basket>> GetUserBaskets(int? userId);

    Task<Basket> GetBasketById(int basketId);
    Task<Basket> GetDefaultBasket(int? userId);

    Task<int> CopyImages(int sourceBasketId, int destBasketId);
}

public interface IUserBasketService : IBasketService
{
    Basket CurrentBasket { get; }
    ICollection<Image> BasketImages { get; }
    event Action<BasketChanged> OnBasketChanged;
    Task<Basket> SwitchToBasket(int basketId);
    Task<Basket> SwitchToDefaultBasket(int? userId);
    Task SetImageBasketState(bool newState, ICollection<int> imageIds);
    Task<int> CopyImages(int destBasketId);
    bool IsInCurrentBasket(Image image);
    Task Clear();
    Task<Basket> Create(string name);
    Task<ICollection<Basket>> GetUserBaskets();
    Task<Basket> SwitchToDefaultBasket();
}