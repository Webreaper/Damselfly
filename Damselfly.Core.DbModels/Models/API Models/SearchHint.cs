using System;
using Damselfly.Core.ScopedServices.Interfaces;

namespace Damselfly.Core.DbModels.Models.API_Models;

public class SearchHint : ISearchHint
{
    public Action Clear { get; set; }
    public string Description { get; set; }
}