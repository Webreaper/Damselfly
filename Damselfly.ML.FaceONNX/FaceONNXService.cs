using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Damselfly.Core.Utils;
using Damselfly.Core.Utils.ML;
using Damselfly.Shared.Utils;
using FaceEmbeddingsClassification;
using FaceONNX;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Damselfly.ML.FaceONNX;

public class FaceONNXService : IDisposable
{
    private readonly ILogger<FaceONNXService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
        
    private FaceDetector _faceDetector;
    private FaceLandmarksExtractor _faceLandmarksExtractor;
    private FaceEmbedder _faceEmbedder;
    private Embeddings _embeddings;

    public FaceONNXService(IServiceScopeFactory scopeFactory, ILogger<FaceONNXService> logger )
    {
        _scopeFactory = scopeFactory;
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
    public void StartService()
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
            Logging.LogError($"Unable to start FaceONNX service: {ex.Message}");
            if ( ex.InnerException != null )
                Logging.LogError($"Inner exception: {ex.InnerException}");
        }
    }

    public void LoadFaceEmbeddings(Dictionary<string,float[]> embeddings)
    {
        _embeddings = new Embeddings(embeddings);
    }
    
    private class FaceONNXFace
    {
        public string PersonGuid { get; set; }
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
        
        // First, convert from an image, to an array of RGB float values. 
        image.ProcessPixelRows(pixelAccessor =>
        {
            for ( var y = 0; y < pixelAccessor.Height; y++ )
            {
                var row = pixelAccessor.GetRowSpan(y);
                for(var x = 0; x < pixelAccessor.Width; x++ )
                {
                    // ImageSharp pixel RGB are bytes from 0-255, but we 
                    // need to convert to tensor values of 0.0 - 1.0
                    array[0][y, x] = row[x].R / 255.0F;
                    array[1][y, x] = row[x].G / 255.0F;
                    array[2][y, x] = row[x].B / 255.0F;
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
        List<ImageDetectResult> detected = new();
        var watch = new Stopwatch("FaceOnnxDetection");

        try
        {
            var detectedFaces = GetFacesFromImage(image).ToList();

            foreach( var face in detectedFaces )
            {
                // For each result, loop through and see if we have a match
                var (personGuid, similarity) = _embeddings.FromSimilarity(face.Embeddings);

                bool isNewPerson = true;
                
                // TODO - maybe make this similarity threshold a preference?
                if( personGuid == null || similarity < 0.5 )
                {
                    // No match, so create a new person GUID
                    face.PersonGuid = Guid.NewGuid().ToString();
                    // Add it to the embeddings DB
                    _embeddings.Add(face.PersonGuid, face.Embeddings);
                }
                else
                {
                    // Looks like we have a match. Yay!
                    face.PersonGuid = personGuid;
                    isNewPerson = false;
                }

                detected.Add( new ImageDetectResult
                {
                    // TODO - need to disambiguate rectangles...
                    Rect = new System.Drawing.Rectangle(face.Rectangle.X, face.Rectangle.Y, 
                            face.Rectangle.Width, face.Rectangle.Height),
                    Score = face.Score,
                    Tag = "Face",
                    Service = "FaceONNX",
                    IsNewPerson = isNewPerson,
                    PersonGuid = face.PersonGuid,
                    Embeddings = face.Embeddings
                } );
            }
        }
        catch ( Exception ex )
        {
            Logging.LogError($"Exception during FaceONNX face detection: {ex}");
        }

        watch.Stop();

        if ( detected.Any() )
            Logging.Log($"  FaceONNAX Detected {detected.Count()} faces in {watch.ElapsedTime}ms");

        return detected;
    }
}