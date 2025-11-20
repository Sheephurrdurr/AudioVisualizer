using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;

namespace AudioVisualizer.Audio
{
    public enum PlayerState { Stopped, Playing, Paused }

    public class AudioPlayer : IDisposable
    {
        private WaveOutEvent? waveOut;
        private AudioFileReader? audioFile;
        private SampleChannel? sampleChannel;
        private MeteringSampleProvider? meteringProvider;

        private readonly List<string> playlist;
        private int currentTrackIndex = -1;

        public event Action<float>? AmplitudeChanged;
        public event Action<float[]>? FFTComputed;
        public event Action<PlayerState>? StateChanged;

        private PlayerState _state = PlayerState.Stopped;
        public PlayerState State
        {
            get => _state;
            private set
            {
                _state = value;
                StateChanged?.Invoke(value);
            }
        }

        public AudioPlayer(List<string> playlist)
        {
            this.playlist = playlist ?? new List<string>();
        }

        public void LoadTrack(int index)
        {
            DisposePlayback();

            if (index < 0 || index >= playlist.Count) return;

            currentTrackIndex = index;
            string audioPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", playlist[currentTrackIndex]);

            audioFile = new AudioFileReader(audioPath);

            sampleChannel = new SampleChannel(audioFile, true);

            var aggregator = new SampleAggregator(sampleChannel, mags => FFTComputed?.Invoke(mags));

            meteringProvider = new MeteringSampleProvider(aggregator);
            meteringProvider.StreamVolume += OnStreamVolume;
            waveOut = new WaveOutEvent();
            waveOut.PlaybackStopped += WaveOut_PlaybackStopped;
            waveOut.Init(meteringProvider);

            State = PlayerState.Stopped;
        }

        public void Play()
        {
            waveOut?.Play();
            State = PlayerState.Playing;
        }

        public void Pause()
        {
            waveOut?.Pause();
            State = PlayerState.Paused;
        }

        public void Stop()
        {
            waveOut?.Stop();
            State = PlayerState.Stopped;
        }

        public void Next()
        {
            int nextIndex = (currentTrackIndex + 1) % playlist.Count;
            LoadTrack(nextIndex);
            Play();
        }

        public void Previous()
        {
            int prevIndex = (currentTrackIndex - 1 + playlist.Count) % playlist.Count;
            LoadTrack(prevIndex);
            Play();
        }

        private void OnStreamVolume(object sender, StreamVolumeEventArgs e)
        {
            // Fire amplitude event
            float amp = Math.Max(e.MaxSampleValues[0], e.MaxSampleValues[1]);
            AmplitudeChanged?.Invoke(amp);
        }

        private void WaveOut_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            State = PlayerState.Stopped;
        }

        private void DisposePlayback()
        {
            waveOut?.Stop();
            if (waveOut != null)
            {
                waveOut.PlaybackStopped -= WaveOut_PlaybackStopped;
                waveOut.Dispose();
                waveOut = null;
            }

            if (meteringProvider != null)
            {
                meteringProvider.StreamVolume -= OnStreamVolume;
                meteringProvider = null;
            }
                audioFile?.Dispose();
                audioFile = null;
        }

        public void Dispose()
        {
            DisposePlayback();
        }
    }

    public class SampleAggregator : ISampleProvider
    {
        private readonly ISampleProvider source;
        private readonly Action<float[]> onFFTComputed;
        private readonly int fftLength;
        private readonly Complex[] fftBuffer;
        private readonly float[] windowBuffer;
        private int bufferPos;

        public WaveFormat WaveFormat => source.WaveFormat;

        public SampleAggregator(ISampleProvider source, Action<float[]> onFFTComputed, int fftLength = 1024)
        {
            this.source = source;
            this.onFFTComputed = onFFTComputed;
            this.fftLength = fftLength;
            fftBuffer = new Complex[fftLength];
            windowBuffer = new float[fftLength];
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = source.Read(buffer, offset, count);

            for (int i = 0; i < samplesRead; i++)
            {
                windowBuffer[bufferPos++] = buffer[offset + i];
                if (bufferPos >= fftLength)
                {
                    ComputeFFT();
                    bufferPos = 0;
                }
            }
            return samplesRead;
        }

        private void ComputeFFT()
        {
            for (int i = 0; i < fftLength; i++)
            {
                fftBuffer[i].X = (float)(windowBuffer[i] * FastFourierTransform.HammingWindow(i, fftLength));
                fftBuffer[i].Y = 0;
            }

            FastFourierTransform.FFT(true, (int)Math.Log(fftLength, 2), fftBuffer);

            float[] magnitudes = new float[fftLength / 2];
            for (int i = 0; i < magnitudes.Length; i++)
                magnitudes[i] = (float)Math.Sqrt(fftBuffer[i].X * fftBuffer[i].X + fftBuffer[i].Y * fftBuffer[i].Y);

            onFFTComputed?.Invoke(magnitudes);
        }
    }
}

