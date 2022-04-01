
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Blazor.FileReader.Demo.Common;

/// <summary>
/// Writable test Stream that does nothing other than setting it's Position property
/// </summary>
public class PositionStream : Stream
{

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => throw new NotImplementedException();

    public override long Position { get; set; }

    public override void Flush()
    {
        // No-op
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        Position += count;
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
