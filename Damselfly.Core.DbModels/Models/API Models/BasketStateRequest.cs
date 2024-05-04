using System;
using System.Collections.Generic;

namespace Damselfly.Core.DbModels.Models.APIModels;

public class BasketStateRequest
{
    public Guid BasketId { get; set; }
    public ICollection<Guid> ImageIds { get; set; }
    public bool NewState { get; set; }
}