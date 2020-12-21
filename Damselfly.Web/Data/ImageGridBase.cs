using System.Collections.Generic;
using Damselfly.Core.Models;
using Damselfly.Core.Services;
using Damselfly.Core.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Damselfly.Web.Data
{
    /// <summary>
    /// Base class for the main image grid and basket image grids.
    /// Implements the selection model logic for Mouse-clicks
    /// </summary>
    public class ImageGridBase : ComponentBase 
    {
        protected readonly List<Image> gridImages = new List<Image>();
        private Image prevSelection = null;

        /// <summary>
        /// Manage the selection state for the grid images.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="image"></param>
        protected void ToggleSelected(MouseEventArgs e, Image image)
        {
            var watch = new Stopwatch("ToggleSelection");
            if (e.ShiftKey && prevSelection != null)
            {
                // Range selection.
                var first = gridImages.FindIndex(x => x.ImageId == prevSelection.ImageId);
                var last = gridImages.FindIndex(x => x.ImageId == image.ImageId);

                if (first > last)
                {
                    var temp = last;
                    last = first;
                    first = temp;
                }

                Logging.LogVerbose($"Selecting images {first} ({prevSelection.FileName}) to {last} ({image.FileName})");

                for (int i = first; i <= last; i++)
                {
                    var img = gridImages[i];
                    SelectionService.Instance.SelectImage(img);
                }
            }
            else
            {
                if (e.MetaKey)
                {
                    // Apple key was pressed - toggle the selection
                    SelectionService.Instance.ToggleSelection(new List<Image> { image });
                }
                else
                {
                    // No keys pressed. Select if unselected, or deselect if selected - but
                    // clear any other selection at the same time. Store the last selection
                    // as it could be the beginning of a range selection
                    bool wasPreviouslySelected = SelectionService.Instance.IsSelected(image);
                    SelectionService.Instance.ClearSelection();
                    prevSelection = null;

                    if (!wasPreviouslySelected)
                    {
                        SelectionService.Instance.SelectImage(image);
                        prevSelection = image;
                    }
                }
            }

            watch.Stop();
        }
    }
}
