namespace Damselfly.Core.Interfaces;

public interface IHashProvider
{
    string GetPerceptualHash(string path);
}