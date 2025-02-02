using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Damselfly.Core.Interfaces
{
    public interface IIpOriginService
    {
        Task<string> GetIpOrigin(string ipAddress);
    }
}
