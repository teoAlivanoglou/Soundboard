using NAudio.Wave;
using Soundboard.Models;
using System.Diagnostics;

namespace Soundboard.AudioEngine;

class CachedSoundSampleProvider(Sound cachedSound) : ISampleProvider
{
    private long _position;
    public Sound CachedSound { get; init; } = cachedSound;

    public int Read(Span<float> buffer)
    {
        Debug.Assert(CachedSound.AudioData != null);

        var availableSamples = CachedSound.AudioData.Length - _position;
        var samplesToCopy = Math.Min(availableSamples, buffer.Length);
        
        CachedSound.AudioData.AsSpan((int)_position, (int)samplesToCopy).CopyTo(buffer);
        _position += samplesToCopy;
        return (int)samplesToCopy;
    }

    public WaveFormat WaveFormat => CachedSound.WaveFormat;
}
