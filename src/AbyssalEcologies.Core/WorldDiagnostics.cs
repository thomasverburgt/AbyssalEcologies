using System;
using System.Collections.Generic;
using System.Linq;

namespace AbyssalEcologies.Core;

public sealed class WorldValidationReport
{
    internal WorldValidationReport(int regionCount, int placementCount, IReadOnlyList<string> errors, IReadOnlyList<string> warnings)
    {
        RegionCount = regionCount;
        PlacementCount = placementCount;
        Errors = errors;
        Warnings = warnings;
    }

    public int RegionCount { get; }
    public int PlacementCount { get; }
    public IReadOnlyList<string> Errors { get; }
    public IReadOnlyList<string> Warnings { get; }
    public bool IsValid => Errors.Count == 0;
}

public static class WorldDiagnostics
{
    public static WorldValidationReport Validate(GeneratedWorld world, bool enforceCurrentExclusions = true)
    {
        if (world == null) throw new ArgumentNullException(nameof(world));

        var errors = new List<string>();
        var warnings = new List<string>();
        var placementCount = 0;

        var regions = world.Regions;
        if (regions == null)
        {
            errors.Add("World contains no regions.");
            return new WorldValidationReport(0, 0, errors, warnings);
        }
        if (regions.Count == 0)
            errors.Add("World contains no regions.");

        for (var regionIndex = 0; regionIndex < regions.Count; regionIndex++)
        {
            var region = regions[regionIndex];
            var label = $"Region {regionIndex + 1}";
            if (region == null)
            {
                errors.Add($"{label} is null.");
                continue;
            }

            label = $"Region {regionIndex + 1} '{region.DisplayName}'";
            if (string.IsNullOrWhiteSpace(region.ArchetypeId) || string.IsNullOrWhiteSpace(region.DisplayName))
                errors.Add($"{label} has an invalid identity.");
            if (!IsFinite(region.Center) || !IsFinite(region.Radius) || region.Radius <= 0f)
                errors.Add($"{label} has invalid center or radius values.");
            if (WorldProtectionCatalog.TryFindExclusion(region.Center, 140f, out var centerArea))
                AddExclusionFinding($"{label} center enters protected area '{centerArea}'.", enforceCurrentExclusions, errors, warnings);
            if (region.Placements == null || region.Placements.Count == 0)
            {
                errors.Add($"{label} contains no placements.");
                continue;
            }

            placementCount += region.Placements.Count;
            if (region.Placements.Count != 41)
                errors.Add($"{label} contains {region.Placements.Count} placements instead of 41.");

            var landmarkCount = region.Placements.Count(placement => placement != null && placement.Kind == PlacementKind.Landmark);
            if (landmarkCount != 1)
                errors.Add($"{label} contains {landmarkCount} landmarks instead of one.");

            for (var placementIndex = 0; placementIndex < region.Placements.Count; placementIndex++)
            {
                var placement = region.Placements[placementIndex];
                var placementLabel = $"{label}, placement {placementIndex + 1}";
                if (placement == null)
                {
                    errors.Add($"{placementLabel} is null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(placement.ContentId))
                    errors.Add($"{placementLabel} has no content identifier.");
                if (!IsFinite(placement.Position) || !IsFinite(placement.EulerAngles) || !IsFinite(placement.Scale) || placement.Scale <= 0f)
                    errors.Add($"{placementLabel} contains invalid numeric values.");
                if (placement.Position.HorizontalDistanceSquared(region.Center) > (region.Radius * region.Radius) + 0.1f)
                    errors.Add($"{placementLabel} lies outside the region radius.");

                var clearance = placement.Kind == PlacementKind.Landmark ? 35f : 8f;
                if (WorldProtectionCatalog.TryFindExclusion(placement.Position, clearance, out var placementArea))
                    AddExclusionFinding($"{placementLabel} enters protected area '{placementArea}'.", enforceCurrentExclusions, errors, warnings);
            }
        }

        return new WorldValidationReport(regions.Count, placementCount, errors, warnings);
    }

    private static void AddExclusionFinding(string message, bool enforceCurrentExclusions, ICollection<string> errors, ICollection<string> warnings)
    {
        if (enforceCurrentExclusions)
            errors.Add(message);
        else
            warnings.Add(message);
    }

    private static bool IsFinite(WorldPoint point) => IsFinite(point.X) && IsFinite(point.Y) && IsFinite(point.Z);
    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
