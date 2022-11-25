using System.Collections.Generic;
using Yolov5Net.Scorer.Models.Abstract;

namespace Yolov5Net.Scorer.Models;

public class YoloCocoModel : YoloModel
{
    public YoloCocoModel()
    {
        Labels = new List<YoloLabel>
        {
            new() { Id = 1, Name = "person" },
            new() { Id = 2, Name = "bicycle" },
            new() { Id = 3, Name = "car" },
            new() { Id = 4, Name = "motorcycle" },
            new() { Id = 5, Name = "airplane" },
            new() { Id = 6, Name = "bus" },
            new() { Id = 7, Name = "train" },
            new() { Id = 8, Name = "truck" },
            new() { Id = 9, Name = "boat" },
            new() { Id = 10, Name = "traffic light" },
            new() { Id = 11, Name = "fire hydrant" },
            new() { Id = 12, Name = "stop sign" },
            new() { Id = 13, Name = "parking meter" },
            new() { Id = 14, Name = "bench" },
            new() { Id = 15, Name = "bird" },
            new() { Id = 16, Name = "cat" },
            new() { Id = 17, Name = "dog" },
            new() { Id = 18, Name = "horse" },
            new() { Id = 19, Name = "sheep" },
            new() { Id = 20, Name = "cow" },
            new() { Id = 21, Name = "elephant" },
            new() { Id = 22, Name = "bear" },
            new() { Id = 23, Name = "zebra" },
            new() { Id = 24, Name = "giraffe" },
            new() { Id = 25, Name = "backpack" },
            new() { Id = 26, Name = "umbrella" },
            new() { Id = 27, Name = "handbag" },
            new() { Id = 28, Name = "tie" },
            new() { Id = 29, Name = "suitcase" },
            new() { Id = 30, Name = "frisbee" },
            new() { Id = 31, Name = "skis" },
            new() { Id = 32, Name = "snowboard" },
            new() { Id = 33, Name = "sports ball" },
            new() { Id = 34, Name = "kite" },
            new() { Id = 35, Name = "baseball bat" },
            new() { Id = 36, Name = "baseball glove" },
            new() { Id = 37, Name = "skateboard" },
            new() { Id = 38, Name = "surfboard" },
            new() { Id = 39, Name = "tennis racket" },
            new() { Id = 40, Name = "bottle" },
            new() { Id = 41, Name = "wine glass" },
            new() { Id = 42, Name = "cup" },
            new() { Id = 43, Name = "fork" },
            new() { Id = 44, Name = "knife" },
            new() { Id = 45, Name = "spoon" },
            new() { Id = 46, Name = "bowl" },
            new() { Id = 47, Name = "banana" },
            new() { Id = 48, Name = "apple" },
            new() { Id = 49, Name = "sandwich" },
            new() { Id = 50, Name = "orange" },
            new() { Id = 51, Name = "broccoli" },
            new() { Id = 52, Name = "carrot" },
            new() { Id = 53, Name = "hot dog" },
            new() { Id = 54, Name = "pizza" },
            new() { Id = 55, Name = "donut" },
            new() { Id = 56, Name = "cake" },
            new() { Id = 57, Name = "chair" },
            new() { Id = 58, Name = "couch" },
            new() { Id = 59, Name = "potted plant" },
            new() { Id = 60, Name = "bed" },
            new() { Id = 61, Name = "dining table" },
            new() { Id = 62, Name = "toilet" },
            new() { Id = 63, Name = "tv" },
            new() { Id = 64, Name = "laptop" },
            new() { Id = 65, Name = "mouse" },
            new() { Id = 66, Name = "remote" },
            new() { Id = 67, Name = "keyboard" },
            new() { Id = 68, Name = "cell phone" },
            new() { Id = 69, Name = "microwave" },
            new() { Id = 70, Name = "oven" },
            new() { Id = 71, Name = "toaster" },
            new() { Id = 72, Name = "sink" },
            new() { Id = 73, Name = "refrigerator" },
            new() { Id = 74, Name = "book" },
            new() { Id = 75, Name = "clock" },
            new() { Id = 76, Name = "vase" },
            new() { Id = 77, Name = "scissors" },
            new() { Id = 78, Name = "teddy bear" },
            new() { Id = 79, Name = "hair drier" },
            new() { Id = 80, Name = "toothbrush" }
        };
    }

    public override int Width { get; } = 640;
    public override int Height { get; } = 640;
    public override int Depth { get; } = 3;

    public override int Dimensions { get; } = 85;

    public override float[] Strides { get; } = { 8, 16, 32 };

    public override float[][][] Anchors { get; } =
    {
        new[] { new float[] { 010, 13 }, new float[] { 016, 030 }, new float[] { 033, 023 } },
        new[] { new float[] { 030, 61 }, new float[] { 062, 045 }, new float[] { 059, 119 } },
        new[] { new float[] { 116, 90 }, new float[] { 156, 198 }, new float[] { 373, 326 } }
    };

    public override int[] Shapes { get; } = { 80, 40, 20 };

    public override float Confidence { get; } = 0.20f;
    public override float MulConfidence { get; } = 0.25f;
    public override float Overlap { get; } = 0.45f;


    //public override string Weights { get; } = "assets/weights/yolov2-coco-9.onnx";
    public override string ModelPath { get; } = "./Models/yolov5s.onnx";

    public override string[] OutputNames { get; } = { "561" };

    public override bool UseDetect { get; } = true;
}