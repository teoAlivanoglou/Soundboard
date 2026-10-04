using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using UserControl = System.Windows.Controls.UserControl;

namespace Soundboard.UI.Controls
{
    /// <summary>
    /// Interaction logic for SoundPad.xaml
    /// </summary>
    public partial class SoundPad : UserControl
    {
        public SoundPad()
        {
            InitializeComponent();
        }


        private const double FontRatio = 1.17;
        private const double MaxFontSize = 12.0;

        private double _lineHeight = 14.0;
        private int _visibleLines = 3;

        private void TitleTextArea_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var availableHeight = TitleTextArea.ActualHeight;
            if (availableHeight <= 10) return;

            var lines = 2;
            var lineHeight = availableHeight / lines;
            var fontSize = lineHeight / FontRatio;

            while (fontSize > MaxFontSize)
            {
                lines++;
                lineHeight = availableHeight / lines;
                fontSize = lineHeight / FontRatio;
            }

            _visibleLines = lines;
            _lineHeight = lineHeight;

            TitleText.FontSize = fontSize;
            TitleText.LineHeight = lineHeight;
            TitleText.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
        }


        private void SoundPad_OnMouseEnter(object sender, MouseEventArgs e)
        {
            var totalLines = Math.Round(TitleText.ActualHeight / _lineHeight);
            var overflowLines = Math.Max(0, totalLines - _visibleLines);

            if (overflowLines <= 0) return;

            var rollDistance = overflowLines * _lineHeight;
            var anim = new DoubleAnimation
            {
                From = 0,
                To = -rollDistance,
                Duration = TimeSpan.FromSeconds(Math.Max(1.5, overflowLines * 0.9)),
                BeginTime = TimeSpan.FromSeconds(0.4),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            TextRollTransform.BeginAnimation(TranslateTransform.YProperty, anim);
        }

        private void SoundPad_MouseLeave(object sender, MouseEventArgs e)
        {
            TextRollTransform.BeginAnimation(TranslateTransform.YProperty, null);
            TextRollTransform.Y = 0;
        }
    }
}