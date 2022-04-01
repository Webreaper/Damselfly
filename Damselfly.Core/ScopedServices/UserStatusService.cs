using System;
using Damselfly.Core.Interfaces;
using Damselfly.Core.Utils;
using Damselfly.Core.Services;

namespace Damselfly.Core.ScopedServices;

public class UserStatusService : IStatusService, IDisposable
{
    private StatusService _statusService;
    private string statusText;
    public event Action<string> OnChange;

    public UserStatusService( StatusService statusService )
    {
        _statusService = statusService;
        _statusService.OnChange += HandleGlobalStatus;
    }

    public void Dispose()
    {
        _statusService.OnChange -= HandleGlobalStatus;
    }

    private void HandleGlobalStatus( string text )
    {
        SetStatus( text );
    }

    private void NotifyStateChanged()
    {
        OnChange?.Invoke(statusText);
    }

    private void SetStatus(string newText)
    {
        if (statusText != newText)
        {
            statusText = newText;
            NotifyStateChanged();
        }
    }

    public string StatusText
    {
        get { return statusText; }
        set { SetStatus(value); }
    }
}
