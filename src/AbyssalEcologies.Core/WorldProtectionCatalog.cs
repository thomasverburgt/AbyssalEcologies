using System;
using System.Collections.Generic;

namespace AbyssalEcologies.Core;

public static class WorldProtectionCatalog
{
    private static readonly ProtectedArea[] Areas =
    {
        new("aurora", 1030f, -180f, 650f),
        new("quarantine-enforcement-platform", 375f, 1100f, 280f),
        new("floating-island-degasi-bases", -800f, -1050f, 260f),

        new("lifepod-2", -490f, 1320f, 150f),
        new("lifepod-3", -35f, -400f, 150f),
        new("lifepod-4", 705f, 160f, 150f),
        new("lifepod-6", 360f, 310f, 150f),
        new("lifepod-7", -55f, -1040f, 150f),
        new("lifepod-12", -1120f, -685f, 150f),
        new("lifepod-13", -920f, 510f, 150f),
        new("lifepod-17", -515f, -55f, 150f),
        new("lifepod-19", -810f, -875f, 150f)
    };

    public static IReadOnlyList<ProtectedArea> All => Areas;

    public static bool TryFindExclusion(WorldPoint point, float clearance, out string areaId)
    {
        foreach (var area in Areas)
        {
            var x = point.X - area.X;
            var z = point.Z - area.Z;
            var radius = area.Radius + clearance;
            if ((x * x) + (z * z) < radius * radius)
            {
                areaId = area.Id;
                return true;
            }
        }

        areaId = string.Empty;
        return false;
    }
}

public sealed class ProtectedArea
{
    public ProtectedArea(string id, float x, float z, float radius)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Protected area id is required.", nameof(id));
        if (radius <= 0f) throw new ArgumentOutOfRangeException(nameof(radius));
        Id = id;
        X = x;
        Z = z;
        Radius = radius;
    }

    public string Id { get; }
    public float X { get; }
    public float Z { get; }
    public float Radius { get; }
}

public static class PlacementSearchPattern
{
    private const int SampleCount = 24;
    private const float GoldenAngleRadians = 2.39996323f;

    public static IEnumerable<WorldPoint> Around(WorldPoint origin, int stableOffset, float maximumRadius)
    {
        if (maximumRadius < 0f) throw new ArgumentOutOfRangeException(nameof(maximumRadius));
        yield return origin;

        var phase = (stableOffset & 1023) * ((float)Math.PI * 2f / 1024f);
        for (var index = 1; index <= SampleCount; index++)
        {
            var radius = maximumRadius * (float)Math.Sqrt(index / (float)SampleCount);
            var angle = phase + (index * GoldenAngleRadians);
            yield return new WorldPoint(
                origin.X + ((float)Math.Cos(angle) * radius),
                origin.Y,
                origin.Z + ((float)Math.Sin(angle) * radius));
        }
    }
}
