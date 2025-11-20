using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AudioVisualizer.Controls
{
    public partial class AmplitudeVisualizer : UserControl, IVisualizerPage
    {
        private float smoothedAmplitude = 0f;
        private const float smoothingFactor = 0.2f;

        public AmplitudeVisualizer()
        {
            InitializeComponent();
        }

        public void UpdateData(float[] data)
        {
            if (data.Length > 0)
            {
                float amplitude = data[0];
                smoothedAmplitude = (smoothingFactor * amplitude) + ((1 - smoothingFactor) * smoothedAmplitude);
                DrawVisualizer(smoothedAmplitude);
            }
        }

        private void DrawVisualizer(float amplitude)
        {

            double canvasHeight = VisualizerCanvas.ActualHeight;
            double canvasWidth = VisualizerCanvas.ActualWidth;

            double barHeight = amplitude * canvasHeight;

            var rect = new Rectangle
            {
                Width = canvasWidth,
                Height = barHeight,
                Fill = new LinearGradientBrush(
                    Colors.Cyan, Colors.DeepPink, new Point(0.5, 1), new Point(0.5, 0))
                { MappingMode = BrushMappingMode.RelativeToBoundingBox }
            };

            Canvas.SetLeft(rect, 0);
            Canvas.SetTop(rect, canvasHeight - barHeight);
            VisualizerCanvas.Children.Add(rect);
        }
    }
}
