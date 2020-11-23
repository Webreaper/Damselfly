
using Damselfly.Core.Services;
/// <summary>
/// State helpers for the scrollview
/// </summary>
namespace Damselfly.Web.Components
{
    public class ScrollView
    {
        public int ClientHeight { get; set; }
        public int ScrollTop { set; get; }

        public override string ToString()
        {
            return $"ClientHeight: {ClientHeight}, Top: {ScrollTop}";
        }

        public override bool Equals(object obj)
        {
            var other = obj as ScrollView;

            if (other == null || other.ClientHeight != ClientHeight || other.ScrollTop != ScrollTop)
                return false;

            return true;
        }
    }

    public class ScrollViewResult
    {
        public int Height { get; set; }
        public int SkipItems { get; set; }
        public int TakeItems { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as ScrollViewResult;

            if (other == null || other.Height != Height || other.SkipItems != SkipItems || other.TakeItems != TakeItems)
                return false;

            return true;
        }

        public override string ToString()
        {
            return $"Height: {Height}, Skip: {SkipItems}, Take: {TakeItems}";
        }
    }
}