using Damselfly.Core.Utils;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using Yolov5Net.Scorer.Extensions;
using Yolov5Net.Scorer.Models.Abstract;

namespace Yolov5Net.Scorer
{
    /// <summary>
    /// Object detector.
    /// </summary>
    public class YoloScorer<T> where T : YoloModel
    {
        private readonly T _model;
        private readonly InferenceSession _inference;

        /// <summary>
        /// Outputs value between 0 and 1.
        /// </summary>
        private float Sigmoid(float value)
        {
            return 1 / (1 + MathF.Exp(-value));
        }

        /// <summary>
        /// Converts xywh bbox format to xyxy.
        /// </summary>
        private float[] Xywh2xyxy(float[] source)
        {
            var result = new float[4];

            result[0] = source[0] - source[2] / 2f;
            result[1] = source[1] - source[3] / 2f;
            result[2] = source[0] + source[2] / 2f;
            result[3] = source[1] + source[3] / 2f;

            return result;
        }

        /// <summary>
        /// Fits input to net format.
        /// </summary>
        private Bitmap ResizeImage(Image image)
        {
            PixelFormat format = image.PixelFormat;

            var result = new Bitmap(_model.Width, _model.Height, format);

            if( image.HorizontalResolution == 0 || image.VerticalResolution == 0 )
                result.SetResolution(300, 300);
            else
                result.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            var rect = new Rectangle(0, 0, _model.Width, _model.Height);

            using (var graphics = Graphics.FromImage(result))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                graphics.SmoothingMode = SmoothingMode.HighQuality;

                graphics.DrawImage(image, rect);
            }

            return result;
        }

        /// <summary>
        /// Extracts pixels into tensor for net input.
        /// </summary>
        private Tensor<float> ExtractPixels(Image image)
        {
            var bitmap = new Bitmap(image);

            var rectangle = new Rectangle(0, 0, image.Width, image.Height);

            BitmapData locked = bitmap.LockBits(rectangle, ImageLockMode.ReadOnly, image.PixelFormat);

            var tensor = new DenseTensor<float>(new[] { 1, 3, image.Height, image.Width });

            unsafe // speed up conversion by direct work with memory
            {
                for (int y = 0; y < locked.Height; y++)
                {
                    byte* row = (byte*)locked.Scan0 + (y * locked.Stride);

                    for (int x = 0; x < locked.Width; x++)
                    {
                        tensor[0, 0, y, x] = row[x * 3 + 0] / 255.0F;
                        tensor[0, 1, y, x] = row[x * 3 + 1] / 255.0F;
                        tensor[0, 2, y, x] = row[x * 3 + 2] / 255.0F;
                    }
                }

                bitmap.UnlockBits(locked);
            }

            return tensor;
        }

        /// <summary>
        /// Runs inference session.
        /// </summary>
        private DenseTensor<float>[] Inference(Image image)
        {
            Bitmap resized = null;

            if (image.Width != _model.Width || image.Height != _model.Height)
            {
                resized = ResizeImage(image); // fit image size to specified input size
            }

            var inputs = new List<NamedOnnxValue> // add image as onnx input
            {
                NamedOnnxValue.CreateFromTensor("images", ExtractPixels(resized ?? image))
            };

            var result = _inference.Run(inputs); // run inference session

            var output = new List<DenseTensor<float>>();

            foreach (var item in _model.OutputNames) // add outputs for processing
            {
                output.Add(result.First(x => x.Name == item).Value as DenseTensor<float>);
            };

            return output.ToArray();
        }

        /// <summary>
        /// Parses net output (detect) to predictions.
        /// </summary>
        private List<YoloPrediction> ParseDetect(DenseTensor<float> output, Image image)
        {
            var result = new List<YoloPrediction>();

            var (xGain, yGain) = (_model.Width / (float)image.Width, _model.Height / (float)image.Height);

            for (int i = 0; i < output.Length / _model.Dimensions; i++) // iterate tensor
            {
                if (output[0, i, 4] <= _model.Confidence) continue;

                for (int j = 5; j < _model.Dimensions; j++) // compute mul conf
                {
                    output[0, i, j] = output[0, i, j] * output[0, i, 4]; // conf = obj_conf * cls_conf
                }

                for (int k = 5; k < _model.Dimensions; k++)
                {
                    if (output[0, i, k] <= _model.MulConfidence) continue;

                    var xMin = (output[0, i, 0] - output[0, i, 2] / 2) / xGain; // top left x
                    var yMin = (output[0, i, 1] - output[0, i, 3] / 2) / yGain; // top left y
                    var xMax = (output[0, i, 0] + output[0, i, 2] / 2) / xGain; // bottom right x
                    var yMax = (output[0, i, 1] + output[0, i, 3] / 2) / yGain; // bottom right y

                    YoloLabel label = _model.Labels[k - 5];

                    var prediction = new YoloPrediction(label, output[0, i, k])
                    {
                        Rectangle = new RectangleF(xMin, yMin, xMax - xMin, yMax - yMin)
                    };

                    result.Add(prediction);
                }
            }

            return result;
        }

        /// <summary>
        /// Parses net outputs (sigmoid) to predictions.
        /// </summary>
        private List<YoloPrediction> ParseSigmoid(DenseTensor<float>[] output, Image image)
        {
            var result = new List<YoloPrediction>();

            var (xGain, yGain) = (_model.Width / (float)image.Width, _model.Height / (float)image.Height);

            for (int i = 0; i < output.Length; i++) // iterate outputs
            {
                int shapes = _model.Shapes[i]; // shapes per output

                for (int a = 0; a < _model.Anchors.Length; a++) // iterate anchors
                {
                    for (int y = 0; y < shapes; y++) // iterate rows
                    {
                        for (int x = 0; x < shapes; x++) // iterate columns
                        {
                            int offset = (shapes * shapes * a + shapes * y + x) * _model.Dimensions;

                            float[] buffer = output[i].Skip(offset).Take(_model.Dimensions).Select(Sigmoid).ToArray();

                            var objConfidence = buffer[4]; // extract object confidence

                            if (objConfidence < _model.Confidence) // check predicted object confidence
                                continue;

                            List<float> scores = buffer.Skip(5).Select(x => x * objConfidence).ToList();

                            float mulConfidence = scores.Max(); // find the best label

                            if (mulConfidence <= _model.MulConfidence) // check class obj_conf * cls_conf confidence
                                continue;

                            var rawX = (buffer[0] * 2 - 0.5f + x) * _model.Strides[i]; // predicted bbox x (center)
                            var rawY = (buffer[1] * 2 - 0.5f + y) * _model.Strides[i]; // predicted bbox y (center)

                            var rawW = MathF.Pow(buffer[2] * 2, 2) * _model.Anchors[i][a][0]; // predicted bbox width
                            var rawH = MathF.Pow(buffer[3] * 2, 2) * _model.Anchors[i][a][1]; // predicted bbox height

                            float[] xyxy = Xywh2xyxy(new float[] { rawX, rawY, rawW, rawH });

                            var xMin = xyxy[0] / xGain; // final bbox tlx scaled with ratio (to original size)
                            var yMin = xyxy[1] / yGain; // final bbox tly scaled with ratio (to original size)
                            var xMax = xyxy[2] / xGain; // final bbox brx scaled with ratio (to original size)
                            var yMax = xyxy[3] / yGain; // final bbox bry scaled with ratio (to original size)

                            YoloLabel label = _model.Labels[scores.IndexOf(mulConfidence)];

                            var prediction = new YoloPrediction(label, mulConfidence)
                            {
                                Rectangle = new RectangleF(xMin, yMin, xMax - xMin, yMax - yMin)
                            };

                            result.Add(prediction);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Parses net outputs (sigmoid or detect layer) to predictions.
        /// </summary>
        private List<YoloPrediction> ParseOutput(DenseTensor<float>[] output, Image image)
        {
            return _model.UseDetect ? ParseDetect(output[0], image) : ParseSigmoid(output, image);
        }

        /// <summary>
        /// Removes overlaped duplicates (nms).
        /// </summary>
        private List<YoloPrediction> Supress(List<YoloPrediction> items)
        {
            var result = new List<YoloPrediction>(items);

            foreach (var item in items)
            {
                foreach (var current in result.ToList())
                {
                    if (current == item) continue;

                    var (rect1, rect2) = (item.Rectangle, current.Rectangle);

                    RectangleF intersection = RectangleF.Intersect(rect1, rect2);

                    float intArea = intersection.Area();
                    float unionArea = rect1.Area() + rect2.Area() - intArea;
                    float overlap = intArea / unionArea;

                    if (overlap > _model.Overlap)
                    {
                        if (item.Score > current.Score)
                        {
                            result.Remove(current);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Runs object detection.
        /// </summary>
        public List<YoloPrediction> Predict(Image image)
        {
            return Supress(ParseOutput(Inference(image), image));
        }

        public YoloScorer()
        {
            _model = Activator.CreateInstance<T>();

            var modelFullPath = Path.Combine(AppContext.BaseDirectory, _model.ModelPath);

            if (File.Exists(modelFullPath))
            {
                _inference = new InferenceSession(modelFullPath);
                Logging.Log($"Initialised object detection with model: {modelFullPath}");
            }
            else
                Logging.LogError($"Object Detection model not found at {modelFullPath}");
        }
    }
}
