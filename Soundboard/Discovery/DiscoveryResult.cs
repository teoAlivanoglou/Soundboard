namespace Soundboard.Discovery;

public record DiscoveryResult(
    IReadOnlyList<CategoryModel> Categories,
    IReadOnlyList<SoundModel> Sounds
);