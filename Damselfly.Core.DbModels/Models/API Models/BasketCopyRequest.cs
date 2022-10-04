using System;
using System.Collections.Generic;

namespace Damselfly.Core.DbModels.Models.APIModels;

public class BasketCopyRequest
{
    public int SourceBasketId { get; set; }
    public int DestBasketId { get; set; }
    public bool Move { get; set; }
}

