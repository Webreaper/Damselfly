using System.Drawing;

namespace Damselfly.Core.Utils;

public static class ColorUtils
{
    public static string ToHex(this Color c)
    {
        return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
    }

    public static string ToRgb(this Color c)
    {
        return $"rgb({c.R}, {c.G}, {c.B})";
    }
}