using System;
using System.Threading.Tasks;
using Damselfly.Core.Constants;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IWorkService
{
    Task<ServiceStatus> GetWorkStatus();
    Task Pause( bool paused );
    event Action<ServiceStatus> OnStatusChanged;
}

