namespace Damselfly.Core.DbModels;

public class ThemeConfig
{
    public string Name { get; set; }
    public string Path { get; set; }

    // Colours
    public string Primary { get; set; }
    public string PrimaryDarken { get; set; }
    public string PrimaryLighten { get; set; }
    public string Black { get; set; }
    public string Background { get; set; }
    public string BackgroundGrey { get; set; }
    public string Surface { get; set; }
    public string Tertiary { get; set; }
    public string DrawerBackground { get; set; }
    public string DrawerText { get; set; }
    public string AppbarBackground { get; set; }
    public string AppbarText { get; set; }
    public string TextPrimary { get; set; }
    public string TextSecondary { get; set; }
    public string ActionDefault { get; set; }
    public string DrawerIcon { get; set; }
    public string ActionDisabled { get; set; }
    public string ActionDisabledBackground { get; set; }
    public string Divider { get; set; }
    public string DividerLight { get; set; }
    public string TableLines { get; set; }
    public string LinesDefault { get; set; }
    public string LinesInputs { get; set; }
    public string TextDisabled { get; set; }
    public string Warning { get; set; }

    public override string ToString()
    {
        return $"Theme: {Name} [bg: {Background}, fg: {Primary}] Path: {Path}";
    }
}