using System.Threading.Tasks;
using Damselfly.Core.DbModels.Models;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface ISystemSettingsService
{
    Task<SystemConfigSettings> GetSystemSettings();
    Task SaveSystemSettings(SystemConfigSettings systemSettings);
}