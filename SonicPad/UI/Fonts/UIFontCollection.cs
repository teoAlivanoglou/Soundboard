using System;
using Avalonia.Media.Fonts;

namespace SonicPad.UI.Fonts;

public class UIFontCollection : EmbeddedFontCollection
{
    public UIFontCollection() : base(
        new Uri("fonts:Inter", UriKind.Absolute), 
        new Uri("avares://SonicPad/UI/Fonts", UriKind.Absolute)
        )
    {
    }
}