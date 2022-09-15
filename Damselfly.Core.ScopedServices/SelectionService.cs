using Damselfly.Core.Models;
using Damselfly.Core.ScopedServices.Interfaces;

namespace Damselfly.Core.ScopedServices;

public class SelectionService
{
    private readonly IUserStatusService _statusService;

    private readonly IUserService _userService;

    // Maintain a look up of all selected images, by ID
    private readonly IDictionary<int, Image> selectedImages = new Dictionary<int, Image>();

    // TODO: Remember last selected image and use it for range selections etc?

    public SelectionService(IUserStatusService statusService, IUserService userService)
    {
        _statusService = statusService;
        _userService = userService;
    }

    public int SelectionCount => selectedImages.Count;

    /// <summary>
    ///     Unordered set of selected images.
    /// </summary>
    public ICollection<Image> Selection => selectedImages.Values;

    public event Action OnSelectionChanged;

    private void NotifyStateChanged()
    {
        OnSelectionChanged?.Invoke();
    }

    /// <summary>
    ///     Empty the current selection
    /// </summary>
    public void ClearSelection()
    {
        if ( selectedImages.Count > 0 )
        {
            selectedImages.Clear();
            NotifyStateChanged();

            _statusService.UpdateStatus("Selection cleared.");
        }
    }

    /// <summary>
    ///     Add images into the selection
    /// </summary>
    /// <param name="images"></param>
    public void SelectImages(List<Image> images)
    {
        var added = false;

        foreach ( var img in images )
            if ( selectedImages.TryAdd(img.ImageId, img) )
                added = true;

        if ( added )
        {
            NotifyStateChanged();
            if ( images.Count > 1 && _statusService != null )
                _statusService.UpdateStatus($"{images.Count} images selected.");
        }
    }

    /// <summary>
    ///     Add images into the selection
    /// </summary>
    /// <param name="images"></param>
    public void DeselectImages(List<Image> images)
    {
        var removed = false;

        foreach ( var img in images )
            if ( selectedImages.Remove(img.ImageId) )
                removed = true;

        if ( removed )
            NotifyStateChanged();
    }

    /// <summary>
    ///     Add images into the selection
    /// </summary>
    /// <param name="images"></param>
    public void ToggleSelection(List<Image> images)
    {
        foreach ( var img in images )
            // Try and add it. If it wasn't there, it'll succeed.
            // If it fails, we need to remove it.
            if ( !selectedImages.TryAdd(img.ImageId, img) )
                selectedImages.Remove(img.ImageId);

        NotifyStateChanged();
    }

    /// <summary>
    ///     Add a single image into the selection
    /// </summary>
    /// <param name="img"></param>
    public void SelectImage(Image img)
    {
        SelectImages(new List<Image> { img });
    }

    /// <summary>
    ///     Remove an image from the selection
    /// </summary>
    /// <param name="img"></param>
    /// <returns></returns>
    public void DeselectImage(Image img)
    {
        DeselectImages(new List<Image> { img });
    }

    public bool IsSelected(Image image)
    {
        return selectedImages.ContainsKey(image.ImageId);
    }
}