using System;
using System.Linq;
using Damselfly.Core.DbModels;
using Damselfly.Core.Interfaces;
using Damselfly.Core.Models;
using Damselfly.Web.Utils;

namespace Damselfly.Core.ScopedServices;

// TODO: Write values to the back-end service
public class ClientConfigService : BaseConfigService, IConfigService
{
    public override void InitialiseCache()
    {
    }
}
