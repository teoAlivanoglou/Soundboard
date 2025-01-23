using NAudio.Wave;

namespace Soundboard.AudioEngine
{
    public class AutoDisposeFileReader(AudioFileReader reader) : ISampleProvider
    {
        private bool _isDisposed;

        public int Read(float[] buffer, int offset, int count)
        {
            if (_isDisposed)
                return 0;
            var read = reader.Read(buffer, offset, count);
            if (read != 0) return read;
            
            reader.Dispose();
            _isDisposed = true;
            return read;
        }

        public WaveFormat WaveFormat { get; private set; } = reader.WaveFormat;
    }
}
