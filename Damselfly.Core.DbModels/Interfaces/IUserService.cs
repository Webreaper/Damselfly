using System;
using System.Threading.Tasks;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IUserService
{
    int? UserId { get; }
    bool RolesEnabled { get; }
    public event Action<int?> OnUserIdChanged;
    Task<bool> PolicyApplies(string policy);
}