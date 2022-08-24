using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.Constants;
using Damselfly.Core.DbModels.Models;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.Interfaces
{
    public interface ISystemSettingsService
    {
        Task<SystemConfigSettings> GetSystemSettings();
        Task SaveSystemSettings(SystemConfigSettings systemSettings);
    }
}
