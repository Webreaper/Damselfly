using Accord.Imaging.Filters;
using System.Drawing;

namespace Damselfly.ML.Face.Accord
{
    /// <summary>
    /// From https://code-ai.mk/image-face-detection-with-c/
    /// </summary>
    internal class ImageProcessor
    {
        private Bitmap _bitmap;
        public Bitmap Result { get => _bitmap; }
        public ImageProcessor(Bitmap bitmap)
        {
            _bitmap = bitmap;
        }
        internal ImageProcessor Grayscale()
        {
            var grayscale = new Grayscale(0.2125, 0.7154, 0.0721);
            _bitmap = grayscale.Apply(_bitmap);
            return this;
        }

        internal ImageProcessor EqualizeHistogram()
        {
            HistogramEqualization filter = new HistogramEqualization();
            filter.ApplyInPlace(_bitmap);
            return this;
        }

        internal ImageProcessor Resize(int maxSideLength, out float ratio)
        {
            int longestSide = _bitmap.Height > _bitmap.Width ? _bitmap.Height : _bitmap.Width;

            if (longestSide < maxSideLength)
            {
                // Already small enough. Nothing to do.
                ratio = 1;
                return this;
            }

            ratio = maxSideLength / (float)longestSide;

            var newSize = new Size((int)(_bitmap.Width * ratio), (int)(_bitmap.Height * ratio));

            _bitmap = new Bitmap(_bitmap, newSize);
            return this;
        }
    }
}