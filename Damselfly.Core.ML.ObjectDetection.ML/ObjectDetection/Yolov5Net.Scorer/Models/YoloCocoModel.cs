using System.Collections.Generic;
using Microsoft.ML.OnnxRuntime;
using Yolov5Net.Scorer.Models.Abstract;

namespace Yolov5Net.Scorer.Models
{
    public class YoloCocoModel : YoloModel
    {
        public override int Width { get; } = 640;
        public override int Height { get; } = 640;
        public override int Depth { get; } = 3;

        public override int Dimensions { get; } = 85;

        public override float[] Strides { get; } = new float[] { 8, 16, 32 };

        public override float[][][] Anchors { get; } = new float[][][]
        {
            new float[][] { new float[] { 010, 13 }, new float[] { 016, 030 }, new float[] { 033, 023 } },
            new float[][] { new float[] { 030, 61 }, new float[] { 062, 045 }, new float[] { 059, 119 } },
            new float[][] { new float[] { 116, 90 }, new float[] { 156, 198 }, new float[] { 373, 326 } }
        };

        public override int[] Shapes { get; } = new int[] { 80, 40, 20 };

        public override float Confidence { get; } = 0.20f;
        public override float MulConfidence { get; } = 0.25f;
        public override float Overlap { get; } = 0.45f;

        
        //public override string Weights { get; } = "assets/weights/yolov2-coco-9.onnx";
        public override string ModelPath { get; } = "./Models/yolov5s.onnx";

        public override string[] OutputNames { get; } = new[] { "561" };

        public override bool UseDetect { get; } = true;

        public YoloCocoModel()
        {
            Labels = new List<YoloLabel>()
            {
                new YoloLabel { Id = 1, Name = "person" },
                new YoloLabel { Id = 2, Name = "bicycle" },
                new YoloLabel { Id = 3, Name = "car" },
                new YoloLabel { Id = 4, Name = "motorcycle" },
                new YoloLabel { Id = 5, Name = "airplane" },
                new YoloLabel { Id = 6, Name = "bus" },
                new YoloLabel { Id = 7, Name = "train" },
                new YoloLabel { Id = 8, Name = "truck" },
                new YoloLabel { Id = 9, Name = "boat" },
                new YoloLabel { Id = 10, Name = "traffic light" },
                new YoloLabel { Id = 11, Name = "fire hydrant" },
                new YoloLabel { Id = 12, Name = "stop sign" },
                new YoloLabel { Id = 13, Name = "parking meter" },
                new YoloLabel { Id = 14, Name = "bench" },
                new YoloLabel { Id = 15, Name = "bird" },
                new YoloLabel { Id = 16, Name = "cat" },
                new YoloLabel { Id = 17, Name = "dog" },
                new YoloLabel { Id = 18, Name = "horse" },
                new YoloLabel { Id = 19, Name = "sheep" },
                new YoloLabel { Id = 20, Name = "cow" },
                new YoloLabel { Id = 21, Name = "elephant" },
                new YoloLabel { Id = 22, Name = "bear" },
                new YoloLabel { Id = 23, Name = "zebra" },
                new YoloLabel { Id = 24, Name = "giraffe" },
                new YoloLabel { Id = 25, Name = "backpack" },
                new YoloLabel { Id = 26, Name = "umbrella" },
                new YoloLabel { Id = 27, Name = "handbag" },
                new YoloLabel { Id = 28, Name = "tie" },
                new YoloLabel { Id = 29, Name = "suitcase" },
                new YoloLabel { Id = 30, Name = "frisbee" },
                new YoloLabel { Id = 31, Name = "skis" },
                new YoloLabel { Id = 32, Name = "snowboard" },
                new YoloLabel { Id = 33, Name = "sports ball" },
                new YoloLabel { Id = 34, Name = "kite" },
                new YoloLabel { Id = 35, Name = "baseball bat" },
                new YoloLabel { Id = 36, Name = "baseball glove" },
                new YoloLabel { Id = 37, Name = "skateboard" },
                new YoloLabel { Id = 38, Name = "surfboard" },
                new YoloLabel { Id = 39, Name = "tennis racket" },
                new YoloLabel { Id = 40, Name = "bottle" },
                new YoloLabel { Id = 41, Name = "wine glass" },
                new YoloLabel { Id = 42, Name = "cup" },
                new YoloLabel { Id = 43, Name = "fork" },
                new YoloLabel { Id = 44, Name = "knife" },
                new YoloLabel { Id = 45, Name = "spoon" },
                new YoloLabel { Id = 46, Name = "bowl" },
                new YoloLabel { Id = 47, Name = "banana" },
                new YoloLabel { Id = 48, Name = "apple" },
                new YoloLabel { Id = 49, Name = "sandwich" },
                new YoloLabel { Id = 50, Name = "orange" },
                new YoloLabel { Id = 51, Name = "broccoli" },
                new YoloLabel { Id = 52, Name = "carrot" },
                new YoloLabel { Id = 53, Name = "hot dog" },
                new YoloLabel { Id = 54, Name = "pizza" },
                new YoloLabel { Id = 55, Name = "donut" },
                new YoloLabel { Id = 56, Name = "cake" },
                new YoloLabel { Id = 57, Name = "chair" },
                new YoloLabel { Id = 58, Name = "couch" },
                new YoloLabel { Id = 59, Name = "potted plant" },
                new YoloLabel { Id = 60, Name = "bed" },
                new YoloLabel { Id = 61, Name = "dining table" },
                new YoloLabel { Id = 62, Name = "toilet" },
                new YoloLabel { Id = 63, Name = "tv" },
                new YoloLabel { Id = 64, Name = "laptop" },
                new YoloLabel { Id = 65, Name = "mouse" },
                new YoloLabel { Id = 66, Name = "remote" },
                new YoloLabel { Id = 67, Name = "keyboard" },
                new YoloLabel { Id = 68, Name = "cell phone" },
                new YoloLabel { Id = 69, Name = "microwave" },
                new YoloLabel { Id = 70, Name = "oven" },
                new YoloLabel { Id = 71, Name = "toaster" },
                new YoloLabel { Id = 72, Name = "sink" },
                new YoloLabel { Id = 73, Name = "refrigerator" },
                new YoloLabel { Id = 74, Name = "book" },
                new YoloLabel { Id = 75, Name = "clock" },
                new YoloLabel { Id = 76, Name = "vase" },
                new YoloLabel { Id = 77, Name = "scissors" },
                new YoloLabel { Id = 78, Name = "teddy bear" },
                new YoloLabel { Id = 79, Name = "hair drier" },
                new YoloLabel { Id = 80, Name = "toothbrush" }
            };
        }
    }
}
