using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.DbModels.Models.APIModels;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IBasketService
{
    Task<Basket> Create(string name, int? userId);
    Task Delete(Guid basketId);
    Task Clear(Guid basketId);
    Task Save(Basket basket);

    Task SetImageBasketState(Guid basketId, bool newState, ICollection<Guid> imageIds);

    Task<ICollection<Basket>> GetUserBaskets(int? userId);

    Task<Basket> GetBasketById(Guid basketId);
    Task<Basket> GetDefaultBasket(int? userId);

    Task<int> CopyImages(Guid sourceBasketId, Guid destBasketId);
}

public interface IUserBasketService : IBasketService
{
    Basket CurrentBasket { get; }
    ICollection<Image> BasketImages { get; }
    event Action<BasketChanged> OnBasketChanged;
    Task<Basket> SwitchToBasket(Guid basketId);
    Task<Basket> SwitchToDefaultBasket(int? userId);
    Task SetImageBasketState(bool newState, ICollection<Guid> imageIds);
    Task<int> CopyImages(Guid destBasketId);
    bool IsInCurrentBasket(Image image);
    Task Clear();
    Task<Basket> Create(string name);
    Task<ICollection<Basket>> GetUserBaskets();
    Task<Basket> SwitchToDefaultBasket();
}