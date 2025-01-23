using System.Windows;
using System.Windows.Forms.VisualStyles;
using Microsoft.VisualBasic.Devices;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Soundboard.Models;
using MessageBox = System.Windows.Forms.MessageBox;

namespace Soundboard;

public class AudioPlayer
{
    private static Dictionary<Sound, int> soundPlayerRefs = new();
    HashSet<WaveOutEvent> players = new();

    private readonly IWavePlayer outputDevice;
    private readonly MixingSampleProvider mixer;

    public AudioPlayer()
    {
        outputDevice = new WaveOutEvent();
        mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
        mixer.ReadFully = true;
        outputDevice.Init(mixer);
        outputDevice.Play();

    }


    public async Task Play(Sound sound, int fadeInTime = 200)
    {

        await using var audioFile = new AudioFileReader(sound.FilePath);
        
        var fade = new DelayFadeOutSampleProvider(audioFile, false);

        fade.BeginFadeIn(fadeInTime);

        var outputDevice = new WaveOutEvent();


        players.Add(outputDevice);

        outputDevice.Init(fade);
        outputDevice.Play();
        var fmt = outputDevice.OutputWaveFormat;

        soundPlayerRefs.TryAdd(sound, 0);

        soundPlayerRefs[sound]++;
        sound.isPlaying = true;

        var myIndex = soundPlayerRefs[sound];
        var playbackStartTime = DateTime.UtcNow;
        var fadingOut = false;

        while (outputDevice.PlaybackState == PlaybackState.Playing)
        {
            var elapsed = (DateTime.UtcNow - playbackStartTime);
            if (myIndex == soundPlayerRefs[sound])
            {
                var preciseProgress = elapsed.TotalSeconds / audioFile.TotalTime.TotalSeconds;
                var currentProgress = audioFile.CurrentTime.TotalSeconds / audioFile.TotalTime.TotalSeconds;
                sound.Progress = Math.Clamp(preciseProgress, 0, 1);

            }

            if (fade.fadeState == DelayFadeOutSampleProvider.FadeState.FadingOut && !fadingOut)
            {
                fadingOut = true;
                // Task.Run(() => { MessageBox.Show("Fade out begin"); });
            }
            await Task.Delay(10);
        }

        if (sound.Progress > 0.99)
        {
            sound.Progress = 0;
            soundPlayerRefs[sound] = 0;
            sound.IsPlaying = false;
        }

        outputDevice.Dispose();
        players.Remove(outputDevice);
    }

    public Task Abort()
    {
        foreach (var player in players)
        {
            player.Stop();
            players.Remove(player);
            player.Dispose();
        }

        foreach (var sound in soundPlayerRefs.Keys)
        {
            sound.Progress = 0;
            sound.IsPlaying = false;        
            soundPlayerRefs[sound] = 0;
        }

        return Task.CompletedTask;
    }
}