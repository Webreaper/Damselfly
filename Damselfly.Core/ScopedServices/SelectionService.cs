using System;
using System.Collections.Generic;
using Damselfly.Core.Models;

namespace Damselfly.Core.ScopedServices;

public class SelectionService
{
    // Maintain a look up of all selected images, by ID
    private readonly IDictionary<int, Image> selectedImages = new Dictionary<int, Image>();
    private readonly UserStatusService _statusService;
    public event Action OnSelectionChanged;

    // TODO: Remember last selected image and use it for range selections etc?

    public SelectionService( UserStatusService statusService )
    {
        _statusService = statusService;
    }

    private void NotifyStateChanged()
    {
        OnSelectionChanged?.Invoke();
    }

    /// <summary>
    /// Empty the current selection
    /// </summary>
    public void ClearSelection()
    {
        if (selectedImages.Count > 0 )
        {
            selectedImages.Clear();
            NotifyStateChanged();

            _statusService.StatusText = "Selection cleared.";
        }
    }

    /// <summary>
    /// Add images into the selection
    /// </summary>
    /// <param name="images"></param>
    public void SelectImages(List<Image> images)
    {
        bool added = false;

        foreach (var img in images)
        {
            if (selectedImages.TryAdd(img.ImageId, img))
                added = true;
        }

        if (added)
        {
            NotifyStateChanged();
            if( images.Count > 1 )
                _statusService.StatusText = $"{images.Count} images selected.";
        }
    }

    /// <summary>
    /// Add images into the selection
    /// </summary>
    /// <param name="images"></param>
    public void DeselectImages(List<Image> images)
    {
        bool removed = false;

        foreach (var img in images)
        {
            if (selectedImages.Remove(img.ImageId))
                removed = true;
        }

        if (removed)
            NotifyStateChanged();
    }

    /// <summary>
    /// Add images into the selection
    /// </summary>
    /// <param name="images"></param>
    public void ToggleSelection(List<Image> images)
    {
        foreach( var img in images )
        {
            // Try and add it. If it wasn't there, it'll succeed.
            // If it fails, we need to remove it.
            if( ! selectedImages.TryAdd(img.ImageId, img) )
                selectedImages.Remove(img.ImageId);
        }

        NotifyStateChanged();
    }

    /// <summary>
    /// Add a single image into the selection
    /// </summary>
    /// <param name="img"></param>
    public void SelectImage(Image img) => SelectImages(new List<Image> { img });

    /// <summary>
    /// Remove an image from the selection
    /// </summary>
    /// <param name="img"></param>
    /// <returns></returns>
    public void DeselectImage(Image img) => DeselectImages(new List<Image> { img });

    public int SelectionCount {  get { return selectedImages.Count;  } }

    /// <summary>
    /// Unordered set of selected images.
    /// </summary>
    public ICollection<Image> Selection
    {
        get { return selectedImages.Values; }
    }

    public bool IsSelected( Image image )
    {
        return selectedImages.ContainsKey(image.ImageId);
    }
}
