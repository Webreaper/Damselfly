using Microsoft.JSInterop;

namespace Damselfly.Core.ScopedServices;

public class ApplicationStateService
{
    public ApplicationStateService(IJSRuntime jsRuntime)
    {
        IsWebAssembly = jsRuntime is IJSInProcessRuntime;
    }

    public bool IsWebAssembly { get; }
}