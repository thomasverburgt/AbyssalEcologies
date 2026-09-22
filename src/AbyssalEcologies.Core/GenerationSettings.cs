using System;

namespace AbyssalEcologies.Core;

public sealed class GenerationSettings
{
    public int Seed { get; set; } = 451230;
    public int RegionCount { get; set; } = 3;
    public float MinimumMapRadius { get; set; } = 550f;
    public float MaximumMapRadius { get; set; } = 1350f;
    public float MinimumDepth { get; set; } = 180f;
    public float MaximumDepth { get; set; } = 520f;
    public float MinimumRegionSeparation { get; set; } = 420f;

    public void Validate()
    {
        if (RegionCount < 1 || RegionCount > 12)
            throw new ArgumentOutOfRangeException(nameof(RegionCount), "Region count must be between 1 and 12.");
        if (MinimumMapRadius < 300f || MaximumMapRadius <= MinimumMapRadius)
            throw new ArgumentOutOfRangeException(nameof(MaximumMapRadius), "Map radii must form a valid annulus outside the starting area.");
        if (MinimumDepth < 25f || MaximumDepth <= MinimumDepth)
            throw new ArgumentOutOfRangeException(nameof(MaximumDepth), "Depth range is invalid.");
        if (MinimumRegionSeparation < 100f)
            throw new ArgumentOutOfRangeException(nameof(MinimumRegionSeparation), "Region separation is too small.");
    }
}

