using System.Collections.Generic;

namespace Soundboard.Avalonia.Discovery;

public record DiscoveryResult(
    IReadOnlyList<CategoryModel> Categories,
    IReadOnlyList<SoundModel> Sounds
);