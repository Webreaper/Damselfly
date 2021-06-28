using Accord.Imaging.Filters;
using System.Drawing;

namespace Damselfly.ML.Accord.Face
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

        internal ImageProcessor Resize(Size size)
        {
            _bitmap = new Bitmap(_bitmap, size);
            return this;
        }
    }
}