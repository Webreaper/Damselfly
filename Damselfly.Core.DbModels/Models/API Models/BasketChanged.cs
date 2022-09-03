using System;
using System.Collections.Generic;
using Damselfly.Core.Constants;

namespace Damselfly.Core.DbModels.Models.APIModels;

public class BasketChanged
{
    public int BasketId { get; set; }
    public BasketChangeType ChangeType { get; set; }
    public ICollection<int> BasketEntries { get; set; }
}

