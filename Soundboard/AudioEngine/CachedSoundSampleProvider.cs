using NAudio.Wave;
using Soundboard.Models;

namespace Soundboard.AudioEngine;

class CachedSoundSampleProvider(Sound cachedSound) : ISampleProvider
{
    private long _position;
    public Sound CachedSound { get; init; } = cachedSound;

    public int Read(float[] buffer, int offset, int count)
    {
        var availableSamples = CachedSound.AudioData.Length - _position;
        var samplesToCopy = Math.Min(availableSamples, count);
        Array.Copy(CachedSound.AudioData, _position, buffer, offset, samplesToCopy);
        _position += samplesToCopy;
        return (int)samplesToCopy;
    }

    public WaveFormat WaveFormat => CachedSound.WaveFormat;
}
