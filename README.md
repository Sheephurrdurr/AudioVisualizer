# AudioVisualizer 1.0
**Built with Claude Sonnet** I'm merely along for the ride to learn some cool new stuff.

> Make sure audio files exist in the `Assets/` folder — the player auto-loads everything it finds there.

## Overview
C# WPF application that visualizes audio playback in real-time with a neon/cyberpunk aesthetic.
Uses NAudio for audio playback and FFT analysis, and WPF Canvas for rendering visuals.

## Features
- Play, Pause, Next, Previous track functionality
- Auto-loads all `.wav` and `.mp3` files from `Assets/` folder
- Volume control
- **Amplitude Visualizer** — pulsing circular ring with shockwave beat detection
- **FFT Visualizer** — frequency spectrum bars with gradient colors and peak indicators
- Toggle between visualizers on the fly
- Neon cyberpunk UI with custom styled controls
- Custom draggable title bar

## How to run
1. Open project in Visual Studio
2. Drop audio files (`.wav` / `.mp3`) into `Assets/` folder
3. Build and run
4. Controls:
   - `▶ / ⏸` — Play / Pause
   - `⏮ / ⏭` — Previous / Next track
   - `Toggle View` — Switch between Amplitude and FFT visualizer
   - Volume slider on the right side

## Tech
- .NET 10 / WPF
- NAudio (playback, FFT, metering)
- MathNet.Numerics
