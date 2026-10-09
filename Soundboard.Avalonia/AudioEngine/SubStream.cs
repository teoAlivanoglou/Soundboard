using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Rendering.Composition.Animations;

namespace Soundboard.Avalonia.AudioEngine;

public class SubStream : Stream
{
    private readonly Stream _baseStream;
    private readonly long _offset;
    private readonly long _length;
    private long _position;
    private readonly bool _leaveOpen;


    public SubStream(Stream baseStream, long offset, long length, bool leaveOpen = false)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(offset, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 0);
        ArgumentOutOfRangeException.ThrowIfNotEqual(baseStream.CanSeek, true);
        ArgumentOutOfRangeException.ThrowIfNotEqual(baseStream.CanRead, true);

        _baseStream = baseStream;
        _offset = offset;
        _length = length;
        _leaveOpen = leaveOpen;
        _position = 0;
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer)
    {
        if (_position >= _length)
            return 0;

        var remaining = _length - _position;
        var bytesToRead = (int)Math.Min(buffer.Length, remaining);
        _baseStream.Position = _offset + _position;

        var slice = buffer[..bytesToRead];

        _baseStream.Position = _offset + _position;
        var bytesRead = _baseStream.Read(slice);
        _position += bytesRead;

        return bytesRead;

    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var target = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => _length - Math.Abs(offset),
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };

        if (target < 0)
            throw new IOException("Cannot seek before the beginning of the substream.");

        _position = target;
        _baseStream.Position = _offset + _position;
        return _position;
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException("This is a readonly stream");
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException("This is a readonly stream");
    }

    public override async ValueTask DisposeAsync()
    {
        try
        {
            if (!_leaveOpen)
            {
                await _baseStream.DisposeAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_leaveOpen)
        {
            _baseStream.Dispose();
        }
        base.Dispose(disposing);
    }

    public override bool CanRead => _baseStream.CanRead;
    public override bool CanSeek => _baseStream.CanSeek;
    public override bool CanWrite => false;
    public override long Length => _length;

    public override long Position
    {
        get => _position;
        set => Seek(value, SeekOrigin.Begin);
    }
}