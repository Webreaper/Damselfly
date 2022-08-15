using Microsoft.JSInterop;

namespace Damselfly.Web.Components;

/// <summary>
/// Callback management for th Scrollview JS interop
/// </summary>
public class VirtualScrollJsHelper
{
    private readonly IVirtualScroll _host;

    public VirtualScrollJsHelper(IVirtualScroll host)
    {
        _host = host;
    }

    [JSInvokable]
    public void VirtualScrollingSetView(ScrollView view)
    {
        _host.VirtualScrollingSetView(view);
    }
}

public interface IVirtualScroll
{
    void VirtualScrollingSetView(ScrollView view);
}
