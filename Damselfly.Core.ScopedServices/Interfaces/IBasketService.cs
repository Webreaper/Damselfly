using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IBasketService
{
    List<Image> BasketImages { get; }
    event Action OnBasketChanged;
    Task SetBasketState(ICollection<Image> images, bool newState);
    bool IsSelected(Image image);
}

