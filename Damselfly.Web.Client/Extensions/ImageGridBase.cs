using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Shared.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Damselfly.Web.Components;

/// <summary>
///     Base class for the main image grid and basket image grids.
///     Implements the selection model logic for Mouse-clicks
/// </summary>
public class ImageGridBase : ComponentBase
{
    // Grid images is a list of lists of images.
    protected readonly List<Image> gridImages = new();

    private SelectionInfo prevSelection;

    [Inject] protected SelectionService selectionService { get; init; }

    [Inject] protected IImageCacheService cacheService { get; init; }

    [Inject] private ILogger<ImageGridBase> _logger { get; init; }

    /// <summary>
    ///     Manage the selection state for the grid images.
    /// </summary>
    /// <param name="e"></param>
    /// <param name="image"></param>
    protected async Task ToggleSelected(MouseEventArgs e, SelectionInfo selectionInfo)
    {
        var watch = new Stopwatch("ToggleSelection");
        if ( e.ShiftKey && prevSelection != null )
        {
            // Range selection.
            var first = prevSelection.index;
            var last = selectionInfo.index;

            if ( first > last )
            {
                var temp = last;
                last = first;
                first = temp;
            }

            _logger.LogTrace(
                $"Selecting images {first} ({prevSelection.image.FileName}) to {last} ({selectionInfo.image.FileName})");

            var selectedImages = gridImages.Skip(first).Take(last - (first - 1)).Select(x => x.ImageId).ToList();
            var images = await cacheService.GetCachedImages(selectedImages);
            selectionService.SelectImages(images);
        }
        else
        {
            if ( e.MetaKey || e.CtrlKey )
            {
                // Apple key was pressed - toggle the selection
                selectionService.ToggleSelection(new List<Image> { selectionInfo.image });
            }
            else
            {
                // No keys pressed. Select if unselected, or deselect if selected - but
                // clear any other selection at the same time. Store the last selection
                // as it could be the beginning of a range selection
                var wasPreviouslySelected = selectionService.IsSelected(selectionInfo.image);
                selectionService.ClearSelection();
                prevSelection = null;

                if ( !wasPreviouslySelected )
                {
                    selectionService.SelectImage(selectionInfo.image);
                    prevSelection = selectionInfo;
                }
            }
        }

        watch.Stop();
    }

    public class SelectionInfo
    {
        public Image image;
        public int index;
    }

    protected class ImageGrouping
    {
        public string Key { get; set; }
        public List<Image> Images { get; set; }
    }
}