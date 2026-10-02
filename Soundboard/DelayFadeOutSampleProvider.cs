using NAudio.Wave;

// TODO: Why UNUSED namespace when there are references to it?
namespace UNUSED;
/// <summary>
/// Sample Provider to allow fading in and out
/// </summary>
public class DelayFadeOutSampleProvider : ISampleProvider
{
    public enum FadeState
    {
        Silence,
        FadingIn,
        FullVolume,
        FadingOut,
    }

    private readonly object lockObject = new object();
    private readonly ISampleProvider source;
    private int fadeSamplePosition;
    private int fadeSampleCount;
    private int fadeOutDelaySamples;
    private int fadeOutDelayPosition;
    public FadeState fadeState { get; private set; }

    /// <summary>
    /// Creates a new FadeInOutSampleProvider
    /// </summary>
    /// <param name="source">The source stream with the audio to be faded in or out</param>
    /// <param name="initiallySilent">If true, we start faded out</param>
    public DelayFadeOutSampleProvider(ISampleProvider source, bool initiallySilent = false)
    {
        this.source = source;
        this.fadeState = initiallySilent ? FadeState.Silence : FadeState.FullVolume;
    }

    /// <summary>
    /// Requests that a fade-in begins (will start on the next call to Read)
    /// </summary>
    /// <param name="fadeDurationInMilliseconds">Duration of fade in milliseconds</param>
    public void BeginFadeIn(double fadeDurationInMilliseconds)
    {
        lock (lockObject)
        {
            fadeSamplePosition = 0;
            fadeSampleCount = (int)((fadeDurationInMilliseconds * source.WaveFormat.SampleRate) / 1000);
            fadeState = FadeState.FadingIn;
        }
    }

    /// <summary>
    /// Requests that a fade-out begins (will start on the next call to Read)
    /// </summary>
    /// <param name="fadeDurationInMilliseconds">Duration of fade in milliseconds</param>
    public void BeginFadeOut(double fadeAfterMilliseconds, double fadeDurationInMilliseconds)
    {
        lock (lockObject)
        {
            fadeSamplePosition = 0;
            fadeSampleCount = (int)((fadeDurationInMilliseconds * source.WaveFormat.SampleRate) / 1000);
            fadeOutDelaySamples = (int)((fadeAfterMilliseconds * source.WaveFormat.SampleRate) / 1000);
            fadeOutDelayPosition = 0;

            //fadeState = FadeState.FadingOut;
        }
    }

    /// <summary>
    /// Reads samples from this sample provider
    /// </summary>
    /// <param name="buffer">Buffer to read into</param>
    /// <param name="offset">Offset within buffer to write to</param>
    /// <param name="count">Number of samples desired</param>
    /// <returns>Number of samples read</returns>
    public int Read(Span<float> buffer)
    {
        int sourceSamplesRead = source.Read(buffer);

        lock (lockObject)
        {
            if (fadeOutDelaySamples > 0)
            {
                int oldFadeOutDelayPos = fadeOutDelayPosition;
                fadeOutDelayPosition += sourceSamplesRead / WaveFormat.Channels;
                if (fadeOutDelayPosition > fadeOutDelaySamples)
                {
                    int normalSamples = (fadeOutDelaySamples - oldFadeOutDelayPos) * WaveFormat.Channels;
                    int fadeOutSamples = (fadeOutDelayPosition - fadeOutDelaySamples) * WaveFormat.Channels;
                    // apply the fade-out only to the samples after fadeOutDelayPosition
                    FadeOut(buffer);

                    fadeOutDelaySamples = 0;
                    fadeState = FadeState.FadingOut;
                    return sourceSamplesRead;
                }
            }
            if (fadeState == FadeState.FadingIn)
            {
                FadeIn(buffer);
            }
            else if (fadeState == FadeState.FadingOut)
            {
                FadeOut(buffer);
            }
            else if (fadeState == FadeState.Silence)
            {
                ClearBuffer(buffer);
            }
        }
        return sourceSamplesRead;
    }



    private static void ClearBuffer(Span<float> buffer)
    {
        buffer.Clear();
    }

    private void FadeOut(Span<float> buffer)
    {
        int sample = 0;
        while (sample < buffer.Length)
        {
            float multiplier = 1.0f - (fadeSamplePosition / (float)fadeSampleCount);
            for (int ch = 0; ch < source.WaveFormat.Channels; ch++)
            {
                buffer[sample++] *= multiplier;
            }
            fadeSamplePosition++;
            if (fadeSamplePosition > fadeSampleCount)
            {
                fadeState = FadeState.Silence;
                // clear out the end
                ClearBuffer(buffer);
                break;
            }
        }
    }

    private void FadeIn(Span<float> buffer)
    {
        int sample = 0;
        while (sample < buffer.Length)
        {
            float multiplier = (fadeSamplePosition / (float)fadeSampleCount);
            for (int ch = 0; ch < source.WaveFormat.Channels; ch++)
            {
                buffer[sample++] *= multiplier;
            }
            fadeSamplePosition++;
            if (fadeSamplePosition > fadeSampleCount)
            {
                fadeState = FadeState.FullVolume;
                // no need to multiply any more
                break;
            }
        }
    }

    /// <summary>
    /// WaveFormat of this SampleProvider
    /// </summary>
    public WaveFormat WaveFormat
    {
        get { return source.WaveFormat; }
    }
}