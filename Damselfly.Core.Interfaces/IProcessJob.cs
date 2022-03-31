using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Damselfly.Core.Interfaces
{
    public enum JobPriorities
    {
        FullIndexing = 0,
        ExifService = 1,
        Indexing = 2,
        Metadata = 3,
        Thumbnails = 4,
        ImageRecognition = 5
    };

    public interface IProcessJob
    {
        Task Process();
        bool CanProcess { get; }
        string Name { get; }
        string Description { get; }
        JobPriorities Priority { get; }
    }

    public interface IProcessJobFactory
    {
        Task<ICollection<IProcessJob>> GetPendingJobs( int maxJobs );
        JobPriorities Priority { get; }
    }
}

