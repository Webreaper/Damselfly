using Microsoft.JSInterop;

namespace Damselfly.Web.Components;

public class CropData
{
    public int Top { get; set; }
    public int Left { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

/// <summary>
/// Callback management for th Crop JS interop
/// </summary>
public class CropJsHelper
{
    private readonly ICropHelper _host;

    public CropJsHelper(ICropHelper host)
    {
        _host = host;
    }

    [JSInvokable]
    public void CompleteCrop(CropData cropData)
    {
        _host.CompleteCrop(cropData);
    }
}

public interface ICropHelper
{
    void CompleteCrop(CropData cropData);
}

