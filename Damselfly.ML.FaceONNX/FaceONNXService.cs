using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Damselfly.Core.Constants;
using Damselfly.Core.Interfaces;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Utils;
using Damselfly.Shared.Utils;
using FaceEmbeddingsClassification;
using FaceONNX;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Damselfly.ML.FaceONNX;

public class FaceONNXService : IDisposable
{
    private readonly IConfigService _configService;

    private FaceDetector _faceDetector;
    private FaceLandmarksExtractor _faceLandmarksExtractor;
    private FaceEmbedder _faceEmbedder;
    private Embeddings _embeddings;

    public FaceONNXService(IConfigService configService)
    {
        _configService = configService;
    }
    
    public void Dispose()
    {
        _faceDetector.Dispose();
        _faceLandmarksExtractor.Dispose();
        _faceEmbedder.Dispose();
    }

    /// <summary>
    ///     Initialise the face training service
    /// </summary>
    /// <returns></returns>
    public async Task StartService()
    {
        try
        {
            // TODO: Load the faces from the DB and populate the embeddings
            _faceDetector = new FaceDetector();
            _faceLandmarksExtractor = new FaceLandmarksExtractor();
            _faceEmbedder = new FaceEmbedder();
            _embeddings = new Embeddings();
        }
        catch ( Exception ex )
        {
            Logging.LogError($"Unable to start Azure service: {ex.Message}");
            if ( ex.InnerException != null )
                Logging.LogError($"Inner exception: {ex.InnerException}");
        }
    }

    private class FaceONNXFace
    {
        public string? PersonId { get; set; }
        public Rectangle Rectangle { get; set; }
        public float[] Embeddings { get; set; }
    }
    
    private IEnumerable<FaceONNXFace> GetFacesFromImage(Image<Rgb24> image)
    {
        var array = new []
        {
            new float [image.Height,image.Width],
            new float [image.Height,image.Width],
            new float [image.Height,image.Width]
        };            
            
        image.ProcessPixelRows(pixelAccessor =>
        {
            for ( var y = 0; y < pixelAccessor.Height; y++ )
            {
                var row = pixelAccessor.GetRowSpan(y);
                for(var x = 0; x < pixelAccessor.Width; x++ )
                {
                    array[0][y, x] = row[x].R;
                    array[1][y, x] = row[x].G;
                    array[2][y, x] = row[x].B;
                }
            }
        });

        var detectResults = _faceDetector.Forward(array);

        foreach( var face in detectResults )
        {
            if ( !face.Box.IsEmpty )
            {
                // landmarks
                var points = _faceLandmarksExtractor.Forward(array, face.Box);
                var angle = points.GetRotationAngle();

                // alignment
                var aligned = FaceLandmarksExtractor.Align(array, face.Box, angle);
                yield return new FaceONNXFace
                {
                    Embeddings = _faceEmbedder.Forward(aligned),
                    Rectangle = new Rectangle(face.Box.X, face.Box.Y, face.Box.Width, face.Box.Height)
                };
            }
        }
    }
    
    /// <summary>
    ///     Detect faces
    /// </summary>
    /// <param name="image"></param>
    /// <returns></returns>
    public async Task<List<Face>> DetectFaces(Image<Rgb24> image, IImageProcessor imageProcessor)
    {
        var faces = new List<Face>();
        
        var watch = new Stopwatch("AzureFace");

        try
        {
            var detectedFaces = GetFacesFromImage(image);

            foreach( var face in detectedFaces )
            {
                // For each result, loop through and see if we have a match
                var proto = _embeddings.FromSimilarity(face.Embeddings);
                var label = proto.Item1;
                var similarity = proto.Item2;

                if( similarity > 0.75 )
                {
                    // Looks like we have a match. Yay!
                    face.PersonId = label;
                }
                else
                {
                    // No match, so create a new person GUID
                    face.PersonId = Guid.NewGuid().ToString();
                    // Add it to the embeddings DB
                    _embeddings.Add( face.Embeddings, face.PersonId);
                }
            }
        }
        catch ( Exception ex )
        {
            Logging.LogError($"Exception during FaceONNX face detection: {ex}");
        }

        watch.Stop();

        if ( faces.Any() )
            Logging.Log($"  FaceONNAX Detected {faces.Count()} faces in {watch.ElapsedTime}ms");
        
        return faces;
    }
    
    public class Face
    {
        public Guid? PersonId { get; set; }
        public int Left { set; get; }
        public int Top { set; get; }
        public int Width { set; get; }
        public int Height { set; get; }
    }
}