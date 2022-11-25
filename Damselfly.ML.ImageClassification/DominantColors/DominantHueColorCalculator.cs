using System.Collections.Generic;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = System.Drawing.Color;

namespace Damselfly.ML.ImageClassification.DominantColors;

/// <summary>
///     Dominant Colour calculator by Jelle Vergeer
///     https://github.com/jellever/DominantColor
/// </summary>
public class DominantHueColorCalculator
{
    private readonly float brightnessThreshold;
    private readonly int hueSmoothFactor;
    private readonly float saturationThreshold;
    private Dictionary<int, uint> hueHistogram;
    private Dictionary<int, uint> smoothedHueHistogram;

    /// <summary>
    ///     ctor
    /// </summary>
    /// <param name="saturationThreshold">The saturation thresshold</param>
    /// <param name="brightnessThreshold">The brightness thresshold</param>
    /// <param name="hueSmoothFactor">hue smoothing factor</param>
    public DominantHueColorCalculator(float saturationThreshold, float brightnessThreshold, int hueSmoothFactor)
    {
        this.saturationThreshold = saturationThreshold;
        this.brightnessThreshold = brightnessThreshold;
        this.hueSmoothFactor = hueSmoothFactor;
        hueHistogram = new Dictionary<int, uint>();
        smoothedHueHistogram = new Dictionary<int, uint>();
    }

    public DominantHueColorCalculator() :
        this(0.3f, 0.0f, 4)
    {
    }

    /// <summary>
    ///     The Hue histogram used in the calculation for dominant color
    /// </summary>
    public Dictionary<int, uint> HueHistogram => new ( hueHistogram );

    /// <summary>
    ///     The smoothed histogram used in the calculation for dominant color
    /// </summary>
    public Dictionary<int, uint> SmoothedHueHistorgram => new ( smoothedHueHistogram );

    /// <summary>
    ///     Get dominant hue in given hue histogram
    /// </summary>
    /// <param name="hueHistogram"></param>
    /// <returns></returns>
    private int GetDominantHue(Dictionary<int, uint> hueHistogram)
    {
        var dominantHue = hueHistogram.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
        return dominantHue;
    }

    /// <summary>
    ///     Calculate dominant color for given bitmap
    /// </summary>
    /// <param name="bitmap"></param>
    /// <returns></returns>
    public Color CalculateDominantColor(Image<Rgb24> image)
    {
        hueHistogram = DominantColorUtils.GetColorHueHistogram(image, saturationThreshold,
            brightnessThreshold);
        smoothedHueHistogram = DominantColorUtils.SmoothHistogram(hueHistogram, hueSmoothFactor);
        var dominantHue = GetDominantHue(smoothedHueHistogram);

        return DominantColorUtils.ColorFromHSV(dominantHue, 1, 1);
    }
}