using System;
using Avalonia.Media.Fonts;

namespace Soundboard.Avalonia.UI.Fonts;

public class UIFontCollection : EmbeddedFontCollection
{
    public UIFontCollection() : base(
        new Uri("fonts:Inter", UriKind.Absolute), 
        new Uri("avares://Soundboard.Avalonia/UI/Fonts", UriKind.Absolute)
        )
    {
    }
}