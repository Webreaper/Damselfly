using Damselfly.Core.ImageProcessing;
using Damselfly.Core.Models;
using Damselfly.Core.Services;

namespace Damselfly.Web.Data
{
    public class ListableImage
    {
        public string ThumbURL { get; private set; }
        public Image Image { get; private set; }

        public ListableImage(Image image, ThumbSize size)
        {
            Image = image;
            ThumbURL = ThumbnailService.Instance.GetThumbRequestPath(image, size, "/no-image.png");
        }

    }
}
