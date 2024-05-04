using Damselfly.Core.Constants;
using System;

namespace Damselfly.Core.DbModels.Models.APIModels;

public class BasketChanged
{
    public Guid BasketId { get; set; }
    public BasketChangeType ChangeType { get; set; }
}