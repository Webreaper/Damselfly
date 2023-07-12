using Damselfly.Core.Constants;
using Damselfly.Core.Interfaces;
using Humanizer;

namespace Damselfly.Core.Models;

/// <summary>
///     Config associated with an export or download
/// </summary>
public class ExportConfig : IExportSettings
{
    public int ExportConfigId { get; set; }
    public string? Name { get; set; }
    public ExportType Type { get; set; } = ExportType.Download;
    public ExportSize Size { get; set; } = ExportSize.FullRes;
    public bool KeepFolders { get; set; }
    public string? WatermarkText { get; set; }

    public int MaxImageSize => MaxSize(Size);

    public string SizeDesc()
    {
        return SizeDesc(Size);
    }

    public static string SizeDesc(ExportSize size)
    {
        return $"{size.Humanize()}" + (size == ExportSize.FullRes ? "" : $" (max {MaxSize(size)}x{MaxSize(size)})");
    }

    public static int MaxSize(ExportSize size)
    {
        return size switch
        {
            ExportSize.ExtraLarge => 1920,
            ExportSize.Large => 1600,
            ExportSize.Medium => 1024,
            ExportSize.Small => 800,
            _ => int.MaxValue
        };
    }
}