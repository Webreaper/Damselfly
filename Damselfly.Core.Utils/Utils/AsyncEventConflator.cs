using System;
using System.Threading;
using System.Threading.Tasks;

namespace Damselfly.Core.Utils;

/// <summary>
///     Event conflator. Ensures we don't call methods many times for
///     a series of events, such as keypresses when somebody is typing.
///     For every event, we start a timer; if another event is handled
///     within that time, we cancel it and start again. If we get to the
///     end of the timer without another key being pressed, only then do
///     we call the callback passed into us.
///     This can be used for any type of event we want to conflate.
/// </summary>
public class AsyncEventConflator : IDisposable
{
    private CancellationTokenSource? _cancellationTokenSource = null;
    private bool _disposed;
    private int _delayMs = 500;
    
    public AsyncEventConflator(int delayMs = 500)
    {
        _delayMs = delayMs;
    }
    
    /// <summary>
    /// Starts the debouncing.
    /// </summary>
    /// <param name="actionAsync">The asynchronous action to be executed.
    /// The <see cref="CancellationToken"/> gets canceled if the method is
    /// called again.</param>
    public async Task ConflateAsync(Func<CancellationToken, Task> actionAsync)
    {
        try
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
            }
            
            _cancellationTokenSource = new CancellationTokenSource();
            
            await Task.Delay(_delayMs, _cancellationTokenSource.Token);

            await actionAsync(_cancellationTokenSource.Token);
        }
        catch (TaskCanceledException)
        {
            // do nothing
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource?.Dispose();
                }
            }
            
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
    }
}