using Damselfly.Core.ImageProcessing;
using Damselfly.Core.Models;

namespace Damselfly.Web.Data
{
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
}
