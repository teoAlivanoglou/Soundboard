using System;
using System.Collections.Generic;
using System.Threading;
using Soundboard.Avalonia.Discovery;

namespace Soundboard.Avalonia.AudioEngine;

public class AudioDataCache
{
    private readonly long _maxBytes;
    private readonly int _maxCount;
    private readonly long _maxBytesPerSound;
    private long _currentBytes;

    private readonly LinkedList<SoundModel> _lru = new();
    private readonly Dictionary<SoundModel, LinkedListNode<SoundModel>> _lookup = new();
    private readonly Lock _lock = new();

    public AudioDataCache(long maxMegabytes = 64, int maxCount = 25, int minFitRatio = 4)
    {
        _maxBytes = maxMegabytes * 1024 * 1024;
        _maxCount = maxCount;
        _maxBytesPerSound = _maxBytes / Math.Max(1, minFitRatio);
    }

    /// <summary>
    /// Quick check before decoding: does this sound fit the K-ratio threshold?
    /// </summary>
    public bool CanCache(SoundModel sound)
    {
        // 48 kHz stereo 32-bit float is ~384 KB/sec
        var estimatedBytes = sound.Duration.TotalSeconds * 48000 * 2 * sizeof(float);
        return estimatedBytes <= _maxBytesPerSound;
    }

    /// <summary>
    /// Promotes an already-cached sound to the front of the LRU.
    /// </summary>
    public void Touch(SoundModel sound)
    {
        lock (_lock)
        {
            if (_lookup.TryGetValue(sound, out var node) && node != _lru.First)
            {
                _lru.Remove(node);
                _lru.AddFirst(node);
            }
        }
    }

    /// <summary>
    /// Stores the decoded audio data, evicting oldest sounds if over budget.
    /// </summary>
    public void Add(SoundModel sound, float[] data)
    {
        long soundBytes = (long)data.Length * sizeof(float);
        if (soundBytes > _maxBytesPerSound)
            return;

        lock (_lock)
        {
            // If already tracked, remove previous size
            if (_lookup.TryGetValue(sound, out var existing))
            {
                _lru.Remove(existing);
                _lookup.Remove(sound);
                if (sound.AudioData is not null)
                    _currentBytes -= (long)sound.AudioData.Length * sizeof(float);
            }

            // Evict oldest until within limits
            while ((_currentBytes + soundBytes > _maxBytes || _lookup.Count >= _maxCount) && _lru.Count > 0)
            {
                var oldestNode = _lru.Last!;
                var oldestSound = oldestNode.Value;

                _lru.RemoveLast();
                _lookup.Remove(oldestSound);

                if (oldestSound.AudioData is not null)
                {
                    _currentBytes -= (long)oldestSound.AudioData.Length * sizeof(float);
                    oldestSound.AudioData = null; // Free for GC
                }
            }

            // Admit to cache
            sound.AudioData = data;
            var node = _lru.AddFirst(sound);
            _lookup[sound] = node;
            _currentBytes += soundBytes;
        }
    }
}