namespace Damselfly.Core.DbModels.Models.APIModels;

public class UIClientState
{
    public int ViewportWidth { get; set; }
    public int ViewportHeight { get; set; }
    public string UserAgent { get; set; }

    public bool IsPortrait => ViewportWidth < ViewportHeight;
    public bool IsSmallScreenDevice => IsPortrait ? ViewportWidth < 640 : ViewportHeight < 640;

    public bool IsWideScreen => ViewportWidth > 1260;
}