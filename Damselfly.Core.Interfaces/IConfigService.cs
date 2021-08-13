using System;

namespace Damselfly.Core.Interfaces
{
    public interface IConfigService
    {
        void Set(string name, string value, IDamselflyUser user = null);
        string Get(string name, string defaultIfNotExists = null, IDamselflyUser user = null);
        EnumType Get<EnumType>(string name, EnumType defaultIfNotExists = default, IDamselflyUser user = null) where EnumType : struct;
        bool GetBool(string name, bool defaultIfNotExists = default, IDamselflyUser user = null);
        int GetInt(string name, int defaultIfNotExists = default, IDamselflyUser user = null);
    }
}
