using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Damselfly.Core.Utils.Images;
using System.Drawing;

namespace Damselfly.Core.Interfaces
{
    public interface IImageProcessorFactory
    {
        IImageProcessor GetProcessor(string fileExtension);
        IHashProvider GetHashProvider();
        void SetContentPath(string contentPath);
    }

    /// <summary>
    /// Interface representing a generic image processing pipeline. This
    /// allows us to swap out different implementations etc depending on
    /// performance and other characteristics.
    /// </summary>
    public interface IImageProcessor
    {
        Task<ImageProcessResult> CreateThumbs(FileInfo source, IDictionary<FileInfo, ThumbConfig> destFiles );
        Task GetCroppedFile(FileInfo source, int x, int y, int width, int height, FileInfo destFile);
        Task CropImage(FileInfo path, int x, int y, int width, int height, Stream stream);
        Task TransformDownloadImage(string input, Stream output, IExportSettings exportConfig);

        static ICollection<string> SupportedFileExtensions { get; }
    }
}
