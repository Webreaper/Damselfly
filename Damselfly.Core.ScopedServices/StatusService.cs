using System;
using Damselfly.Core.Interfaces;
using Damselfly.Core.Utils;

namespace Damselfly.Core.Services;

public class StatusService : IStatusService
{
    private string statusText;
    public event Action<string> OnChange;

    public StatusService()
    {
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
            Logging.Log($"Status: {statusText}");
        }
    }

    public string StatusText
    {
        get { return statusText; }
        set { SetStatus(value);  }
    }
}
