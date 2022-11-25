using Damselfly.Core.Constants;

namespace Damselfly.Core.DbModels.Models.APIModels;

public class BasketChanged
{
    public int BasketId { get; set; }
    public BasketChangeType ChangeType { get; set; }
}