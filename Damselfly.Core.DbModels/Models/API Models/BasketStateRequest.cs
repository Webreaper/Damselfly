using System.Collections.Generic;

namespace Damselfly.Core.DbModels.Models.APIModels;

public class BasketStateRequest
{
    public int BasketId { get; set; }
    public ICollection<int> ImageIds { get; set; }
    public bool NewState { get; set; }
}