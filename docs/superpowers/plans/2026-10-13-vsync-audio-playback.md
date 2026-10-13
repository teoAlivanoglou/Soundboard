# AudioPlaybackEngine VSync Synchronization & Event-Driven Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Eliminate playhead jitter and timer drift by replacing the background thread 50Hz `Task.Delay` loop in `AudioPlaybackEngine` with NAudio `MixerInputEnded` event-driven lifecycle cleanup, and driving UI playback progress via Avalonia's `TopLevel.RequestAnimationFrame` VSync loop in `FastSoundPad`.

**Architecture:** 
1. Store `PlaybackStartTime` on `SoundModel` when playback begins.
2. Update `AudioPlaybackEngine` to map active `ISampleProvider` instances back to `SoundModel` so `_mixer.MixerInputEnded` triggers sound completion and disposal naturally without polling. Remove `UpdateTimers` and its background thread.
3. Update `FastSoundPad.Animation.cs` so that its VSync frame loop (`RequestAnimationFrame`) continues while `sound.IsPlaying == true`, computing real-time progress (`(Now - StartTime) / Duration`) synchronized with display refresh rate.

**Tech Stack:** Avalonia UI (XAML / C# 10 / .NET 10), NAudio (`MixingSampleProvider`, `SampleProviderEventArgs`), CommunityToolkit.Mvvm.

---

### Task 1: Add PlaybackStartTime to SoundModel

**Files:**
- Modify: `Soundboard.Avalonia/Discovery/SoundModel.cs:10-25`

- [ ] **Step 1: Add `PlaybackStartTime` property to `SoundModel`**
In `Soundboard.Avalonia/Discovery/SoundModel.cs`, add:
```csharp
[ObservableProperty] public partial DateTime? PlaybackStartTime { get; set; }
```

- [ ] **Step 2: Build project to verify compilation**
Run: `dotnet build Soundboard.Avalonia/Soundboard.Avalonia.csproj`
Expected: Build succeeded with 0 errors.

---

### Task 2: Transition AudioPlaybackEngine to Event-Driven Cleanup and Remove UpdateTimers

**Files:**
- Modify: `Soundboard.Avalonia/AudioEngine/AudioPlaybackEngine.cs`

**Interfaces:**
- Consumes: `_mixer.MixerInputEnded`, `SoundModel.PlaybackStartTime`, `SoundModel.IsPlaying`
- Produces: Provider-to-Sound reverse lookup dictionary `_sampleProviderToSound`, `MixerOnMixerInputEnded` handler. Completely deletes `UpdateTimers` and `_cancellationTokenSource`.

- [ ] **Step 1: Update fields and constructor in `AudioPlaybackEngine.cs`**
- Remove `_cancellationTokenSource` and `_soundsLastPlayed`.
- Add `private readonly Dictionary<ISampleProvider, SoundModel> _providerToSound = [];`.
- Remove `_ = UpdateTimers(_cancellationTokenSource.Token);` from the constructor and `Reset()`.
- Remove `_cancellationTokenSource.Cancel()` from `Dispose()`.

- [ ] **Step 2: Update `PlaySound` to record start time and provider mapping**
- Set `sound.PlaybackStartTime = DateTime.UtcNow;` before `_mixer.AddMixerInput(provider);`.
- In `lock (_lock)`, record `_providerToSound[provider] = sound;`.

- [ ] **Step 3: Update `StopSound` to clean up reverse lookup**
- Remove providers from `_providerToSound`.
- Reset `sound.PlaybackStartTime = null;`.

- [ ] **Step 4: Implement `MixerOnMixerInputEnded` and delete `UpdateTimers`**
In `MixerOnMixerInputEnded(object? sender, SampleProviderEventArgs e)`:
```csharp
SoundModel? finishedSound = null;
lock (_lock)
{
    if (_providerToSound.Remove(e.SampleProvider, out var sound))
    {
        if (_soundsAndSampleProviders.TryGetValue(sound, out var list))
        {
            list.Remove(e.SampleProvider);
            if (list.Count == 0)
            {
                _soundsAndSampleProviders.Remove(sound);
                finishedSound = sound;
            }
        }
    }
}

if (e.SampleProvider is IDisposable disposable)
{
    disposable.Dispose();
}

if (finishedSound != null)
{
    Dispatcher.UIThread.Post(() =>
    {
        finishedSound.IsPlaying = false;
        finishedSound.Progress = 0;
        finishedSound.PlaybackStartTime = null;
    });
}
```
Delete `UpdateTimers` method.

- [ ] **Step 5: Build project to verify compilation**
Run: `dotnet build Soundboard.Avalonia/Soundboard.Avalonia.csproj`
Expected: Build succeeded with 0 errors.

---

### Task 3: Drive Progress in FastSoundPad via VSync Loop

**Files:**
- Modify: `Soundboard.Avalonia/UI/Controls/FastSoundPad.Animation.cs`
- Modify: `Soundboard.Avalonia/UI/Controls/FastSoundPad.Rendering.cs`

- [ ] **Step 1: Extend `RequestFrameIfNeeded` to keep running while sound is playing**
In `FastSoundPad.Animation.cs`:
```csharp
private bool ShouldAnimate => (_timeline?.IsRunning == true) || (_boundSound?.IsPlaying == true);

private void RequestFrameIfNeeded()
{
    if (!_isFrameScheduled && ShouldAnimate)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
        {
            _isFrameScheduled = true;
            topLevel.RequestAnimationFrame(OnAnimationFrame);
        }
    }
}
```

- [ ] **Step 2: Update `OnAnimationFrame` to calculate progress and keep ticking**
In `OnAnimationFrame(TimeSpan currentTime)`:
- Continue executing if `ShouldAnimate`.
- If `_boundSound is { IsPlaying: true, PlaybackStartTime: not null }`:
  Calculate progress:
  ```csharp
  var elapsedMs = (DateTime.UtcNow - _boundSound.PlaybackStartTime.Value).TotalMilliseconds;
  var totalMs = Math.Max(_boundSound.Duration.TotalMilliseconds, 250.0);
  _boundSound.Progress = Math.Clamp(elapsedMs / totalMs, 0.0, 1.0);
  ```
- Always call `InvalidateVisual()`.
- If `ShouldAnimate`, call `RequestFrameIfNeeded()`.

- [ ] **Step 3: Build project to verify compilation**
Run: `dotnet build Soundboard.Avalonia/Soundboard.Avalonia.csproj`
Expected: Build succeeded with 0 errors.

---

### Task 4: Update README and Documentation

**Files:**
- Modify: `README.md`

- [ ] **Step 1: Check off the item in README.md**
Mark `[x] **AudioPlaybackEngine VSync synchronization**` as complete in `README.md`.
