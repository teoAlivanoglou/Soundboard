using System;
using System.IO;

namespace Soundboard.Avalonia.AudioEngine;

public class VorbisWavHelper
{
    private const ushort WaveFormatOggVorbisMode1 = 0x674F;
    private const ushort WaveFormatOggVorbisMode2 = 0x6750;

    /// <summary>
    /// If the stream is an FL Studio Vorbis-in-WAV file, returns a SubStream wrapping
    /// the embedded OggS stream. Otherwise, resets stream.Position to 0 and returns the stream.
    /// </summary>
    public static Stream WrapIfVorbisWav(Stream stream, bool leaveOpen = false)
    {
        if (TryFindVorbisChunk(stream, out var offset, out var length))
        {
            return new SubStream(stream, offset, length, leaveOpen);
        }

        stream.Position = 0;
        return stream;
    }


    //Positions   Sample Value         Description
    // 1 - 4       "RIFF"               Marks the file as a riff file. Characters are each 1. byte long.
    // 5 - 8       File size (integer)  Size of the overall file - 8 bytes, in bytes (32-bit integer). Typically, you'd fill this in after creation.
    // 9 -12       "WAVE"               File Type Header. For our purposes, it always equals "WAVE".
    // 13-16       "fmt "               Format chunk marker. Includes trailing null
    // 17-20       16                   Length of format data as listed above
    // 21-22       1                    Type of format (1 is PCM) - 2 byte integer
    // 23-24       2                    Number of Channels - 2 byte integer
    // 25-28       44100                Sample Rate - 32 bit integer. Common values are 44100 (CD), 48000 (DAT). Sample Rate = Number of Samples per second, or Hertz.
    // 29-32       176400               (Sample Rate * BitsPerSample * Channels) / 8.
    // 33-34       4                    (BitsPerSample * Channels) / 8.1 - 8 bit mono2 - 8 bit stereo/16 bit mono4 - 16 bit stereo
    // 35-36       16                   Bits per sample
    // 37-40       "data"               "data" chunk header. Marks the beginning of the data section.
    // 41-44       File size (data)     Size of the data section, i.e. file size - 44 bytes header.
    private static bool TryFindVorbisChunk(Stream stream, out long dataOffset, out long dataLength)
    {
        dataOffset = 0;
        dataLength = 0;

        if (!stream.CanSeek || stream.Length < 44)
            return false;

        stream.Position = 0;
        using var reader = new BinaryReader(stream, System.Text.Encoding.ASCII, true);

        var riffId = reader.ReadBytes(4);
        if (!riffId.AsSpan().SequenceEqual("RIFF"u8)) return false;

        // Position = 4
        reader.ReadUInt32(); // riff size 

        var waveId = reader.ReadBytes(4);
        if (!waveId.AsSpan().SequenceEqual("WAVE"u8)) return false;

        var isVorbisTag = false;
        var foundOggData = false;

        while (stream.Position + 8 <= stream.Length)
        {
            var chunkId = reader.ReadBytes(4);
            var chunkSize = reader.ReadUInt32();

            var chunkDataStart = stream.Position;

            if (chunkId.AsSpan().SequenceEqual("fmt "u8) && chunkSize >= 2)
            {
                var formatTag = reader.ReadUInt16();
                if (formatTag is WaveFormatOggVorbisMode1 or WaveFormatOggVorbisMode2)
                {
                    isVorbisTag = true;
                }
            }
            else if (chunkId.AsSpan().SequenceEqual("data"u8) && chunkSize >= 4)
            {
                var magic = reader.ReadBytes(4);
                if (magic.AsSpan().SequenceEqual("OggS"u8))
                {
                    foundOggData = true;
                    dataOffset = chunkDataStart;
                    dataLength = chunkSize;
                }
            }

            var paddedSize = (chunkSize + 1) & ~1L; // I've read that in a book somewhere
            stream.Position = chunkDataStart + paddedSize;
        }

        return isVorbisTag && foundOggData;
    }
}