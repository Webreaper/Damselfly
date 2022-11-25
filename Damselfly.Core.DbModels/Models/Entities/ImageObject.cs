using System.ComponentModel.DataAnnotations;
using Humanizer;

namespace Damselfly.Core.Models;

/// <summary>
///     One image can have a number of objects each with a name.
/// </summary>
public class ImageObject
{
    public enum ObjectTypes
    {
        Object = 0,
        Face = 1
    }

    public enum RecognitionType
    {
        Manual = 0,
        Emgu = 1,
        Accord = 2, // Deprecated
        Azure = 3,
        MLNetObject = 4,
        ExternalApp = 5
    }

    [Key] public int ImageObjectId { get; set; }

    [Required] public int ImageId { get; set; }

    public virtual Image Image { get; set; }

    [Required] public int TagId { get; set; }

    public virtual Tag Tag { get; set; }

    public string Type { get; set; } = ObjectTypes.Object.ToString();
    public RecognitionType RecogntionSource { get; set; }
    public double Score { get; set; }
    public int RectX { get; set; }
    public int RectY { get; set; }
    public int RectWidth { get; set; }
    public int RectHeight { get; set; }

    public int? PersonId { get; set; }
    public virtual Person Person { get; set; }

    public bool IsFace => Type == ObjectTypes.Face.ToString();

    public override string ToString()
    {
        return GetTagName();
    }

    public string GetTagName(bool includeScore = false)
    {
        var ret = "Unidentified Object";

        if ( IsFace )
        {
            if ( Person != null && Person.Name != "Unknown" )
                return $"{Person.Name.Transform(To.TitleCase)}";
            ret = "Unidentified face";
        }
        else if ( Type == ObjectTypes.Object.ToString() && Tag != null )
        {
            ret = $"{Tag.Keyword.Transform(To.SentenceCase)}";
        }

        if ( includeScore && Score > 0 ) ret += $" ({Score:P0})";

        return ret;
    }
}