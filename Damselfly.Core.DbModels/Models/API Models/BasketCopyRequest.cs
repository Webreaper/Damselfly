using System;
using System.Collections.Generic;

namespace Damselfly.Core.DbModels.Models.APIModels;

public class BasketCopyRequest
{
    public Guid SourceBasketId { get; set; }
    public Guid DestBasketId { get; set; }
    public bool Move { get; set; }
}

