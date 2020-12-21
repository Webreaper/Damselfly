using System.IO;
using System.Collections.Generic;
using Damselfly.Core.ImageProcessing;

namespace Damselfly.Core.Interfaces
{
    /// <summary>
    /// Interface representing a generic image processing pipeline. This
    /// allows us to swap out different implementations etc depending on
    /// performance and other characteristics.
    /// </summary>
    public interface IImageProcessor
    {
        void CreateThumbs(FileInfo source, IDictionary<FileInfo, ThumbConfig> destFiles, out string imageHash);
        void TransformDownloadImage(string input, Stream output, string waterMarkText = null);
        ICollection<string> SupportedFileExtensions { get; }
    }
}
