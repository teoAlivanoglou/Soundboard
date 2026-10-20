using System;
using NAudio.Wave;
using SonicPad.Discovery;

namespace SonicPad.AudioEngine;

internal class CachedSoundSampleProvider(SoundModel cachedSound) : ISampleProvider
{
    private long _position;
    private readonly float[] _audioData = cachedSound.AudioData ??
                                          throw new ArgumentException("Sound has no cached audio data.", nameof(cachedSound));
    private readonly WaveFormat _waveFormat = cachedSound.WaveFormat ??
                                              throw new ArgumentException("Sound has no wave format.", nameof(cachedSound));
    public SoundModel CachedSound { get; init; } = cachedSound;


    public int Read(Span<float> buffer)
    {
        var availableSamples = _audioData.Length - _position;
        var samplesToCopy = Math.Min(availableSamples, buffer.Length);

        _audioData.AsSpan((int)_position, (int)samplesToCopy).CopyTo(buffer);
        _position += samplesToCopy;
        return (int)samplesToCopy;
    }

    public WaveFormat WaveFormat => _waveFormat;
}
