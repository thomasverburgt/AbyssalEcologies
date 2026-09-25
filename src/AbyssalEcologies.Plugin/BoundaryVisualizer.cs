using System;
using System.Collections.Generic;
using AbyssalEcologies.Core;
using UnityEngine;

namespace AbyssalEcologies.Plugin;

internal static class BoundaryVisualizer
{
    public const int DefaultLifetimeSeconds = 90;
    public const int MinimumLifetimeSeconds = 10;
    public const int MaximumLifetimeSeconds = 300;

    private const int RingSegments = 96;
    private static readonly List<Material> Materials = new();
    private static GameObject? _root;

    public static int Show(GeneratedWorld world, int lifetimeSeconds)
    {
        if (world == null) throw new ArgumentNullException(nameof(world));
        if (lifetimeSeconds < MinimumLifetimeSeconds || lifetimeSeconds > MaximumLifetimeSeconds)
            throw new ArgumentOutOfRangeException(nameof(lifetimeSeconds), $"Boundary lifetime must be {MinimumLifetimeSeconds}-{MaximumLifetimeSeconds} seconds.");

        Hide();
        var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color")
            ?? throw new InvalidOperationException("No compatible unlit shader is available for boundary lines.");
        _root = new GameObject("AE Temporary Region Boundaries");
        try
        {
            for (var index = 0; index < world.Regions.Count; index++)
            {
                var region = world.Regions[index];
                var color = Color.HSVToRGB((index * 0.27f) % 1f, 0.8f, 1f);
                AddRing(_root.transform, shader, region, index, color);
                AddCenterMarker(_root.transform, shader, region, index, color);
            }

            UnityEngine.Object.Destroy(_root, lifetimeSeconds);
            foreach (var material in Materials)
                UnityEngine.Object.Destroy(material, lifetimeSeconds);
            return world.Regions.Count;
        }
        catch
        {
            Hide();
            throw;
        }
    }

    public static bool Hide()
    {
        var wasVisible = _root != null;
        if (_root != null) UnityEngine.Object.Destroy(_root);
        foreach (var material in Materials)
            if (material != null) UnityEngine.Object.Destroy(material);
        Materials.Clear();
        _root = null;
        return wasVisible;
    }

    private static void AddRing(Transform parent, Shader shader, GeneratedRegion region, int index, Color color)
    {
        var line = CreateLine(parent, shader, $"AE Region {index + 1} Boundary", color, 2.5f, RingSegments + 1);
        var center = region.Center;
        for (var segment = 0; segment <= RingSegments; segment++)
        {
            var angle = segment * Mathf.PI * 2f / RingSegments;
            line.SetPosition(segment, new Vector3(
                center.X + (Mathf.Cos(angle) * region.Radius),
                center.Y + 3f,
                center.Z + (Mathf.Sin(angle) * region.Radius)));
        }
    }

    private static void AddCenterMarker(Transform parent, Shader shader, GeneratedRegion region, int index, Color color)
    {
        var line = CreateLine(parent, shader, $"AE Region {index + 1} Center", color, 4f, 2);
        line.SetPosition(0, new Vector3(region.Center.X, region.Center.Y - 15f, region.Center.Z));
        line.SetPosition(1, new Vector3(region.Center.X, region.Center.Y + 15f, region.Center.Z));
    }

    private static LineRenderer CreateLine(Transform parent, Shader shader, string name, Color color, float width, int positionCount)
    {
        var lineObject = new GameObject(name);
        lineObject.transform.SetParent(parent, worldPositionStays: true);
        var line = lineObject.AddComponent<LineRenderer>();
        var material = new Material(shader) { color = color };
        Materials.Add(material);
        line.sharedMaterial = material;
        line.useWorldSpace = true;
        line.positionCount = positionCount;
        line.startWidth = width;
        line.endWidth = width;
        line.startColor = color;
        line.endColor = color;
        line.numCapVertices = 2;
        return line;
    }
}
