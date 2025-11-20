using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace AudioVisualizer.Controls
{
    public partial class FFTVisualizer : UserControl, IVisualizerPage
    {
        private readonly List<Rectangle> bars = new List<Rectangle>();
        private bool isCanvasLoaded = false;

        private float[]? smoothedEnergy;
        private float[]? barDecay;

        private (int start, int end)[]? bandRanges;

        public FFTVisualizer()
        {
            InitializeComponent();
            Canvas.Loaded += (s, e) => { isCanvasLoaded = true; };
        }

        public void UpdateData(float[] data) // updates when new FFT data is recieved. Parameter is FFT magnitude array.
                                             // handles the animation for the moving bars, creates the ranges
        {
            if (!isCanvasLoaded || data == null || data.Length == 0) return;

            int bands = 32; // bars being displayed
            double canvasHeight = Math.Max(1, Canvas.ActualHeight);
            double barWidth = Math.Max(1, Canvas.ActualWidth / bands);

            // Initialize bars and ranges, if none exist
            if (bars.Count != bands || bandRanges == null || bandRanges.Length != bands)
            {
                Canvas.Children.Clear();
                bars.Clear();
                barDecay = new float[bands];
                smoothedEnergy = new float[bands];
                bandRanges = new (int, int)[bands];

                double logLen = Math.Log(data.Length, 2);

                for (int i = 0; i < bands; i++) 
                {
                    var rect = new Rectangle
                    {
                        Width = barWidth - 1,
                        Height = 0,
                        Fill = new SolidColorBrush(Colors.LimeGreen)
                    };
                    Canvas.Children.Add(rect);
                    bars.Add(rect);

                    int start = (int)Math.Pow(2, i * logLen / bands);
                    int end = (int)Math.Pow(2, (i + 1) * logLen / bands);
                    if (end <= start) end = start + 1;
                    if (end > data.Length) end = data.Length;
                    bandRanges[i] = (start, end);
                }
            }

            var decay = barDecay!;
            var ranges = bandRanges!;

            const double barSmoothing = 0.25;

            // Convert data to moving bars. Apply a bunch of math for bar smoothing.
            for (int b = 0; b < bands; b++)
            {
                var (start, end) = ranges[b];
                double sum = 0;
                for (int j = start; j < end; j++)
                {
                    double v = data[j];
                    if (double.IsNaN(v) || double.IsInfinity(v)) v = 0;
                    sum += v * v;
                }

                double rms = Math.Sqrt(sum / Math.Max(1, end - start));
                float smoothingFactor = 0.4f;
                smoothedEnergy[b] = smoothedEnergy[b] * smoothingFactor + (float)rms * (1 - smoothingFactor); // Exponential smoothing 

                double db = 20.0 * Math.Log10(smoothedEnergy[b] + 1e-9);
                double norm = Math.Clamp((db + 60.0) / 60.0, 0, 1);

                double targetHeight = norm * canvasHeight;
                double currentHeight = bars[b].Height;
                double newHeight = currentHeight + (targetHeight - currentHeight) * 0.25;
                bars[b].Height = newHeight;
                Canvas.SetTop(bars[b], canvasHeight - newHeight);
                Canvas.SetLeft(bars[b], b * barWidth);
            }

        }
    }
}

