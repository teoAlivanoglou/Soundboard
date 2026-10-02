using NAudio.Wave;

namespace Soundboard.AudioEngine;

//TODO: I'm 90% sure this is poopy and can be massively improved.
/// <summary>
/// Sample Provider to allow fading in and out
/// </summary>
public class DelayFadeOutSampleProvider : ISampleProvider
{
    private readonly Lock _lockObject = new();
    private readonly ISampleProvider _source;

    private int _fadeInStart;
    private int _fadeInSamples;
    private int _fadeOutStart;
    private int _fadeOutSamples;
    private int _position;

    /// <summary>
    /// Creates a new FadeInOutSampleProvider
    /// </summary>
    /// <param name="source">The source stream with the audio to be faded in or out</param>
    public DelayFadeOutSampleProvider(ISampleProvider source)
    {
        this._source = source;
    }

    public void SetFadeIn(TimeSpan fadeInLength, TimeSpan fadeInStartPosition)
    {
        lock (_lockObject)
        {
            _fadeInStart = (int)(fadeInStartPosition.TotalSeconds * _source.WaveFormat.SampleRate *
                                 _source.WaveFormat.Channels);
            _fadeInSamples = (int)(fadeInLength.TotalSeconds * _source.WaveFormat.SampleRate *
                                   _source.WaveFormat.Channels);
        }
    }

    public void SetFadeIn(TimeSpan fadeInLength)
    {
        lock (_lockObject)
        {
            _fadeInStart = 0;
            _fadeInSamples = (int)(fadeInLength.TotalSeconds * _source.WaveFormat.SampleRate *
                                   _source.WaveFormat.Channels);
        }
    }

    public void SetFadeIn(double fadeDurationInMilliseconds)
    {
        lock (_lockObject)
        {
            _fadeInStart = 0;
            _fadeInSamples =
                (int)((fadeDurationInMilliseconds * _source.WaveFormat.SampleRate * _source.WaveFormat.Channels) /
                      1000);
        }
    }

    /// <summary>
    /// Requests that a fade-out begins (will start on the next call to Read)
    /// </summary>
    /// <param name="fadeAfterMilliseconds">Time in milliseconds after which the fade-out should start</param>
    /// <param name="fadeDurationInMilliseconds">Duration of fade in milliseconds</param>
    public void SetFadeOut(double fadeAfterMilliseconds, double fadeDurationInMilliseconds)
    {
        lock (_lockObject)
        {
            _fadeOutStart =
                (int)((fadeAfterMilliseconds * _source.WaveFormat.SampleRate * _source.WaveFormat.Channels) / 1000);
            _fadeOutSamples =
                (int)((fadeDurationInMilliseconds * _source.WaveFormat.SampleRate * _source.WaveFormat.Channels) /
                      1000);
        }
    }
    /// <summary>
    /// Sets the fade-out parameters for the end of the audio
    /// </summary>
    /// <param name="fadeOutLength">The length of the fade-out</param>
    /// <param name="sourceLength">The length of the source audio</param>
    public void FadeEnding(TimeSpan fadeOutLength, TimeSpan sourceLength)
    {
        lock (_lockObject)
        {
            var wave = _source as IWaveProvider;
            _fadeOutStart = (int)((sourceLength - fadeOutLength).TotalSeconds * _source.WaveFormat.SampleRate *
                                  _source.WaveFormat.Channels);
            _fadeOutSamples = (int)(fadeOutLength.TotalSeconds * _source.WaveFormat.SampleRate *
                                    _source.WaveFormat.Channels);
        }
    }

    /// <summary>
    /// Reads samples from this sample provider
    /// </summary>
    /// <param name="buffer">Buffer to read into</param>
    /// <returns>Number of samples read</returns>
    public int Read(Span<float> buffer)
    {
        var sourceSamplesRead = _source.Read(buffer);
        lock (_lockObject)
        {
            for (var i = 0; i < sourceSamplesRead; i++)
            {
                var samplePos = _position + i;
                if (_fadeInSamples > 0)
                {
                    if (samplePos < _fadeInStart)
                    {
                        buffer[i] = 0;
                    }
                    else if (samplePos >= _fadeInStart && samplePos < (_fadeInStart + _fadeInSamples))
                    {
                        buffer[i] *= (samplePos - _fadeInStart) / (float)_fadeInSamples;
                    }
                }

                if (_fadeOutSamples <= 0) continue;
                if (samplePos >= _fadeOutStart && samplePos < (_fadeOutStart + _fadeOutSamples))
                {
                    buffer[i] *= (1 - ((samplePos - _fadeOutStart) / (float)_fadeOutSamples));
                }
                else if (samplePos >= _fadeOutStart + _fadeOutSamples)
                {
                    buffer[i] = 0;
                }
            }

            _position += sourceSamplesRead;
        }

        return sourceSamplesRead;
    }

    /// <summary>
    /// WaveFormat of this SampleProvider
    /// </summary>
    public WaveFormat WaveFormat => _source.WaveFormat;
}