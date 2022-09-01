using System;
using System.Collections.Generic;

namespace Damselfly.Core.DbModels.Models.APIModels;

public class BasketCreateRequest
{
    public string Name { get; set; }
    public int? UserId { get; set; }
}

