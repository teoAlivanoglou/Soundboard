using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace Soundboard.Discovery
{
    public class DesignSoundModel
    {
        public string Name { get; set; } = "Cleanest 808";
        public string Duration { get; set; } = "0:04";
        public string PadNumber { get; set; } = "#01";
        public bool IsPlaying { get; set; } = false;
        public double Progress { get; set; } = 0.05;
        public CategoryModel Category { get; set; } = new("BASS", 7, new SolidColorBrush(Color.FromHex("#38BDF8")));
    }
}
