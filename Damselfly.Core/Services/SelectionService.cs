using System;
using System.Collections.Generic;
using Damselfly.Core.Models;

namespace Damselfly.Core.Services
{
    public class SelectionService
    {
        // Maintain a look up of all selected images, by ID
        private readonly IDictionary<int, Image> selectedImages = new Dictionary<int, Image>();
        public static SelectionService Instance { get; private set; }
        public event Action OnSelectionChanged;

        public SelectionService()
        {
            Instance = this;
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
            }
        }

        /// <summary>
        /// Add images into the selection
        /// </summary>
        /// <param name="images"></param>
        public void SelectImages(List<Image> images)
        {
            bool added = false;

            foreach( var img in images )
            {
                if (selectedImages.TryAdd(img.ImageId, img))
                    added = true;
            }

            if (added)
                NotifyStateChanged();
        }

        /// <summary>
        /// Add a single image into the selection
        /// </summary>
        /// <param name="img"></param>
        public void SelectImage(Image img)
        {
            if (selectedImages.TryAdd(img.ImageId, img) )
                NotifyStateChanged(); 
        }

        /// <summary>
        /// Remove an image from the selection
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        public bool DeselectImage(Image img)
        {
            if( selectedImages.Remove( img.ImageId ) )
            {
                NotifyStateChanged();
                return true;
            }

            return false;
        }

        public int SelectionCount {  get { return selectedImages.Count;  } }
    }
}
