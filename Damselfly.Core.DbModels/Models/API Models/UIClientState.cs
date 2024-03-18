namespace Damselfly.Core.DbModels.Models.APIModels;

public class UIClientState
{
    public int ViewportWidth { get; set; }
    public int ViewportHeight { get; set; }
    public string UserAgent { get; set; }

    public bool IsSmallScreenDevice => ViewportWidth < 640; 
    
    public bool IsWideScreen => ViewportWidth > 1260;
}