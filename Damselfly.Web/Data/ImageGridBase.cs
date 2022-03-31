using System.Linq;
using System.Collections.Generic;
using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices;
using Damselfly.Core.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Damselfly.Web.Data;

/// <summary>
/// Base class for the main image grid and basket image grids.
/// Implements the selection model logic for Mouse-clicks
/// </summary>
public class ImageGridBase : ComponentBase 
{
    [Inject]
    protected SelectionService selectionService { get; set; }

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


    // Grid images is a list of lists of images.
    protected readonly List<Image> gridImages = new List<Image>();

    private SelectionInfo prevSelection = null;

    /// <summary>
    /// Manage the selection state for the grid images.
    /// </summary>
    /// <param name="e"></param>
    /// <param name="image"></param>
    protected void ToggleSelected(MouseEventArgs e, SelectionInfo selectionInfo)
    {
        var watch = new Stopwatch("ToggleSelection");
        if (e.ShiftKey && prevSelection != null)
        {
            // Range selection.
            var first = prevSelection.index;
            var last = selectionInfo.index;

            if (first > last)
            {
                var temp = last;
                last = first;
                first = temp;
            }

            Logging.LogVerbose($"Selecting images {first} ({prevSelection.image.FileName}) to {last} ({selectionInfo.image.FileName})");

            var selectedImages = gridImages.Skip(first).Take(last - (first - 1)).ToList();
            selectionService.SelectImages(selectedImages);
        }
        else
        {
            if (e.MetaKey || e.CtrlKey)
            {
                // Apple key was pressed - toggle the selection
                selectionService.ToggleSelection(new List<Image> { selectionInfo.image });
            }
            else
            {
                // No keys pressed. Select if unselected, or deselect if selected - but
                // clear any other selection at the same time. Store the last selection
                // as it could be the beginning of a range selection
                bool wasPreviouslySelected = selectionService.IsSelected(selectionInfo.image);
                selectionService.ClearSelection();
                prevSelection = null;

                if (!wasPreviouslySelected)
                {
                    selectionService.SelectImage(selectionInfo.image);
                    prevSelection = selectionInfo;
                }
            }
        }

        watch.Stop();
    }
}
