using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using Damselfly.Core.Constants;
using Damselfly.Core.Interfaces;
using Damselfly.Core.ScopedServices.Interfaces;
using Damselfly.Core.Utils;
using Damselfly.Shared.Utils;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

namespace Damselfly.ML.Face.Azure;

/// <summary>
///     https://docs.microsoft.com/en-us/azure/cognitive-services/face/quickstarts/client-libraries?tabs=visual-studio
///     &pivots=programming-language-csharp
/// </summary>
public class AzureFaceService
{
    private const string GroupId = "damselflypersondir";

    // Assumine the newer/bigger numbers are better. 
    private const string RECOGNITION_MODEL = RecognitionModel.Recognition04;
    private const string DETECTION_MODEL = DetectionModel.Detection03;
    private readonly IList<FaceAttributeType> _attributes;
    private readonly IConfigService _configService;
    private readonly ITransactionThrottle _transThrottle;
    private FaceClient _faceClient;
    private HttpClient _httpClient;
    private int _persistedFaces;

    public AzureFaceService(IConfigService configService, ITransactionThrottle transThrottle)
    {
        _configService = configService;
        _transThrottle = transThrottle;

        // We have opted to not support a general-purpose system in the Face API that purports to infer
        // emotional states, gender, age, smile, facial hair, hair, and makeup.
        // Detection of these attributes will no longer be available to new customers beginning June 21, 2022
        // https://azure.microsoft.com/en-us/blog/responsible-ai-investments-and-safeguards-for-facial-recognition/?WT.mc_id=AI-MVP-5003365
        _attributes = new[]
        {
            FaceAttributeType.Glasses,
            FaceAttributeType.HeadPose
        };
    }

    public AzureDetection DetectionType { get; private set; }

    private void InitFromConfig()
    {
        var endpoint = _configService.Get(ConfigSettings.AzureEndpoint);
        var key = _configService.Get(ConfigSettings.AzureApiKey);

        if ( !string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(key) )
            // We have config and have set up the service, now figure out what we're going to use it for
            DetectionType = _configService.Get(ConfigSettings.AzureDetectionType, AzureDetection.Disabled);
        else
            // No config, so we're disabled.
            DetectionType = AzureDetection.Disabled;

        if ( DetectionType != AzureDetection.Disabled )
        {
            var useFreeTier = _configService.GetBool(ConfigSettings.AzureUseFreeTier, true);

            if ( useFreeTier )
                // Free tier allows 20 trans/min, and a max of 30k per month
                _transThrottle.SetLimits(20, 30000);
            else
                // Standard paid tier allows 10 trans/sec, and unlimited. But at 10 txn/sec,
                // the actual max is 30 million per month (about 26784000 per month). So
                // limit to 30 million.
                _transThrottle.SetLimits(600, 30000000);

            // This is a bit sucky - it basically ignores cert issues completely, which is a bit of a security risk.
            var clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
            {
                return true;
            };

            _httpClient = new HttpClient(clientHandler);
            var agent = new ProductInfoHeaderValue("Damselfly", $"{Assembly.GetExecutingAssembly().GetName().Version}");
            _httpClient.DefaultRequestHeaders.UserAgent.Add(agent);

            var creds = new ApiKeyServiceClientCredentials(key);
            _faceClient = new FaceClient(creds, _httpClient, true);
            _faceClient.Endpoint = endpoint;
        }
    }

    /// <summary>
    ///     Discard the Person group persisted on MSFT's server, along with
    ///     all the trained data associated with it. Use with care - if
    ///     this is done at the wrong time, all face-recognition will fail.
    /// </summary>
    /// <returns></returns>
    public async Task ClearAllFaceData()
    {
        // TEMP WHILE DEBUGGING
        await _transThrottle.Call("Delete", _faceClient.PersonDirectory.DeleteDynamicPersonGroupAsync(GroupId));
    }

    /// <summary>
    ///     Initialise the face training service
    /// </summary>
    /// <returns></returns>
    public async Task StartService()
    {
        try
        {
            InitFromConfig();

            if ( _faceClient != null && DetectionType != AzureDetection.Disabled ) await InitializeAzureService();
        }
        catch ( Exception ex )
        {
            Logging.LogError($"Unable to start Azure service: {ex.Message}");
            if ( ex.InnerException != null )
                Logging.LogError($"Inner exception: {ex.InnerException}");
        }
    }

    /// <summary>
    ///     Set up the service and query the state of the person directory etc
    /// </summary>
    /// <returns></returns>
    private async Task InitializeAzureService()
    {
        Logging.Log("Starting Azure Face Service...");
        var exists = false;
        try
        {
            var groups = await _transThrottle.Call("List", _faceClient.PersonDirectory.ListDynamicPersonGroupsAsync());

            if ( groups.Any(x => x.DynamicPersonGroupId == GroupId) )
            {
                var faces = await _transThrottle.Call("List", _faceClient.PersonDirectory.GetPersonsAsync());

                _persistedFaces = faces.Count();
                Logging.Log($"Azure directory currently contains {_persistedFaces} recognisable faces.");
                exists = true;
            }

            Logging.Log($"Azure - Found {_persistedFaces} in PersonDirectory Group: {GroupId}");
        }
        catch ( APIErrorException ex )
        {
            Logging.LogWarning($"Failed to initialise Azure Group: {ex.Message} [{ex.Response.Content}]");
            exists = false;
        }

        if ( !exists )
            try
            {
                var body = new DynamicPersonGroupCreateRequest
                {
                    Name = GroupId
                };
                var response = await _transThrottle.Call("CreateGroup",
                    _faceClient.PersonDirectory.CreateDynamicPersonGroupAsync(GroupId, body));

                // TODO: Use response.OperationLocation to wait for it to complete
                Logging.Log($"Created Azure person group: {GroupId}");
                exists = true;
            }
            catch ( Exception ex )
            {
                Logging.LogError($"Failed to create person group: {ex.Message}");
            }

        if ( !exists )
        {
            Logging.LogError("Unable to create Azure face group. Azure face detection will be disabled.");
            _faceClient = null;
            DetectionType = AzureDetection.Disabled;
        }
    }

    /// <summary>
    ///     Push the image into a memorystream and send it to the Azure service.
    /// </summary>
    /// <param name="image"></param>
    /// <returns></returns>
    private async Task<IList<DetectedFace>> AzureDetect(string imagePath)
    {
        using var fileStream = File.OpenRead(imagePath);

        Logging.LogVerbose("Calling Azure Face service...");

        var detectedFaces = await _transThrottle.Call("Detect",
            _faceClient.Face.DetectWithStreamAsync(fileStream, true, true, _attributes, RECOGNITION_MODEL));

        Logging.LogVerbose("Azure Face service call complete.");

        return detectedFaces;
    }

    /// <summary>
    ///     Detect faces
    /// </summary>
    /// <param name="imageFilePath"></param>
    /// <returns></returns>
    public async Task<List<Face>> DetectFaces(string imagePath, IImageProcessor imageProcessor)
    {
        var faces = new List<Face>();

        if ( _transThrottle.Disabled )
        {
            Logging.LogWarning("Azure has reached maximum monthly transactions, so is disabled.");
            return faces;
        }

        if ( _faceClient != null && DetectionType != AzureDetection.Disabled )
        {
            var watch = new Stopwatch("AzureFace");

            try
            {
                var detectedFaces = await AzureDetect(imagePath);

                if ( detectedFaces != null && detectedFaces.Any() )
                {
                    faces = await IdentifyOrCreateFaces(detectedFaces);

                    foreach ( var face in faces )
                    {
                        // Hopefully they'll improve this....
                        // https://docs.microsoft.com/en-us/answers/questions/494886/azure-faceclient-persondirectory-api-usage.html

                        if (face.Height >= 200 && face.Width >= 200)
                        {
                            using var stream = new MemoryStream();
                            await imageProcessor.CropImage(new FileInfo(imagePath), face.Left, face.Top, face.Width,
                                face.Height, stream);
                            stream.Seek(0, SeekOrigin.Begin);

                            if (stream != null)
                            {
                                try
                                {
                                    var persistedFace = await _transThrottle.Call("AddFace",
                                        _faceClient.PersonDirectory.AddPersonFaceFromStreamAsync(
                                            face.PersonId.ToString(),
                                            image: stream,
                                            recognitionModel: RECOGNITION_MODEL,
                                            detectionModel: DETECTION_MODEL));
                                }
                                catch (ErrorException ex)
                                {
                                    Logging.Log($"Exception AddFace: {ex.Message} : {ex.Response.Content}");
                                }
                            }
                            else
                            {
                                Logging.Log($"Unable to crop image for Azure: no supported image processor for {imagePath}");
                            }
                        }
                        else
                        {
                            Logging.Log($"Cropped face was too small for Azure face detection: {imagePath}");
                        }
                    }
                }
            }
            catch ( ErrorException ex )
            {
                Logging.LogError($"Azure face error: {ex.Response.Content}");
            }
            catch ( Exception ex )
            {
                Logging.LogError($"Exception during Azure face detection: {ex}");
            }

            watch.Stop();

            if ( faces.Any() )
                Logging.Log($"  Azure Detected {faces.Count()} faces in {watch.ElapsedTime}ms");

            _transThrottle.ProcessNewTransactions();
        }
        else
        {
            Logging.LogVerbose("Azure Face Service was not configured.");
        }

        return faces;
    }

    /// <summary>
    ///     Attempt to identify the faces from the trained set we have now.
    /// </summary>
    /// <param name="detectedFaces"></param>
    /// <returns></returns>
    private async Task<List<Face>> IdentifyOrCreateFaces(IList<DetectedFace> detectedFaces)
    {
        var faces = detectedFaces.Select(x => new Face
        {
            FaceId = x.FaceId,
            Left = x.FaceRectangle.Left,
            Top = x.FaceRectangle.Top,
            Width = x.FaceRectangle.Width,
            Height = x.FaceRectangle.Height,
            Emotion = GetTopEmotion(x.FaceAttributes)
        }).ToList();

        // If we have any trained faces yet, try and identify
        if ( _persistedFaces > 0 )
        {
            var faceIdsToMatch = detectedFaces.Where(x => x.FaceId.HasValue).Select(x => x.FaceId.Value).ToList();

            var matches = await _transThrottle.Call("Identify", _faceClient.Face.IdentifyAsync(faceIdsToMatch,
                personIds: new List<string> { "*" }, maxNumOfCandidatesReturned: 3));

            if ( matches != null )
                foreach ( var match in matches )
                {
                    // Find the face for this match
                    var face = faces.FirstOrDefault(x => x.FaceId == match.FaceId);

                    if ( match.Candidates?.Count > 0 )
                    {
                        // We got a match. Pick the first one for now.
                        face.PersonId = match.Candidates[0].PersonId;
                        face.Score = match.Candidates[0].Confidence;
                        // TODO: face.score = match.Candidates[0].Confidence;
                        Logging.Log($"Identified person {face.PersonId.Value}.");
                    }
                }
        }

        // Okay, so having found all the existing faces, now we go through and create
        // Person objects for any faces we didn't recognise.
        var newFaces = faces.Where(x => !x.PersonId.HasValue).ToList();

        foreach ( var newFace in newFaces )
        {
            var body = new EnrolledPerson { PersonId = newFace.PersonId, Name = $"Unknown{_persistedFaces}" };
            // It's somebody new - create the person
            var createdPerson =
                await _transThrottle.Call("CreatePerson", _faceClient.PersonDirectory.CreatePersonAsync(body));

            if ( createdPerson != null && createdPerson.Body != null )
            {
                // Keep track of this - we could call GetGroup.List, but that uses up a transaction...
                _persistedFaces++;


                newFace.PersonId = createdPerson.Body.PersonId;
                Logging.Log($"Created new person {newFace.PersonId.Value}.");
            }
            else
            {
                Logging.LogWarning("New person was not created from Azure. Possible API limit breach.");
            }
        }

        return faces;
    }

    private static string GetTopEmotion(FaceAttributes attributes)
    {
        var emotionValues = new Dictionary<string, double>
        {
            { "Anger", attributes.Emotion.Anger },
            { "Contempt", attributes.Emotion.Contempt },
            { "Disgust", attributes.Emotion.Disgust },
            { "Fear", attributes.Emotion.Fear },
            { "Happiness", attributes.Emotion.Happiness },
            { "Neutral", attributes.Emotion.Neutral },
            { "Sadness", attributes.Emotion.Sadness },
            { "Surprise", attributes.Emotion.Surprise }
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
        public Guid? PersonId { get; set; }
        public double Score { get; set; }
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