using AudioVisualizer.Audio;
using AudioVisualizer.Controls;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace AudioVisualizer
{
    public partial class MainWindow : Window
    {
        private readonly Controls.AmplitudeVisualizer amplitudeVisualizer;
        private readonly Controls.FFTVisualizer fftVisualizer;
        private readonly AudioPlayer _player;
        private DispatcherTimer timer;

        private IVisualizerPage activeVisualizer;

        public MainWindow()
        {
            InitializeComponent();
            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };

            amplitudeVisualizer = new Controls.AmplitudeVisualizer();
            fftVisualizer = new Controls.FFTVisualizer();

            VisualizerHost.Content = amplitudeVisualizer;
            activeVisualizer = amplitudeVisualizer;

            var assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");

            var files = Directory.GetFiles(assetsPath, "*.mp3")
                .Concat(Directory.GetFiles(assetsPath, "*.wav"))
                .Select(Path.GetFileName)
                .Where(f => f != null)
                .Select(f => f!) // null-forgiving operator since we already filtered out nulls with Where()
                .ToList()!;

            _player = new AudioPlayer(files);

            _player.StateChanged += OnStateChanged;
            _player.TrackChanged += OnTrackChanged;

            _player.AmplitudeChanged += OnAmplitudeChanged;
            _player.FFTComputed += OnFFTComputed;

            _player.LoadTrack(0);

            InitTimer();
        }

        private void OnAmplitudeChanged(float amp)
        {
            Dispatcher.Invoke(() =>
            {
                Console.WriteLine($"raw amp: {amp:F3}");
                if (VisualizerHost.Content is AmplitudeVisualizer visualizer)
                    visualizer.UpdateData(new[] { amp });
            });
        }

        private void OnFFTComputed(float[] fft)
        {
            Dispatcher.Invoke(() =>
            {
                if (VisualizerHost.Content is FFTVisualizer visualizer)
                    visualizer.UpdateData(fft);
            });
        }

        private void OnTrackChanged(string trackName)
        {
            Dispatcher.Invoke(() => TrackNameText.Text = trackName);
        }

        private void SwitchVisualizer(IVisualizerPage newVisualizer)
        {
            activeVisualizer = newVisualizer;
            VisualizerHost.Content = newVisualizer;
        }

        private void ToggleVisualizerButton_Click(object sender, RoutedEventArgs e)
        {
            if (VisualizerHost.Content is AmplitudeVisualizer)
                SwitchVisualizer(fftVisualizer);
            else
                SwitchVisualizer(amplitudeVisualizer);
        }

        private void InitTimer()
        {
            timer.Start();
        }

        private void OnStateChanged(Audio.PlayerState state)
        {
            Dispatcher.Invoke(() =>
            {
                PausePlayButton.Content = state == Audio.PlayerState.Playing ? "⏸" : "▶";
            });
           
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e) => _player.Previous();
        private void PausePlayButton_Click(object sender, RoutedEventArgs e)
        {
            switch (_player.State)
            {
                case Audio.PlayerState.Stopped:
                    _player.Play();
                    break;
                case Audio.PlayerState.Playing:
                    _player.Pause();
                    break;
                case Audio.PlayerState.Paused:
                    _player.Play();
                    break;
            }
        }
        private void NextButton_Click(object sender, RoutedEventArgs e) => _player.Next();

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_player != null)
                _player.Volume = (float)e.NewValue;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _player.Stop();
            _player.Dispose();
            Application.Current.Shutdown();
        }
    }
}
