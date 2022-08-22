using System;
using Damselfly.Core.DbModels;
using Damselfly.Core.DbModels.Models;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IStatusService
{
    void UpdateStatus(string newStatus);
    void UpdateUserStatus(string newStatus);
    event Action<string> OnStatusChanged;
}