using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = System.Drawing.Color;

namespace Damselfly.ML.ImageClassification.DominantColors
{
    public class DominantColorUtils
    {
        /// <summary>
        /// Get hue histogram for given bitmap.
        /// </summary>
        /// <param name="bmp">The bitmap to get the histogram for</param>
        /// <param name="saturationThreshold">The saturation threshold to take into account getting the histogram</param>
        /// <param name="brightnessThreshold">The brightness threshold to take into account getting the histogram</param>
        /// <returns>A dictionary representing the hue histogram. Key: Hue index (0-360). Value: Occurence of the hue.</returns>
        internal static unsafe Dictionary<int, uint> GetColorHueHistogram(Image<Rgb24> image, float saturationThreshold, float brightnessThreshold)
        {
            Dictionary<int, uint> colorHueHistorgram = new Dictionary<int, uint>();
            for (int i = 0; i <= 360; i++)
            {
                colorHueHistorgram.Add(i, 0);
            }

            image.ProcessPixelRows( pixelAccessor =>
            {
                for( var y = 0; y < pixelAccessor.Height; y++ )
                {
                    var row = pixelAccessor.GetRowSpan( y );

                    for( var x = 0; x < row.Length; x++ )
                    {
                        var clr = System.Drawing.Color.FromArgb( row[x].R, row[x].G, row[x].B );

                        if( clr.GetSaturation() > saturationThreshold && clr.GetBrightness() > brightnessThreshold )
                        {
                            int hue = (int)Math.Round( clr.GetHue(), 0 );
                            colorHueHistorgram[hue]++;
                        }
                    }
                }
            } );

            return colorHueHistorgram;
        }

        /// <summary>
        /// Correct out of bound hue index
        /// </summary>
        /// <param name="hue">hue index</param>
        /// <returns>Corrected hue index (within 0-360 boundaries)</returns>
        private static int CorrectHueIndex(int hue)
        {
            int result = hue;
            if (result > 360)
                result = result - 360;
            if (result < 0)
                result = result + 360;
            return result;
        }

        /// <summary>
        /// Get color from HSV (Hue, Saturation, Brightness) combination.
        /// </summary>
        /// <param name="hue"></param>
        /// <param name="saturation"></param>
        /// <param name="value"></param>
        /// <returns>The color</returns>
        public static System.Drawing.Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            switch (hi)
            {
                case 0:
                    return Color.FromArgb(255, v, t, p);
                case 1:
                    return Color.FromArgb(255, q, v, p);
                case 2:
                    return Color.FromArgb(255, p, v, t);
                case 3:
                    return Color.FromArgb(255, p, q, v);
                case 4:
                    return Color.FromArgb(255, t, p, v);
                default:
                    return Color.FromArgb(255, v, p, q);
            }
        }

        /// <summary>
        /// Smooth histogram with given smoothfactor. 
        /// </summary>
        /// <param name="colorHueHistogram">The histogram to smooth</param>
        /// <param name="smoothFactor">How many hue neighbouring hue indexes will be averaged by the smoothing algoritme.</param>
        /// <returns>Smoothed hue color histogram</returns>
        internal static Dictionary<int, uint> SmoothHistogram(Dictionary<int, uint> colorHueHistogram, int smoothFactor)
        {
            if (smoothFactor < 0 || smoothFactor > 360)
                throw new ArgumentException("smoothFactor may not be negative or bigger then 360", nameof(smoothFactor));
            if (smoothFactor == 0)
                return new Dictionary<int, uint>(colorHueHistogram);

            Dictionary<int, uint> newHistogram = new Dictionary<int, uint>();
            int totalNrColumns = (smoothFactor * 2) + 1;
            for (int i = 0; i <= 360; i++)
            {
                uint sum = 0;
                uint average = 0;
                for (int x = i - smoothFactor; x <= i + smoothFactor; x++)
                {
                    int hueIndex = CorrectHueIndex(x);
                    sum += colorHueHistogram[hueIndex];
                }
                average = (uint)(sum / totalNrColumns);
                newHistogram[i] = average;
            }
            return newHistogram;
        }
    }
}
