using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Utils;
using Damselfly.Core.Utils.ML;
using Damselfly.Shared.Utils;
using FaceEmbeddingsClassification;
using FaceONNX;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Damselfly.ML.FaceONNX;

public class FaceONNXService : IDisposable
{
    private readonly IConfigService _configService;
    private readonly ILogger<FaceONNXService> _logger;
    
    private FaceDetector _faceDetector;
    private FaceLandmarksExtractor _faceLandmarksExtractor;
    private FaceEmbedder _faceEmbedder;
    private Embeddings _embeddings;

    public FaceONNXService(IConfigService configService, ILogger<FaceONNXService> logger )
    {
        _configService = configService;
        _logger = logger;
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
        if( _embeddings == null )
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
    }

    private class FaceONNXFace
    {
        public string? PersonId { get; set; }
        public Rectangle Rectangle { get; set; }
        public float[] Embeddings { get; set; }
        public float Score { get; set; }
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

        FaceDetectionResult[] detectResults;
        try
        {
            detectResults = _faceDetector.Forward(array);
        }
        catch( Exception ex )
        {
            _logger.LogError($"Unexpected exception during FaceONNX detection: {ex}");
            throw;
        }

        foreach( var face in detectResults )
        {
            if ( !face.Box.IsEmpty )
            {
                if( face.Score < 0.985 )
                    continue;
                
                // landmarks
                var points = _faceLandmarksExtractor.Forward(array, face.Box);
                var angle = points.GetRotationAngle();

                // alignment
                var aligned = FaceLandmarksExtractor.Align(array, face.Box, angle);
                yield return new FaceONNXFace
                {
                    Embeddings = _faceEmbedder.Forward(aligned),
                    Rectangle = new Rectangle(face.Box.X, face.Box.Y, face.Box.Width, face.Box.Height),
                    Score = face.Score
                };
            }
        }
    }
    
    /// <summary>
    ///     Detect faces
    /// </summary>
    /// <param name="image"></param>
    /// <returns></returns>
    public async Task<List<ImageDetectResult>> DetectFaces(Image<Rgb24> image)
    {
        List<FaceONNXFace> detectedFaces;
        
        // TODO - Put this somewhere better
        await StartService();
        
        var watch = new Stopwatch("AzureFace");

        try
        {
            detectedFaces = GetFacesFromImage(image).ToList();

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
            detectedFaces = new();
        }

        watch.Stop();

        if ( detectedFaces.Any() )
            Logging.Log($"  FaceONNAX Detected {detectedFaces.Count()} faces in {watch.ElapsedTime}ms");

        // Convert to the caller's type. 
        var result = detectedFaces.Select( x => new ImageDetectResult
        {
            // TODO - need to disambiguate rectangles...
            Rect = new System.Drawing.Rectangle(x.Rectangle.X, x.Rectangle.Y, x.Rectangle.Width, x.Rectangle.Height),
            Score = x.Score,
            Tag = "Face",
            Service = "FaceONNX"
        }).ToList();
        
        return result;
    }
}