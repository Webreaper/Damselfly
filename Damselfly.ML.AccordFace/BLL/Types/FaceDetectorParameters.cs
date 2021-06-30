using Accord.Vision.Detection;

namespace Damselfly.ML.Face.Accord
{
    internal class FaceDetectorParameters
    {
        public float ScalingFactor { get; private set; }
        public ObjectDetectorScalingMode ScalingMode { get; private set; }
        public ObjectDetectorSearchMode SearchMode { get; private set; }
        public bool UseParallelProcessing { get; private set; }
        public int MinimumSize { get; private set; }
        public int Suppression { get; private set; }

        public bool IsValid { get; private set; }

        private FaceDetectorParameters(float scalingFactor, int minimumSize, ObjectDetectorScalingMode objectDetectorScalingMode,
            ObjectDetectorSearchMode objectDetectorSearchMode, bool useParallelProcessing, int suppression, bool isValid)
        {
            ScalingFactor = scalingFactor;
            MinimumSize = minimumSize;
            ScalingMode = objectDetectorScalingMode;
            SearchMode = objectDetectorSearchMode;
            UseParallelProcessing = useParallelProcessing;
            Suppression = suppression;
            IsValid = isValid;
        }

        public static FaceDetectorParameters Create(float scalingFactor, int minimumSize, ObjectDetectorScalingMode objectDetectorScalingMode,
            ObjectDetectorSearchMode objectDetectorSearchMode, bool useParallelProcessing, int suppression) =>
                new FaceDetectorParameters(scalingFactor, minimumSize, objectDetectorScalingMode, objectDetectorSearchMode, useParallelProcessing, suppression, true);
    }
}