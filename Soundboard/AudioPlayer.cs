using System.Windows;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Soundboard.Models;

namespace Soundboard;

public class AudioPlayer
{
    private static Dictionary<Sound, int> soundPlayerRefs = new();
    HashSet<WaveOutEvent> players = new();

    public AudioPlayer()
    {
    }


    public async Task Play(Sound sound)
    {
        await using var audioFile = new AudioFileReader(sound.FilePath);
        var outputDevice = new WaveOutEvent();

        players.Add(outputDevice);

        outputDevice.Init(audioFile);
        outputDevice.Play();

        outputDevice.PlaybackStopped += (sender, args) =>
        {
            soundPlayerRefs[sound] = 0;
            sound.Progress = 0;
            sound.IsPlaying = false;
        };

        soundPlayerRefs.TryAdd(sound, 0);

        soundPlayerRefs[sound]++;
        sound.isPlaying = true;

        var myIndex = soundPlayerRefs[sound];
        var playbackStartTime = DateTime.UtcNow;

        while (outputDevice.PlaybackState == PlaybackState.Playing)
        {
            if (myIndex == soundPlayerRefs[sound])
            {
                var elapsed = (DateTime.UtcNow - playbackStartTime).TotalSeconds;
                var preciseProgress = elapsed / audioFile.TotalTime.TotalSeconds;
                var currentProgress = audioFile.CurrentTime.TotalSeconds / audioFile.TotalTime.TotalSeconds;
                sound.Progress = Math.Clamp(preciseProgress, 0, 1);
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

        return Task.CompletedTask;
    }
}