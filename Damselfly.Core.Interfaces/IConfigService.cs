using System;

namespace Damselfly.Core.Interfaces
{
    public interface IConfigService
    {
        void Set(string name, string valuel);
        string Get(string name, string defaultIfNotExists = null);
        EnumType Get<EnumType>(string name, EnumType defaultIfNotExists = default) where EnumType : struct;
        bool GetBool(string name, bool defaultIfNotExists = default);
        int GetInt(string name, int defaultIfNotExists = default);
    }
}
