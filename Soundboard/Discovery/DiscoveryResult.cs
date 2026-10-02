using System;
using System.Collections.Generic;
using System.Text;

namespace Soundboard.Discovery
{
    public record DiscoveryResult(
        IReadOnlyList<CategoryModel> Categories, 
        IReadOnlyList<SoundModel> Sounds
    );
}
