using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Damselfly.Core.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Damselfly.Web.Components;

/// <summary>
/// Virtual scrolling view. Builds a list of items, and given the
/// underlying collection, presents a windowed view to it based
/// on the scroll position (which is obtained via a JS interop).
/// </summary>
/// <typeparam name="ItemType"></typeparam>
public class BaseVirtualScroll<ItemType> : ComponentBase, IVirtualScroll
{
    [Parameter]
    public string Class { get; set; }

    [Parameter]
    public string Style { get; set; }

    [Parameter]
    public int ItemHeight { get; set; } = 50;

    [Parameter]
    public RenderFragment<ItemType> ChildContent { get; set; }

    [Parameter]
    public IEnumerable<ItemType> Items { get; set; }

    [Inject]
    public IJSRuntime JsRuntime { get; set; }

    public VirtualScrollJsHelper JsHelper { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            JsHelper = new VirtualScrollJsHelper(this);
            await JsRuntime.InvokeAsync<ScrollView>("blazorVirtualScrolling.init", "vscroll", DotNetObjectReference.Create(JsHelper));
        }
    }

    /// <summary>
    /// Callback from the client Javascript which will be called
    /// whenever the list is scrolled or the view is resized.
    /// </summary>
    /// <param name="scrollView"></param>
    public void VirtualScrollingSetView(ScrollView scrollView)
    {
        if (ScrollView == null || !ScrollView.Equals(scrollView))
        {
            Logging.LogTrace("Scrollview: " + scrollView.ToString());
            ScrollView = scrollView;
            RecalcScrollWindow();
        }
    }

    /// <summary>
    /// Calculate the scroll window - take the item collection, and based
    /// on the visible scroll window size we take the n items that will fit
    /// in that scroll window and return them. This will lead to those n
    /// items being rendered in the DOM
    /// </summary>
    private void RecalcScrollWindow()
    {
        if (ScrollView != null)
        {
            var newResult = new ScrollViewResult();
            newResult.Height = Items.Count() * ItemHeight;
            newResult.SkipItems = ScrollView.ScrollTop / ItemHeight;
            newResult.TakeItems = (int)Math.Ceiling((double)(ScrollView.ScrollTop + ScrollView.ClientHeight) / (double)ItemHeight) - newResult.SkipItems;

            if (ScrollViewResult == null || !ScrollViewResult.Equals(newResult))
            {
                Logging.LogTrace("New scroll state: " + newResult.ToString() );
                ScrollViewResult = newResult;
                StateHasChanged();
            }
        }
    }

    /// <summary>
    /// Handle the case where the parameters change - for example
    /// a new items collection is assigned. At that point the
    /// collection size may have altered, so recalculate the window. 
    /// </summary>
    protected override void OnParametersSet()
    {
        RecalcScrollWindow();
        base.OnParametersSet();
    }

    public ScrollViewResult ScrollViewResult { get; set; }

    public ScrollView ScrollView { get; set; }
}
