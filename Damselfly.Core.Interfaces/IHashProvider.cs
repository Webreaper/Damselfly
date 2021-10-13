using System.IO;

namespace Damselfly.Core.Interfaces
{
    public interface IHashProvider
    {
        string GetPerceptualHash(string path);
    }
}

