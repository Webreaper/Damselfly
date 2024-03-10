using System;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface ISearchHint
{
    Action Clear { get; set; }
    string Description { get; set; }
}