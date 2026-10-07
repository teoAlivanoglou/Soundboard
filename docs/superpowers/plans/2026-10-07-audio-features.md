# Soundboard Audio Features (Unicode Paths, Fade In/Out, Volume) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement Unicode path support for audio discovery & streaming playback, unified fade in/out during playback & stop, and master volume control in Soundboard.Avalonia.

**Architecture:**
- Use .NET `FileStream` with UTF-16 path strings to feed `NAudio.SoundFile.SoundFileReader`, ensuring non-ANSI paths decode without truncation or OS-encoding errors.
- Wrap output in `NAudio.Wave.SampleProviders.VolumeSampleProvider` to control master playback volume directly from the bottom bar UI and settings.
- Apply `DelayFadeOutSampleProvider` for both fade-in on start and fade-out at end-of-track / stop using the unified `FadeInTime` setting.

**Tech Stack:** Avalonia UI 12.1.2, .NET 10, NAudio 3.1.0, NAudio.SoundFile 3.1.0, CommunityToolkit.Mvvm 8.4.2

**Spec:** In-chat bounded design approved by user (Unicode paths via FileStream, unified FadeInTime setting for both fade-in and fade-out, master volume bound to bottom bar slider).

## Global Constraints
- Target project: `Soundboard.Avalonia/Soundboard.Avalonia.csproj`
- Must build cleanly with 0 errors and 0 warnings using `dotnet build Soundboard.Avalonia/Soundboard.Avalonia.csproj`
- Use unified `FadeInTime` in `AudioPlayerSettings` for both fade in and fade out durations
- Maintain thread-safety when updating volume and active playback providers

## Review Focus
1. Non-ANSI paths with accents/symbols (e.g. `café_naïve_🎵.wav`) stream without IO or native decoding exceptions.
2. Stopping a sound triggers a smooth fade-out instead of an immediate hard audio cut / pop, while maintaining responsive UI state.
3. Master volume slider changes take effect immediately on active playback and persist across runs via `SettingsService`.
4. File handles opened for on-demand streaming are deterministically disposed when playback stops or finishes.
5. Zero regressions in default embedded sound playback (`warning.mp3`).

---

### Task 1: Unicode Path Support for Audio Files

**Files:**
- Modify: `Soundboard.Avalonia/Discovery/SoundDiscoveryService.cs`
- Modify: `Soundboard.Avalonia/AudioEngine/AutoDisposeFileReader.cs`
- Modify: `Soundboard.Avalonia/AudioEngine/AudioPlaybackEngine.cs`

- [ ] **Step 1.1**: Update `AutoDisposeFileReader` to accept and manage an optional underlying `Stream` or `IDisposable` so `FileStream` is safely disposed when playback finishes.
- [ ] **Step 1.2**: Update `SoundDiscoveryService.ScanDirectory` to open files via `File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read)` and pass the stream to `SoundFileReader(stream)`.
- [ ] **Step 1.3**: Update `AudioPlaybackEngine.PlaySound` on-demand stream path (`sound.AudioData is null`) to open `File.Open(sound.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read)` and wrap with `AutoDisposeFileReader`.
- [ ] **Step 1.4**: Verify building `Soundboard.Avalonia` succeeds with 0 errors.

---

### Task 2: Hook Fade In and Fade Out Functionality

**Files:**
- Modify: `Soundboard.Avalonia/AudioEngine/DelayFadeOutSampleProvider.cs`
- Modify: `Soundboard.Avalonia/AudioEngine/AudioPlaybackEngine.cs`
- Modify: `Soundboard.Avalonia/UI/Views/SettingsPanelView.axaml`

- [ ] **Step 2.1**: In `DelayFadeOutSampleProvider.cs`, ensure `SetFadeIn` and `FadeEnding` / `SetFadeOut` handle concurrent access safely and support dynamic fade-out on demand. Add a helper `BeginFadeOut(TimeSpan fadeOutDuration)` that starts fading out immediately from current playback position.
- [ ] **Step 2.2**: In `AudioPlaybackEngine.PlaySound`, apply `provider.SetFadeIn(fadeDuration)` when `fadeDuration > 0`. Also apply `provider.FadeEnding(TimeSpan.FromMilliseconds(fadeDuration), sound.Duration)` for end-of-track fade-out.
- [ ] **Step 2.3**: In `AudioPlaybackEngine.StopSound`, if `fadeDuration > 0`, initiate fade-out on active providers via `BeginFadeOut` and schedule removal after the fade duration, avoiding immediate audio click/pop. If `fadeDuration <= 0`, stop immediately.
- [ ] **Step 2.4**: In `SettingsPanelView.axaml`, update label from "Fade In Time:" to "Fade Time:" to reflect that `FadeInTime` governs both fade-in and fade-out.
- [ ] **Step 2.5**: Verify build passes with 0 errors.

---

### Task 3: Volume Control

**Files:**
- Modify: `Soundboard.Avalonia/Settings/AudioPlayerSettings.cs`
- Modify: `Soundboard.Avalonia/AudioEngine/AudioPlaybackEngine.cs`
- Modify: `Soundboard.Avalonia/ViewModels/SoundboardViewModel.cs`
- Modify: `Soundboard.Avalonia/UI/Views/BottomBarView.axaml`

- [ ] **Step 3.1**: In `AudioPlayerSettings.cs`, add `[ObservableProperty] public partial double Volume { get; set; } = 70.0;`.
- [ ] **Step 3.2**: In `AudioPlaybackEngine.cs`, create `private VolumeSampleProvider _volumeProvider;` wrapping `_mixer` during `InitializeDriver()`, and pass `_volumeProvider` to `_outputDevice.Init(...)`. Add `SetVolume(double volume)`.
- [ ] **Step 3.3**: In `SoundboardViewModel.cs`, listen to `Settings.AudioPlayerSettings.PropertyChanged` for `Volume` changes (or expose a bound volume property) and update `_audioEngine.SetVolume(...)`.
- [ ] **Step 3.4**: In `BottomBarView.axaml`, bind the `VolumeSlider`'s `Value` two-way to `{Binding Settings.AudioPlayerSettings.Volume}`.
- [ ] **Step 3.5**: Verify build passes with 0 errors.

---

### Task 4: Integration Verification and Testing

**Files:**
- Create scratch / verification test to validate:
  1. Unicode file path loading via `FileStream`
  2. `DelayFadeOutSampleProvider` fade-in and dynamic fade-out curves
  3. `VolumeSampleProvider` audio scaling
- [ ] **Step 4.1**: Execute automated verification test.
- [ ] **Step 4.2**: Update README checklist.
