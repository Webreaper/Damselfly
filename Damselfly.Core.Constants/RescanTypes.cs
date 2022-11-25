namespace Damselfly.Core.Constants;

[Flags]
public enum RescanTypes
{
    None = 0,
    Indexing = 1,
    Metadata = 2,
    Thumbnails = 4,
    AI = 8
}