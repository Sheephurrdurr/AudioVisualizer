using AudioVisualizer.Audio;
using AudioVisualizer.Controls;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
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

            _player = new AudioPlayer(new List<string> { "StereoLove.wav", "Onion.wav" });

            _player.StateChanged += OnStateChanged;

            _player.AmplitudeChanged += OnAmplitudeChanged;
            _player.FFTComputed += OnFFTComputed;

            _player.FFTComputed += magnitudes =>
            {
                Console.WriteLine("FFT fired: " + magnitudes[0].ToString("F2"));
            };


            InitTimer();
        }

        private void OnAmplitudeChanged(float amp)
        {
            Dispatcher.Invoke(() =>
            {
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
            PausePlayButton.Content = state == Audio.PlayerState.Playing ? "⏸" : "▶";
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
    }
}
