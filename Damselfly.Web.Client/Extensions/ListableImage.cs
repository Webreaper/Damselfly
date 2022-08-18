using Damselfly.Core.Constants;
using Damselfly.Core.Models;

namespace Damselfly.Web.Components;

public class ListableImage
{
    public string ThumbURL => $"/thumb/{Size}/{Image.ImageId}";
    public Image Image { get; private set; }
    private ThumbSize Size { get; set; }

    public ListableImage(Image image, ThumbSize size)
    {
        Image = image;
        Size = size;
    }

}
