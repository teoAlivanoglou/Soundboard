using System;
using System.Collections.Generic;
using System.IO;
using NAudio.SoundFile;
using NAudio.Wave;

namespace Soundboard.Avalonia.AudioEngine;

public class AudioDecoder
{
    public static bool TryDecode(string filePath, out float[]? audioData, out WaveFormat? waveFormat)
    {
        try
        {
            using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var stream = VorbisWavHelper.WrapIfVorbisWav(fileStream);
            using var reader = new SoundFileReader(stream);

            waveFormat = reader.WaveFormat;

            var samples = new List<float>((int)(reader.Length / sizeof(float)));
            var buffer = new float[16 * 1024]; // 16KB
            int read;

            while ((read = reader.Read(buffer.AsSpan())) > 0)
            {
                samples.AddRange(buffer.AsSpan(0, read));
            }

            audioData = [.. samples];
            return true;
        }
        catch
        {
            audioData = null;
            waveFormat = null;
            return false;
        }
    }
}