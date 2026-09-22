using System;
using System.Collections.Generic;

namespace AbyssalEcologies.Core;

public readonly struct WorldPoint : IEquatable<WorldPoint>
{
    public WorldPoint(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public float X { get; }
    public float Y { get; }
    public float Z { get; }

    public float HorizontalDistanceSquared(WorldPoint other)
    {
        var x = X - other.X;
        var z = Z - other.Z;
        return (x * x) + (z * z);
    }

    public bool Equals(WorldPoint other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
    public override bool Equals(object? obj) => obj is WorldPoint other && Equals(other);
    public override int GetHashCode() => (X, Y, Z).GetHashCode();
    public override string ToString() => $"({X:0.0}, {Y:0.0}, {Z:0.0})";
}

public enum PlacementKind
{
    Creature,
    Flora,
    Landmark
}

public sealed class GeneratedPlacement
{
    public GeneratedPlacement(
        string contentId,
        PlacementKind kind,
        WorldPoint position,
        WorldPoint eulerAngles,
        float scale)
    {
        ContentId = contentId;
        Kind = kind;
        Position = position;
        EulerAngles = eulerAngles;
        Scale = scale;
    }

    public string ContentId { get; }
    public PlacementKind Kind { get; }
    public WorldPoint Position { get; }
    public WorldPoint EulerAngles { get; }
    public float Scale { get; }
}

public sealed class GeneratedRegion
{
    public GeneratedRegion(string archetypeId, string displayName, WorldPoint center, float radius, IReadOnlyList<GeneratedPlacement> placements)
    {
        ArchetypeId = archetypeId;
        DisplayName = displayName;
        Center = center;
        Radius = radius;
        Placements = placements;
    }

    public string ArchetypeId { get; }
    public string DisplayName { get; }
    public WorldPoint Center { get; }
    public float Radius { get; }
    public IReadOnlyList<GeneratedPlacement> Placements { get; }
}

public sealed class GeneratedWorld
{
    public GeneratedWorld(int seed, IReadOnlyList<GeneratedRegion> regions)
    {
        Seed = seed;
        Regions = regions;
    }

    public int Seed { get; }
    public IReadOnlyList<GeneratedRegion> Regions { get; }
}

