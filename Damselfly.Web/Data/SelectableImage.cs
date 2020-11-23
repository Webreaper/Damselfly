using Damselfly.Core.ImageProcessing;
using Damselfly.Core.Models;
using Damselfly.Core.Services;

namespace Damselfly.Web.Data
{
    /// <summary>
    /// Small UI wrapper to represent an image which can be selected
    /// and deselected. This is mainly to simplify UI code.
    /// </summary>
    public class SelectableImage
    {
        // TODO - rename this to selection and Selected to Basket
        public bool Selected { get; set; }
        public Image Image { get; private set; }

        public SelectableImage( Image image )
        {
            Image = image;
        }

        public bool InBasket
        {
            get { return BasketService.Instance.IsSelected( Image ); }
            set {
                BasketService.Instance.SetBasketState(Image, value);
                // Notify the image list that the selection has changed
                SearchService.Instance.NotifyStateChanged(); 
            }
        }
    }

    public class ExportableImage
    {
        public string ThumbURL { get; private set; }
        public Image Image { get; private set; }

        public ExportableImage(Image image, ThumbSize size)
        {
            Image = image;
            ThumbURL = ThumbnailService.Instance.GetThumbRequestPath(image, size, "/no-image.png");
        }

    }
}
