using System.Collections.Generic;

namespace SonicPad.Discovery;

/// <summary>
/// Ordered discovery publication. Categories are supplied once, before decoding starts.
/// ProcessedCandidates includes skipped files; SuccessfulCount counts playable sounds only.
/// The consumer owns any mutations to categories after the initial publication.
/// </summary>
public sealed record DiscoveryUpdate(
    IReadOnlyList<CategoryModel>? Categories,
    int TotalCandidates,
    IReadOnlyList<SoundModel> Sounds,
    int ProcessedCandidates,
    int SuccessfulCount,
    bool IsComplete);
