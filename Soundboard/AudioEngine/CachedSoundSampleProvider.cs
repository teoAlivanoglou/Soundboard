using NAudio.Wave;
using Soundboard.Discovery;
using System.Diagnostics;

namespace Soundboard.AudioEngine;

class CachedSoundSampleProvider(SoundModel cachedSound) : ISampleProvider
{
    private long _position;
    public SoundModel CachedSound { get; init; } = cachedSound;


    public int Read(Span<float> buffer)
    {
        Debug.Assert(CachedSound.AudioData != null);

        var availableSamples = CachedSound.AudioData.Length - _position;
        var samplesToCopy = Math.Min(availableSamples, buffer.Length);
        
        CachedSound.AudioData.AsSpan((int)_position, (int)samplesToCopy).CopyTo(buffer);
        _position += samplesToCopy;
        return (int)samplesToCopy;
    }

    public WaveFormat WaveFormat => CachedSound.WaveFormat!;
}
