using System;
using System.IO;
using System.Threading.Tasks;
using Damselfly.Core.Interfaces;

namespace Damselfly.Core.Models
{
    public class ProcessJob
    {
        public string Name { get; set; }
        public DateTime CreationTimeStamp { get; } = DateTime.UtcNow;
        public IProcessJob Job { get; set; }
    }
}

