using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.Interfaces
{
    public interface ICacheService
    {
        Task<string> GetAsync(string key);
        Task SetAsync(string key, string value, TimeSpan expirationTimeSpan);
    }
}
