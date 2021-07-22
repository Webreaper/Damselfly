using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Damselfly.Core.Utils;
using Damselfly.Core.Utils.Constants;
using Damselfly.Core.Interfaces;
using System.Net.Http.Headers;
using System.Reflection;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using System.IO;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using System.Drawing;
using Damselfly.ML.AzureFace;

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
        private DateTime? _lastTrained;
        private int _persistedFaces;
        private TransThrottle _transThrottle;
        private const string GroupName = "Damselfly Faces";
        private const string GroupId = "damselflyfaces";
        private const string RECOGNITION_MODEL = RecognitionModel.Recognition04;

        // TODO: Look at PersonDirectory

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
            _transThrottle = new TransThrottle(20);

            var endpoint = configService.Get(ConfigSettings.AzureEndpoint);
            var key = configService.Get(ConfigSettings.AzureApiKey);

            if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(key))
            {
                // We have config and have set up the service, now figure out what we're going to use it for
                _detectionType = configService.Get(ConfigSettings.AzureDetectionType, AzureDetection.Disabled);
            }
            else
            {
                // No config, so we're disabled.
                _detectionType = AzureDetection.Disabled;
            }

            if( _detectionType != AzureDetection.Disabled )
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
            }
        }

        /// <summary>
        /// Discard the Person group persisted on MSFT's server, along with
        /// all the trained data associated with it. Use with care - if
        /// this is done at the wrong time, all face-recognition will fail.
        /// </summary>
        /// <returns></returns>
        public async Task ClearAllFaceData()
        {
            // TEMP WHILE DEBUGGING
            await _transThrottle.Run( "Delete", _faceClient.PersonGroup.DeleteAsync(GroupId) );
        }

        /// <summary>
        /// Initialise the face training service
        /// TODO: Need to ensure we can initialise this after startup,
        /// when the user specifies the new config.
        /// </summary>
        /// <returns></returns>
        public void StartService()
        {
            if (_faceClient != null && _detectionType != AzureDetection.Disabled)
            {
                try
                {
                    InitializeAzureService().Wait();
                }
                catch( Exception ex )
                {
                    Logging.LogError($"Unable to start Azure service: {ex.Message}");
                }
            }
        }

        private async Task InitializeAzureService()
        {
            Logging.Log("Starting Azure Face Service...");
            bool exists = false;
            try
            {
                var group = await _transThrottle.Call("GetGroup", _faceClient.PersonGroup.GetAsync(GroupId));

                if (group != null)
                {
                    Logging.Log($"Azure - Found existing Face Group: {GroupId}");
                    exists = true;

                    var list = await _transThrottle.Call("ListGroup", _faceClient.PersonGroupPerson.ListAsync(GroupId));
                    _persistedFaces = list.SelectMany(x => x.PersistedFaceIds).Count();
                }
            }
            catch (APIErrorException ex)
            {
                Logging.LogWarning($"Failed to initialise Azure Group: {ex.Message} [{ex.Response.Content}]");
                exists = false;
            }

            if( exists )
            {
                // See if it's been trained yet.
                try
                {
                    // Get the training status and group face list, and cache them
                    // so we don't have to requery, using up valuable API calls.
                    var trainingStatus = await _transThrottle.Call("GetTrainingStatus", _faceClient.PersonGroup.GetTrainingStatusAsync(GroupId));
                    _lastTrained = trainingStatus.LastSuccessfulTraining;
                }
                catch (APIErrorException ex)
                {
                    Logging.LogWarning($"Facegroup has not been trained ({ex.Message}).");
                }
            }

            if (!exists)
            {
                try
                {
                    await _transThrottle.Run("CreateGroup", _faceClient.PersonGroup.CreateAsync(GroupId, GroupName, recognitionModel: RECOGNITION_MODEL));
                    Logging.Log($"Created Azure person group: {GroupId}");
                    exists = true;
                }
                catch (Exception ex)
                {
                    Logging.LogError($"Failed to create person group: {ex.Message}");
                }
            }

            if (!exists)
            {
                Logging.LogError("Unable to create Azure face group. Azure face detection will be disabled.");
                _faceClient = null;
                _detectionType = AzureDetection.Disabled;
            }
        }

        /// <summary>
        /// Push the image into a memorystream and send it to the Azure service.
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private async Task<IList<DetectedFace>> AzureDetect( Bitmap image )
        {
            using var memoryStream = new MemoryStream();
            image.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);
            memoryStream.Seek(0, SeekOrigin.Begin);

            Logging.LogVerbose($"Calling Azure Detect...");

            var detectedFaces = await _transThrottle.Call("Detect", _faceClient.Face.DetectWithStreamAsync(memoryStream, true, true, _attributes, recognitionModel: RECOGNITION_MODEL));

            return detectedFaces;
        }

        /// <summary>
        /// Detect faces
        /// </summary>
        /// <param name="imageFilePath"></param>
        /// <returns></returns>
        public async Task<List<Face>> DetectFaces( Bitmap sourceImage )
        {
            var faces = new List<Face>();

            if (_faceClient != null && _detectionType != AzureDetection.Disabled )
            {
                var watch = new Stopwatch("AzureFace");

                try
                {
                    var detectedFaces = await AzureDetect(sourceImage);

                    if (detectedFaces.Any())
                    {
                        faces = await IdentifyOrCreateFaces(detectedFaces);

                        bool needTraining = false;

                        foreach (var face in faces)
                        {
                            MemoryStream memoryStream = SaveFaceThumb(sourceImage, face);

                            var persistedFace = await _transThrottle.Call("AddFace", _faceClient.PersonGroupPerson.AddFaceFromStreamAsync(GroupId, face.PersonId.Value, memoryStream));
                            // TODO: Do something with the persisted face (save to the DB?!)

                            needTraining = true;
                        }

                        if (needTraining)
                        {
                            // We've added new faces/images, so train.
                            await Train();
                        }
                    }
                }
                catch (APIErrorException ex)
                {
                    Logging.LogError($"Azure face error: {ex.Response.Content}");
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

        /// <summary>
        /// Crop the face from the original image, and save it locally.
        /// </summary>
        /// <param name="sourceImage"></param>
        /// <param name="face"></param>
        /// <returns></returns>
        private static MemoryStream SaveFaceThumb(Bitmap sourceImage, Face face)
        {
            // It's a new face. Extract the face rect.
            var faceRect = new Rectangle(face.Left, face.Top, face.Width, face.Height);

            // Save the faces
            var faceBitmap = GetCroppedFaceFromImage(sourceImage, faceRect);
            var memoryStream = new MemoryStream();
            faceBitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);
            memoryStream.Seek(0, SeekOrigin.Begin);

            if (System.Diagnostics.Debugger.IsAttached)
            {
                // TODO: Probably want to write these to a 'thumbs' style folder, so we can resubmit
                // the face training data if we need to for any reason.
                string faceFile = $"/Users/markotway/Desktop/Faces/{face.PersonId.Value}.jpg";
                faceBitmap.Save(faceFile);
            }

            return memoryStream;
        }

        /// <summary>
        /// Used by the caller to periodically log transaction usage
        /// </summary>
        /// <returns></returns>
        public int GetAndResetTransCount()
        {
            return _transThrottle.ResetTotalTransactions();
        }

        /// <summary>
        /// Attempt to identify the faces from the trained set we have now.
        /// </summary>
        /// <param name="detectedFaces"></param>
        /// <returns></returns>
        private async Task<List<Face>> IdentifyOrCreateFaces( IList<DetectedFace> detectedFaces )
        {
            List<Face> faces = detectedFaces.Select(x => new Face
                {
                    FaceId = x.FaceId,
                    Left = x.FaceRectangle.Left,
                    Top = x.FaceRectangle.Top,
                    Width = x.FaceRectangle.Width,
                    Height = x.FaceRectangle.Height,
                    Emotion = GetTopEmotion(x.FaceAttributes)
                }).ToList();

            // If we have any trained faces yet, try and identify
            if (_persistedFaces > 0 && _lastTrained.HasValue)
            {
                var faceIdsToMatch = detectedFaces.Where(x => x.FaceId.HasValue).Select(x => x.FaceId.Value).ToList();

                var matches = await _transThrottle.Call( "Identify", _faceClient.Face.IdentifyAsync(faceIdsToMatch, personGroupId: GroupId, maxNumOfCandidatesReturned: 3) );

                foreach (var match in matches)
                {
                    // Find the face for this match
                    var face = faces.FirstOrDefault(x => x.FaceId == match.FaceId);

                    if (match.Candidates?.Count > 0)
                    {
                        // We got a match. Pick the first one for now.
                        face.PersonId = match.Candidates[0].PersonId;
                        Logging.Log($"Identified person {face.PersonId.Value}.");
                    }
                }
            }

            // Okay, so having found all the existing faces, now we go through and create
            // Person objects for any faces we didn't recognise.
            var newFaces = faces.Where(x => !x.PersonId.HasValue).ToList();

            foreach (var newFace in newFaces)
            {
                // It's somebody new - create the person
                var createdPerson = await _transThrottle.Call( "CreatePerson", _faceClient.PersonGroupPerson.CreateAsync(GroupId, Guid.NewGuid().ToString()) );

                // Keep track of this - we could call GetGroup.List, but that uses up a transaction...
                _persistedFaces++;

                newFace.PersonId = createdPerson.PersonId;
                Logging.Log($"Created new person {newFace.PersonId.Value}.");
            }

            return faces;
        }

        /// <summary>
        /// Extract the cropped area of a specific face from the source image
        /// </summary>
        /// <param name="sourceImage"></param>
        /// <param name="cropRect"></param>
        /// <returns></returns>
        public static Bitmap GetCroppedFaceFromImage(Bitmap sourceImage, Rectangle cropRect)
        {
            // Inflate by 10% to ensure we capture the whole face. 
            var inflate = new Size( (int)(cropRect.Width * 0.1), (int)(cropRect.Height * 0.1) );
            cropRect.Inflate( inflate );

            Bitmap nb = new Bitmap(cropRect.Width, cropRect.Height);
            using (Graphics g = Graphics.FromImage(nb))
            {
                g.DrawImage(sourceImage, -cropRect.X, -cropRect.Y);
                return nb;
            }
        }

        /// <summary>
        /// Train the model.
        /// </summary>
        /// <returns></returns>
        private async Task Train()
        {
            try
            {
                await _transThrottle.Run("Train", _faceClient.PersonGroup.TrainAsync(GroupId));

                TrainingStatus trainingStatus = null;
                while (true)
                {
                    await Task.Delay(1000);

                    trainingStatus = await _transThrottle.Call("GetTrainingStatus", _faceClient.PersonGroup.GetTrainingStatusAsync(GroupId));

                    if (trainingStatus.Status != TrainingStatusType.Running)
                    {
                        if (trainingStatus.Status == TrainingStatusType.Succeeded)
                            Logging.LogVerbose($"Training succeeded on {GroupId}");
                        if (trainingStatus.Status == TrainingStatusType.Failed)
                            Logging.LogError($"Training failed on {GroupId}");

                        _lastTrained = trainingStatus.LastSuccessfulTraining;
                        break;
                    }

                    Logging.Log("Training in progress...");
                }
            }
            catch( Exception ex )
            {
                Logging.LogError($"Exception during Azure training: {ex.Message}");
            }
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
            public Guid? PersonId { get; set; }
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
