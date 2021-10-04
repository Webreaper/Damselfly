using Damselfly.Core.Utils.Constants;

namespace Damselfly.Core.Interfaces
{
    public interface IExportSettings
    {
        ExportType Type { get; set; }
        ExportSize Size { get; set; }

        bool KeepFolders { get; set; }
        string WatermarkText { get; set; }
        int MaxImageSize { get;  }
    }
}

