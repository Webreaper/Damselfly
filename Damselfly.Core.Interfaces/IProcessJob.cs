using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Damselfly.Core.Interfaces
{
    public interface IProcessJob
    {
        Task Process();
        bool CanProcess { get; }
        string Description { get; }
    }

    public interface IProcessJobFactory
    {
        Task<ICollection<IProcessJob>> GetPendingJobs( int maxJobs );
        int Priority { get; }
    }
}

