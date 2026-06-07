using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace AudioVisualizer.Controls
{
    public partial class AmplitudeVisualizer : UserControl, IVisualizerPage
    {
        private float smoothedAmplitude = 0f;
        private const float smoothingFactor = 0.15f;

        private readonly Ellipse outerRing;
        private readonly Ellipse innerCore;

        private readonly List<ShockwaveRing> shockwaves = new List<ShockwaveRing>();
        private float previousAmplitude = 0f;
        private const float beatThreshold = 0.02f;

        public AmplitudeVisualizer()
        {
            InitializeComponent();

            // Ensure the canvas resizes with the control
            RootGrid.SizeChanged += (s, e) =>
            {
                VisualizerCanvas.Width = e.NewSize.Width;
                VisualizerCanvas.Height = e.NewSize.Height;
                DrawVisualizer(smoothedAmplitude); // Redraw to adjust to new size
            };

            var animationTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(30)
            };
            animationTimer.Tick += (s, e) => UpdateShockwaves();
            animationTimer.Start();

            // Inner core, always visible with a fixed size and a bright cyan color with a glow effect
            innerCore = new Ellipse
            {
                Width = 40,
                Height = 40,
                Fill = new SolidColorBrush(Color.FromRgb(0, 255, 255)), // cyan
                Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(0, 255, 255),
                    BlurRadius = 20,
                    ShadowDepth = 0,
                    Opacity = 1.0
                }
            };

            // Outer ring that moves with the sound
            outerRing = new Ellipse
            {
                Width = 0,
                Height = 0,
                Stroke = new SolidColorBrush(Color.FromRgb(255, 0, 200)), // magenta
                StrokeThickness = 3,
                Fill = Brushes.Transparent,
                Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(255, 0, 200),
                    BlurRadius = 25,
                    ShadowDepth = 0,
                    Opacity = 0.9
                }
            };

            VisualizerCanvas.Children.Add(outerRing);
            VisualizerCanvas.Children.Add(innerCore);
        }

        public void UpdateData(float[] data)
        {
            if (smoothedAmplitude - previousAmplitude > beatThreshold)
            {
                Console.WriteLine($"Beat! amp: {smoothedAmplitude:F3} prev: {previousAmplitude:F3}");
            }
            if (data.Length > 0)
            {
                float amplitude = data[0];
                smoothedAmplitude = (smoothingFactor * amplitude) + ((1 - smoothingFactor) * smoothedAmplitude);

                // Spawns a new ring when a beat is detected (when the amplitude increases sharply)
                if (smoothedAmplitude - previousAmplitude > beatThreshold)
                {
                    var ellipse = new Ellipse
                    {
                        Stroke = new SolidColorBrush(Color.FromRgb(255, 0, 200)),
                        StrokeThickness = 2,
                        Fill = Brushes.Transparent,
                        Effect = new DropShadowEffect
                        {
                            Color = Color.FromRgb(255, 0, 200),
                            BlurRadius = 15,
                            ShadowDepth = 0,
                            Opacity = 0.8
                        }
                    };
                    VisualizerCanvas.Children.Add(ellipse);
                    shockwaves.Add(new ShockwaveRing(ellipse, 40));
                }

                previousAmplitude = smoothedAmplitude;

                DrawVisualizer(smoothedAmplitude);
            }
        }

        private void DrawVisualizer(float amplitude)
        {
            double cx = VisualizerCanvas.ActualWidth / 2;
            double cy = VisualizerCanvas.ActualHeight / 2;

            // Outer ring that moves with the sound
            double maxRadius = Math.Min(cx, cy) * 0.75;
            double ringDiameter = 60 + (amplitude * (maxRadius * 2 - 60));

            outerRing.Width = ringDiameter;
            outerRing.Height = ringDiameter;
            Canvas.SetLeft(outerRing, cx - ringDiameter / 2);
            Canvas.SetTop(outerRing, cy - ringDiameter / 2);

            // Inner core always centered
            Canvas.SetLeft(innerCore, cx - innerCore.Width / 2);
            Canvas.SetTop(innerCore, cy - innerCore.Height / 2);
        }

        // Updates shockwave rings, expanding them and fading them out over time
        private void UpdateShockwaves()
        {
            double cx = VisualizerCanvas.ActualWidth / 2;
            double cy = VisualizerCanvas.ActualHeight / 2;

            for (int i = shockwaves.Count - 1; i >= 0; i--)
            {
                var sw = shockwaves[i];
                sw.Radius += 4;
                sw.Opacity -= 0.03;

                if (sw.Opacity <= 0)
                {
                    VisualizerCanvas.Children.Remove(sw.Ellipse);
                    shockwaves.RemoveAt(i);
                    continue;
                }

                double d = sw.Radius * 2;
                sw.Ellipse.Width = d;
                sw.Ellipse.Height = d;
                sw.Ellipse.Opacity = sw.Opacity;
                Canvas.SetLeft(sw.Ellipse, cx - sw.Radius);
                Canvas.SetTop(sw.Ellipse, cy - sw.Radius);
            }
        }
    }

    internal class ShockwaveRing
    {
        public Ellipse Ellipse { get; set; }
        public double Radius { get; set; }
        public double Opacity { get; set; }

        public ShockwaveRing(Ellipse ellipse, double startRadius)
        {
            Ellipse = ellipse;
            Radius = startRadius;
            Opacity = 1.0;
        }
    }
}