using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices.Interfaces;

public interface IConfigService
{
    void Set(string name, string value);
    string Get(string name, string? defaultIfNotExists = null);
    EnumType Get<EnumType>(string name, EnumType defaultIfNotExists = default) where EnumType : struct;
    bool GetBool(string name, bool defaultIfNotExists = default);
    int GetInt(string name, int defaultIfNotExists = default);
}

public interface IUserConfigService : IConfigService
{
    Task SetForUser(string name, string value);
    event Action<ICollection<ConfigSetting>> OnSettingsLoaded;
}