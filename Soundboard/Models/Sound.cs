using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NAudio.SoundFile;
using NAudio.Wave;

// ReSharper disable InconsistentNaming

namespace Soundboard.Models
{
    public partial class Sound : ObservableObject
    {
        [ObservableProperty] private string? name;
        [ObservableProperty] private string? category;
        [ObservableProperty] private string? fullCategory;
        [ObservableProperty] private double progress = 0; //Random.Shared.NextDouble();
        [ObservableProperty] private TimeSpan duration; //Random.Shared.NextDouble();
        [ObservableProperty] private bool isPlaying = false;
        [ObservableProperty] private bool isVisible = true;

        public int Group { get; set; }
        public bool topLevel { get; set; } = false;
        public string? FilePath;

        public float[]? AudioData { get; private set; }
        public byte[]? ByteData { get; private set; }
        public WaveFormat WaveFormat { get; private set; }


        private static readonly Dictionary<string, Dictionary<string, int>> categoryGroupsComplex = new Dictionary<string, Dictionary<string, int>>();



        public Sound(string name, Stream? memoryStream, string? category = null, bool cache = true,
            int group = 0)
        {
            this.name = name;
            this.category = category;
            this.fullCategory = category;
            this.Group = group;

            var settings = new MediaFoundationReader.MediaFoundationReaderSettings
            {
                RequestFloatOutput = true
            };

            using var audioFileReader = new StreamMediaFoundationReader(memoryStream, settings);
            var sampleProvider = audioFileReader.ToSampleProvider();

            WaveFormat = sampleProvider.WaveFormat;
            var wholeFile = new List<float>((int)(audioFileReader.Length / 4));
            var readBuffer = new float[sampleProvider.WaveFormat.SampleRate * sampleProvider.WaveFormat.Channels];
            int samplesRead;
            while ((samplesRead = sampleProvider.Read(readBuffer.AsSpan())) > 0)
            {
                wholeFile.AddRange(readBuffer.Take(samplesRead));
            }

            AudioData = wholeFile.ToArray();

            duration = TimeSpan.FromSeconds(
                sampleProvider.WaveFormat.ExtraSize + (audioFileReader.Length / 4.0)
                / (double)(sampleProvider.WaveFormat.Channels * sampleProvider.WaveFormat.SampleRate));
        }


        public Sound(string name, string? filePath, string? category = null, bool cache = false, int group = 0)
        {
            this.name = name;
            this.category = category;
            this.fullCategory = category;
            this.Group = group;
            FilePath = filePath;

            //using var audioFileReader = new AudioFileReader(filePath);
            using var audioFileReader = new SoundFileReader(filePath);

            if (!cache)
            {
                WaveFormat = audioFileReader.WaveFormat;
                var wholeFile = new List<float>((int)(audioFileReader.Length / 4));
                var readBuffer = new float[audioFileReader.WaveFormat.SampleRate * audioFileReader.WaveFormat.Channels];
                int samplesRead;
                while ((samplesRead = audioFileReader.Read(readBuffer.AsSpan())) > 0)
                {
                    wholeFile.AddRange(readBuffer.Take(samplesRead));
                }

                AudioData = wholeFile.ToArray();
            }

            duration = TimeSpan.FromSeconds(
                audioFileReader.WaveFormat.ExtraSize + (audioFileReader.Length / 4.0)
                / (double)(audioFileReader.WaveFormat.Channels * audioFileReader.WaveFormat.SampleRate));
        }

        public static void ResetGroups()
        {
            categoryGroupsComplex.Clear();
        }



        public static int GetGroupIndex(Sound sound)
        {
            Debug.Assert(sound.Category != null, "sound.Category != null");
            Debug.Assert(sound.FullCategory != null, "sound.FullCategory != null");


            if (categoryGroupsComplex.ContainsKey(sound.Category))
            {
                if (categoryGroupsComplex[sound.Category].ContainsKey(sound.FullCategory))
                {
                    return categoryGroupsComplex[sound.Category][sound.FullCategory];
                }
                else
                {
                    var g = categoryGroupsComplex[sound.Category].Count;
                    categoryGroupsComplex[sound.Category][sound.FullCategory] = g;
                    return g;
                }
            }
            else
            {
                categoryGroupsComplex[sound.Category] = new Dictionary<string, int>();
                categoryGroupsComplex[sound.Category][sound.FullCategory] = 0;
                return 0;
            }


        }
    }
}