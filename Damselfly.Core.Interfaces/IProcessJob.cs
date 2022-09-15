using System.Collections.Generic;
using System.Threading.Tasks;

namespace Damselfly.Core.Interfaces;

public enum JobPriorities
{
    FullIndexing = 0,
    ExifService = 1,
    Indexing = 2,
    Metadata = 3,
    Thumbnails = 4,
    ImageRecognition = 5
}

public interface IProcessJob
{
    bool CanProcess { get; }
    string Name { get; }
    string Description { get; }
    JobPriorities Priority { get; }
    Task Process();
}

public interface IProcessJobFactory
{
    JobPriorities Priority { get; }
    Task<ICollection<IProcessJob>> GetPendingJobs(int maxJobs);
}