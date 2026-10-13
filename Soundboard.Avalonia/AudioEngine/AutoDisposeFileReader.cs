using System;
using NAudio.SoundFile;
using NAudio.Wave;

namespace Soundboard.Avalonia.AudioEngine;

public class AutoDisposeFileReader(SoundFileReader reader, IDisposable? streamToDispose = null) : ISampleProvider, IDisposable
{
    private bool _isDisposed;

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        reader.Dispose();
        streamToDispose?.Dispose();
    }

    public int Read(Span<float> buffer)
    {
        if (_isDisposed)
            return 0;

        var read = reader.Read(buffer);
        return read != 0 ? read : 0;
    }

    public WaveFormat WaveFormat { get; private set; } = reader.WaveFormat;
}