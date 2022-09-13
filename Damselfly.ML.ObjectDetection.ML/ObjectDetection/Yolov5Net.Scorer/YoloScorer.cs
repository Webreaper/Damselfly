using Damselfly.Core.Utils;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
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

        private Tensor<float> ExtractPixels( Image<Rgb24> image )
        {
            var tensor = new DenseTensor<float>( new[] { 1, 3, image.Height, image.Width } );

            image.ProcessPixelRows( pixelAccessor =>
            {
                for( var y = 0; y < pixelAccessor.Height; y++ )
                {
                    var row = pixelAccessor.GetRowSpan( y );

                    for( var x = 0; x < row.Length; x++ )
                    {
                        tensor[0, 0, y, x] = row[x].R / 255.0F;
                        tensor[0, 1, y, x] = row[x].G / 255.0F;
                        tensor[0, 2, y, x] = row[x].B / 255.0F;
                    }
                }
            } );

            return tensor;
        }

        /// <summary>
        /// Runs inference session.
        /// </summary>
        private DenseTensor<float>[] Inference( Image<Rgb24> image )
        {
            if( image.Height > _model.Height || image.Width > _model.Width )
            {
                image.Mutate( x =>
                {
                    x.Resize( new ResizeOptions
                    {
                        Size = new Size( _model.Width, _model.Height ),
                        Mode = ResizeMode.Pad
                    } );
                } );
            }

            var pixels = ExtractPixels( image );

            var inputs = new List<NamedOnnxValue> // add image as onnx input
            {
                NamedOnnxValue.CreateFromTensor("images", pixels)
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
        private List<YoloPrediction> ParseDetect(DenseTensor<float> output, int height, int width)
        {
            var result = new List<YoloPrediction>();

            if( _model.Height != _model.Width )
                throw new ArgumentException( "Scale calculation will not work if model is not square" );

            var longestSide = 0;
            var hOffset = 0.0f;
            var vOffset = 0.0f;

            if( height == width )
            {
                longestSide = height;
            }
            else if( height > width )
            {
                // Portrait
                hOffset = ( ( height - width ) / 2.0f );
                longestSide = height;
            }
            else
            {
                // Landscape
                vOffset = ( ( width - height ) / 2.0f );
                longestSide = width;
            }

            var scale = (float)longestSide / _model.Width;

            for (int i = 0; i < output.Length / _model.Dimensions; i++) // iterate tensor
            {
                if (output[0, i, 4] <= _model.Confidence)
                    continue;

                for (int j = 5; j < _model.Dimensions; j++) // compute mul conf
                {
                    output[0, i, j] = output[0, i, j] * output[0, i, 4]; // conf = obj_conf * cls_conf
                }

                for (int k = 5; k < _model.Dimensions; k++)
                {
                    if (output[0, i, k] <= _model.MulConfidence)
                        continue;

                    var xMin = (output[0, i, 0] - output[0, i, 2] / 2); // top left x
                    var yMin = (output[0, i, 1] - output[0, i, 3] / 2); // top left y
                    var xMax = (output[0, i, 0] + output[0, i, 2] / 2); // bottom right x
                    var yMax = (output[0, i, 1] + output[0, i, 3] / 2); // bottom right y

                    YoloLabel label = _model.Labels[k - 5];

                    var rectX = (xMin * scale) - hOffset;
                    var rectY = (yMin * scale) - vOffset;
                    var rectHeight = ( xMax - xMin ) * scale;
                    var rectWidth = ( yMax - yMin ) * scale;

                    var prediction = new YoloPrediction(label, output[0, i, k])
                    {
                        Rectangle = new RectangleF(rectX, rectY, rectHeight, rectWidth)
                    };

                    result.Add(prediction);
                }
            }

            return result;
        }

        /// <summary>
        /// Parses net outputs (sigmoid) to predictions.
        /// </summary>
        private List<YoloPrediction> ParseSigmoid(DenseTensor<float>[] output, int height, int width)
        {
            var result = new List<YoloPrediction>();

            var (xGain, yGain) = (_model.Width / (float)width, _model.Height / (float)height);

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
        private List<YoloPrediction> ParseOutput(DenseTensor<float>[] output, int height, int width)
        {
            return _model.UseDetect ? ParseDetect(output[0], height, width) : ParseSigmoid(output, height, width);
        }

        /// <summary>
        /// Removes overlaped duplicates (nms).
        /// </summary>
        private List<YoloPrediction> Supress(List<YoloPrediction> items)
        {
            var result = new List<YoloPrediction>( items );

            try
            {

                foreach( var item in items )
                {
                    foreach( var current in result.ToList() )
                    {
                        if( current == item )
                            continue;

                        var (rect1, rect2) = (item.Rectangle, current.Rectangle);

                        RectangleF intersection = RectangleF.Intersect( rect1, rect2 );

                        float intArea = intersection.Area();
                        float unionArea = rect1.Area() + rect2.Area() - intArea;
                        float overlap = intArea / unionArea;

                        if( overlap > _model.Overlap )
                        {
                            if( item.Score > current.Score )
                            {
                                result.Remove( current );
                            }
                        }
                    }
                }
            }
            catch( Exception ex )
            {
                Logging.Log( $"Exception during object suppression: {ex}" );
                result.Clear();
            }

            return result;
        }

        /// <summary>
        /// Runs object detection.
        /// </summary>
        public List<YoloPrediction> Predict(Image<Rgb24> image )
        {
            var originalWidth = image.Width;
            var originalHeight = image.Height;

            var inference = Inference( image );
            var predictions = ParseOutput( inference, originalHeight, originalWidth );
            var suppressed = Supress( predictions );

            return suppressed;
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
