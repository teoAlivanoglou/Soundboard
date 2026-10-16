using System.Collections.Generic;

namespace SonicPad.Discovery;

public record DiscoveryResult(
    IReadOnlyList<CategoryModel> Categories,
    IReadOnlyList<SoundModel> Sounds
);