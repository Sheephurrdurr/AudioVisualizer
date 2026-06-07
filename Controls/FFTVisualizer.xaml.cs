using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AudioVisualizer.Controls
{
    public partial class FFTVisualizer : UserControl, IVisualizerPage
    {
        private readonly List<Rectangle> bars = new List<Rectangle>();
        private readonly List<Rectangle> peakBars = new List<Rectangle>();
        private bool isCanvasLoaded = false;

        private float[] smoothedEnergy = Array.Empty<float>();
        private float[] barDecay = Array.Empty<float>();
        private float[] peakHeights = Array.Empty<float>();


        private (int start, int end)[] bandRanges = Array.Empty<(int, int)>();

        public FFTVisualizer()
        {
            InitializeComponent();
            Canvas.Loaded += (s, e) => { isCanvasLoaded = true; };

            RootGrid.SizeChanged += (s, e) =>
            {
                Canvas.Width = e.NewSize.Width;
                Canvas.Height = e.NewSize.Height;
            };
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
                peakHeights = new float[bands];
                bandRanges = new (int, int)[bands];

                double logLen = Math.Log(data.Length, 2);

                for (int i = 0; i < bands; i++) 
                {
                    float t = i / (float)bands;
                    byte r = (byte)(255 * (1 - t));
                    byte g = (byte)(255 * t);
                    byte b = 255;

                    var rect = new Rectangle
                    {
                        Width = barWidth - 1,
                        Height = 0,
                        Fill = new SolidColorBrush(Color.FromRgb(r, g, b))
                    };
                    Canvas.Children.Add(rect);
                    bars.Add(rect);

                    float t2 = i / (float)bands;
                    byte pr = (byte)(255 * (1 - t2));
                    byte pg = (byte)(255 * t2);

                    var peak = new Rectangle
                    {
                        Width = barWidth - 1,
                        Height = 2,
                        Fill = new SolidColorBrush(Color.FromRgb(pr, pg, 255)),
                        Opacity = 0.9
                    };
                    Canvas.Children.Add(peak);
                    peakBars.Add(peak);

                    int start = (int)Math.Pow(2, i * logLen / bands);
                    int end = (int)Math.Pow(2, (i + 1) * logLen / bands);
                    if (end <= start) end = start + 1;
                    if (end > data.Length) end = data.Length;
                    bandRanges[i] = (start, end);
                }
            }

            var decay = barDecay!;
            var ranges = bandRanges!;

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
                
                smoothedEnergy[b] = smoothedEnergy[b] * 0.6f + (float)rms * (1 - 0.4f); // Adjust the smoothing factor as needed (0.6f here means 60% of the previous value is retained, and 40% of the new value is added)

                double db = 20.0 * Math.Log10(smoothedEnergy[b] + 1e-9);
                double norm = Math.Clamp((db + 60.0) / 60.0, 0, 1);

                double targetHeight = norm * canvasHeight;
                double currentHeight = bars[b].Height;
                double newHeight = currentHeight + (targetHeight - currentHeight) * 0.12;
                bars[b].Height = newHeight;

                // Peak logic
                if (newHeight > peakHeights[b])
                    peakHeights[b] = (float)newHeight; // hop op
                else
                    peakHeights[b] *= 0.97f; // decay langsomt ned

                Canvas.SetLeft(peakBars[b], b * barWidth);
                Canvas.SetTop(peakBars[b], canvasHeight - peakHeights[b] - 2);

                Canvas.SetTop(bars[b], canvasHeight - newHeight);
                Canvas.SetLeft(bars[b], b * barWidth);
            }

        }
    }
}

