using System;
using System.Threading.Tasks;
using Damselfly.Core.Constants;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IWorkService
{
    Task<ServiceStatus> GetWorkStatus();
    Task Pause(bool paused);
    event Action<ServiceStatus> OnStatusChanged;

    Task<CPULevelSettings> GetCPUSchedule();
    Task SetCPUSchedule(CPULevelSettings settings);
}