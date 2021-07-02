using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Damselfly.Core.Utils;
using Damselfly.Core.Utils.Constants;
using Damselfly.Core.Interfaces;
using System.Net.Http.Headers;
using System.Reflection;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using System.IO;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

namespace Damselfly.ML.Face.Azure
{
    /// <summary>
    /// https://docs.microsoft.com/en-us/azure/cognitive-services/face/quickstarts/client-libraries?tabs=visual-studio&pivots=programming-language-csharp
    /// </summary>
    public class AzureFaceService
    {
        private HttpClient _httpClient;
        private FaceClient _faceClient;
        private IList<FaceAttributeType> _attributes;
        private AzureDetection _detectionType;

        public AzureDetection DetectionType
        {
            get { return _detectionType; }
        }

        public enum AzureDetection
        {
            Disabled = 0,
            AllImages = 1,
            ImagesWithFaces = 2
        }

        public AzureFaceService(IConfigService configService)
        {
            var endpoint = configService.Get(ConfigSettings.AzureEndpoint);
            var key = configService.Get(ConfigSettings.AzureApiKey);

            if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(key))
            {
                _httpClient = new HttpClient();
                var agent = new ProductInfoHeaderValue("Damselfly", $"{Assembly.GetExecutingAssembly().GetName().Version}");
                _httpClient.DefaultRequestHeaders.UserAgent.Add(agent);

                ApiKeyServiceClientCredentials creds = new ApiKeyServiceClientCredentials(key);
                _faceClient = new FaceClient(creds, _httpClient, true);
                _faceClient.Endpoint = endpoint;

                _attributes = new FaceAttributeType[]
                {
                    FaceAttributeType.Gender,
                    FaceAttributeType.Age,
                    FaceAttributeType.Smile,
                    FaceAttributeType.Emotion,
                    FaceAttributeType.Glasses,
                    FaceAttributeType.Hair,
                    FaceAttributeType.FacialHair,
                    FaceAttributeType.HeadPose
                    // These are currently not supported.
                    // FaceAttributeType.Mask
                    // FaceAttributeType.Makeup,
                };

                // We have config and have set up the service, now figure out what we're going to use it for
                _detectionType = configService.Get(ConfigSettings.AzureDetectionType, AzureDetection.Disabled);
            }
            else
            {
                // No config, so we're disabled.
                _detectionType = AzureDetection.Disabled;
            }
        }

        public async Task<List<Face>> DetectFaces( FileInfo imageFilePath )
        {
            var faces = new List<Face>();

            if (_faceClient != null && _detectionType != AzureDetection.Disabled )
            {
                var watch = new Stopwatch("AzureFace");

                try
                {
                    using (Stream imageFileStream = File.OpenRead(imageFilePath.FullName))
                    {
                        var detectedFaces = await _faceClient.Face.DetectWithStreamAsync(imageFileStream, true, true, _attributes);

                        faces = detectedFaces.Select(x => new Face
                        {
                            FaceId = x.FaceId,
                            Left = x.FaceRectangle.Left,
                            Top = x.FaceRectangle.Top,
                            Width = x.FaceRectangle.Width,
                            Height = x.FaceRectangle.Height,
                            Emotion = GetTopEmotion(x.FaceAttributes)
                        }).ToList();
                    }
                }
                catch ( Exception ex )
                {
                    Logging.LogError($"Exception during Azure face detection: {ex}");
                }

                watch.Stop();

                if( faces.Any() )
                    Logging.Log($"Azure Detected {faces.Count()} faces in {watch.ElapsedTime}ms");
            }
            else
            {
                Logging.LogVerbose($"Azure Face Service was not configured.");
            }

            return faces;
        }

        private static string GetTopEmotion( FaceAttributes attributes )
        {
            var emotionValues = new Dictionary<string, double> {
                { "Anger", attributes.Emotion.Anger},
                {"Contempt", attributes.Emotion.Contempt},
                {"Disgust", attributes.Emotion.Disgust},
                {"Fear", attributes.Emotion.Fear},
                {"Happiness", attributes.Emotion.Happiness},
                {"Neutral", attributes.Emotion.Neutral},
                {"Sadness", attributes.Emotion.Sadness},
                {"Surprise", attributes.Emotion.Surprise}
            };

            return emotionValues.MaxBy(x => x.Value).Key;
        }

        public class DataPayload
        {
            public string Url { get; set; }
        }

        public class Face
        {
            public Guid? FaceId { get; set; }
            public int Left { set; get; }
            public int Top { set; get; }
            public int Width { set; get; }
            public int Height { set; get; }
            public string Emotion { get; set; }
        }

        public class FaceAttribute
        {
            public string Gender { get; set; }
            public float Age { get; set; }
            public string Glasses { get; set; }
            public Emotion Emotion { get; set; }
        }

        public class Emotion
        {
            public float Anger { get; set; }
            public float Contempt { get; set; }
            public float Disgust { get; set; }
            public float Fear { get; set; }
            public float Happiness { get; set; }
            public float Neutral { get; set; }
            public float Sadness { get; set; }
            public float Surprise { get; set; }
        }
    }
}
