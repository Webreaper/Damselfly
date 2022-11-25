using Microsoft.JSInterop;

namespace Damselfly.Core.ScopedServices;

public class WebAssemblyStatusService
{
    public WebAssemblyStatusService(IJSRuntime jsRuntime)
    {
        IsWebAssembly = jsRuntime is IJSInProcessRuntime;
    }

    public bool IsWebAssembly { get; }
}