using System;
using System.Collections.Generic;
using System.Linq;

namespace AbyssalEcologies.Core;

public sealed class ProceduralWorldGenerator
{
    private const int PlacementSeedStride = 104729;

    private static readonly RegionArchetype[] Archetypes =
    {
        new("glass-kelp-garden", "Glass Kelp Garden", 115f, "glassfin", "prism-kelp", "glass-arch"),
        new("ember-trench", "Ember Trench", 100f, "cinder-ray", "ember-fan", "thermal-spire"),
        new("ghostlight-nursery", "Ghostlight Nursery", 125f, "lantern-skate", "ghost-bloom", "nursery-heart")
    };

    public GeneratedWorld Generate(GenerationSettings settings)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));
        settings.Validate();

        var random = new StableRandom(settings.Seed);
        var centers = GenerateCenters(settings, random);
        var regions = new List<GeneratedRegion>(centers.Count);
        var archetypeOrder = CreateShuffledArchetypeOrder(random);

        for (var i = 0; i < centers.Count; i++)
        {
            if (i > 0 && i % Archetypes.Length == 0)
                Shuffle(archetypeOrder, random);
            var archetype = Archetypes[archetypeOrder[i % Archetypes.Length]];
            var placementRandom = new StableRandom(unchecked(settings.Seed + ((i + 1) * PlacementSeedStride)));
            var placements = GeneratePlacements(archetype, centers[i], placementRandom);
            regions.Add(new GeneratedRegion(archetype.Id, archetype.DisplayName, centers[i], archetype.Radius, placements));
        }

        return new GeneratedWorld(settings.Seed, regions);
    }

    private static int[] CreateShuffledArchetypeOrder(StableRandom random)
    {
        var order = Enumerable.Range(0, Archetypes.Length).ToArray();
        Shuffle(order, random);
        return order;
    }

    private static void Shuffle(int[] values, StableRandom random)
    {
        for (var i = values.Length - 1; i > 0; i--)
        {
            var swapIndex = random.Range(0, i + 1);
            var value = values[i];
            values[i] = values[swapIndex];
            values[swapIndex] = value;
        }
    }

    private static List<WorldPoint> GenerateCenters(GenerationSettings settings, StableRandom random)
    {
        var result = new List<WorldPoint>(settings.RegionCount);
        var minimumSeparationSquared = settings.MinimumRegionSeparation * settings.MinimumRegionSeparation;

        for (var attempts = 0; attempts < settings.RegionCount * 80 && result.Count < settings.RegionCount; attempts++)
        {
            var angle = random.Range(0f, (float)Math.PI * 2f);
            var radius = (float)Math.Sqrt(random.Range(
                settings.MinimumMapRadius * settings.MinimumMapRadius,
                settings.MaximumMapRadius * settings.MaximumMapRadius));
            var candidate = new WorldPoint(
                (float)Math.Cos(angle) * radius,
                -random.Range(settings.MinimumDepth, settings.MaximumDepth),
                (float)Math.Sin(angle) * radius);

            if (result.All(existing => existing.HorizontalDistanceSquared(candidate) >= minimumSeparationSquared))
                result.Add(candidate);
        }

        if (result.Count != settings.RegionCount)
            throw new InvalidOperationException("Could not place all regions within the configured separation constraints.");

        return result;
    }

    private static IReadOnlyList<GeneratedPlacement> GeneratePlacements(RegionArchetype archetype, WorldPoint center, StableRandom random)
    {
        var placements = new List<GeneratedPlacement>();
        placements.Add(new GeneratedPlacement(archetype.LandmarkId, PlacementKind.Landmark, center, Rotation(random), random.Range(1.8f, 2.5f)));

        for (var i = 0; i < 28; i++)
            placements.Add(CreatePlacement(archetype.FloraId, PlacementKind.Flora, center, archetype.Radius, random, -20f, 15f, 0.65f, 1.6f));

        for (var i = 0; i < 12; i++)
            placements.Add(CreatePlacement(archetype.CreatureId, PlacementKind.Creature, center, archetype.Radius * 0.85f, random, 5f, 55f, 0.8f, 1.25f));

        return placements;
    }

    private static GeneratedPlacement CreatePlacement(
        string contentId,
        PlacementKind kind,
        WorldPoint center,
        float radius,
        StableRandom random,
        float minimumY,
        float maximumY,
        float minimumScale,
        float maximumScale)
    {
        var angle = random.Range(0f, (float)Math.PI * 2f);
        var distance = (float)Math.Sqrt(random.NextFloat()) * radius;
        var position = new WorldPoint(
            center.X + ((float)Math.Cos(angle) * distance),
            center.Y + random.Range(minimumY, maximumY),
            center.Z + ((float)Math.Sin(angle) * distance));
        return new GeneratedPlacement(contentId, kind, position, Rotation(random), random.Range(minimumScale, maximumScale));
    }

    private static WorldPoint Rotation(StableRandom random) => new(0f, random.Range(0f, 360f), 0f);

    private sealed class RegionArchetype
    {
        public RegionArchetype(string id, string displayName, float radius, string creatureId, string floraId, string landmarkId)
        {
            Id = id;
            DisplayName = displayName;
            Radius = radius;
            CreatureId = creatureId;
            FloraId = floraId;
            LandmarkId = landmarkId;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public float Radius { get; }
        public string CreatureId { get; }
        public string FloraId { get; }
        public string LandmarkId { get; }
    }
}
