using System;
using System.Collections.Generic;
using AbyssalEcologies.Core;
using UnityEngine;

namespace AbyssalEcologies.Plugin;

internal static class ExpeditionChunkPrototype
{
    public const string ConfirmationPhrase = "CONFIRM_EXPEDITION_GRID_SPIKE";
    public const string GotoName = "aechunk";
    public static readonly Vector3 Anchor = new(0f, -700f, 0f);
    public static readonly Vector3 Arrival = new(128f, -642f, 128f);
    private const int Subdivisions = 24;
    private static readonly ExpeditionChunkCoordinate[] Coordinates = { new(0, 0), new(1, 0), new(0, 1), new(1, 1) };
    private static readonly List<Mesh> Meshes = new();
    private static readonly List<Material> Materials = new();
    private static GameObject? _root;
    private static long _managedDelta;
    private static int _worldSeed;
    private static float _maximumSeamGap;

    public static bool IsActive => _root != null;

    public static string Create(int worldSeed, string confirmation)
    {
        if (confirmation != ConfirmationPhrase)
            return $"AE expedition grid refused. Usage: ae_chunk_create {ConfirmationPhrase}";
        if (_root != null)
            return "AE expedition grid refused: diagnostic terrain is already active. Run ae_chunk_status or ae_chunk_remove.";

        var before = GC.GetTotalMemory(false);
        _worldSeed = worldSeed;
        var settings = new ExpeditionGenerationSettings();
        var generator = new ExpeditionGenerator();
        _root = new GameObject("Abyssal Ecologies Diagnostic 2x2 Expedition Grid");
        _root.transform.position = Anchor;
        try
        {
            foreach (var coordinate in Coordinates)
                CreateChunk(_root.transform, generator, worldSeed, coordinate, settings);
            AddInspectionLights(_root.transform, settings);
            AddSeamMarkers(_root.transform, generator, worldSeed, settings);
            _maximumSeamGap = MeasureMaximumSeamGap(generator, worldSeed, settings);
            _managedDelta = GC.GetTotalMemory(false) - before;
        }
        catch (Exception exception)
        {
            CleanupRuntimeObjects();
            var failure = $"AE expedition grid creation FAILED and was rolled back: {exception.Message}";
            Plugin.Log.LogError($"{failure} {exception}");
            return failure;
        }

        var result = $"AE expedition grid CREATED: seed={worldSeed}, chunks={Coordinates.Length}, coordinates=(0,0)/(1,0)/(0,1)/(1,1), anchor={Anchor}, vertices={TotalVertices()}, triangles={TotalTriangles()}, colliders={ReadyColliderCount()}/{Coordinates.Length}, maxSeamGap={_maximumSeamGap:0.000000}m, inspectionLights=4, seamMarkers=2, estimatedMesh={EstimatedMeshBytes() / 1024d:0.0} KiB, managedDelta={_managedDelta / 1024d:+0.0;-0.0;0.0} KiB. Run 'goto {GotoName}', cross both bright seams, then leave and run ae_chunk_remove.";
        Plugin.Log.LogWarning(result);
        return result;
    }

    public static string Status()
    {
        if (_root == null)
            return "AE expedition grid: inactive; no diagnostic runtime terrain exists.";
        var playerDistance = Player.main == null ? -1f : Vector3.Distance(Player.main.transform.position, GridCenter());
        return $"AE expedition grid ACTIVE: seed={_worldSeed}, chunks={Coordinates.Length}, vertices={TotalVertices()}, triangles={TotalTriangles()}, colliders={ReadyColliderCount()}/{Coordinates.Length}, maxSeamGap={_maximumSeamGap:0.000000}m, inspectionLights=4, seamMarkers=2, estimatedMesh={EstimatedMeshBytes() / 1024d:0.0} KiB, managedDelta={_managedDelta / 1024d:+0.0;-0.0;0.0} KiB, playerDistance={playerDistance:0.0}m.";
    }

    public static string Remove()
    {
        if (_root == null)
            return "AE expedition grid was not active.";
        if (Player.main != null && Vector3.Distance(Player.main.transform.position, GridCenter()) < 450f)
            return "AE expedition grid removal REFUSED: player is within 450m of the test terrain. Run 'warp 0 0 0' first, then retry ae_chunk_remove.";

        CleanupRuntimeObjects();
        var result = "AE expedition grid REMOVED atomically: four chunk roots, colliders, meshes, materials, lights, and seam markers were scheduled for destruction. No manifest or save data was written.";
        Plugin.Log.LogWarning(result);
        return result;
    }

    private static void CreateChunk(Transform parent, ExpeditionGenerator generator, int worldSeed, ExpeditionChunkCoordinate coordinate, ExpeditionGenerationSettings settings)
    {
        var descriptor = generator.GenerateChunk(worldSeed, coordinate, settings);
        var chunk = new GameObject($"AE Diagnostic Chunk {coordinate.X},{coordinate.Z} [{descriptor.Biome}]");
        chunk.transform.SetParent(parent, false);
        chunk.transform.localPosition = new Vector3(coordinate.X * settings.ChunkSize, 0f, coordinate.Z * settings.ChunkSize);
        var mesh = BuildMesh(generator, worldSeed, coordinate, settings);
        Meshes.Add(mesh);
        chunk.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = chunk.AddComponent<MeshRenderer>();
        var material = CreateTerrainMaterial(descriptor.Biome, coordinate);
        Materials.Add(material);
        renderer.sharedMaterial = material;
        chunk.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    private static Material CreateTerrainMaterial(ExpeditionBiome biome, ExpeditionChunkCoordinate coordinate)
    {
        var shader = Shader.Find("MarmosetUBER") ?? Shader.Find("Standard") ?? throw new InvalidOperationException("No compatible terrain shader is available.");
        var color = biome switch
        {
            ExpeditionBiome.EmberTrench => new Color(0.23f, 0.065f, 0.025f, 1f),
            ExpeditionBiome.GlassKelpGarden => new Color(0.025f, 0.19f, 0.17f, 1f),
            _ => new Color(0.04f, 0.095f, 0.24f, 1f)
        };
        var glow = biome switch
        {
            ExpeditionBiome.EmberTrench => new Color(0.32f, 0.055f, 0.01f, 1f),
            ExpeditionBiome.GlassKelpGarden => new Color(0.015f, 0.2f, 0.18f, 1f),
            _ => new Color(0.035f, 0.12f, 0.32f, 1f)
        };
        var material = new Material(shader) { name = $"AE Diagnostic Terrain Material {coordinate.X},{coordinate.Z}" };
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_GlowColor")) material.SetColor("_GlowColor", glow);
        if (material.HasProperty("_GlowStrength")) material.SetFloat("_GlowStrength", 0.35f);
        return material;
    }

    private static void AddInspectionLights(Transform parent, ExpeditionGenerationSettings settings)
    {
        foreach (var coordinate in Coordinates)
        {
            var lightObject = new GameObject($"AE Temporary Inspection Light {coordinate.X},{coordinate.Z}");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = new Vector3(coordinate.X * settings.ChunkSize, 75f, coordinate.Z * settings.ChunkSize);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 215f;
            light.intensity = 1.35f;
            light.color = new Color(0.62f, 0.9f, 1f);
            light.shadows = LightShadows.None;
        }
    }

    private static void AddSeamMarkers(Transform parent, ExpeditionGenerator generator, int worldSeed, ExpeditionGenerationSettings settings)
    {
        var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color") ?? throw new InvalidOperationException("No compatible seam-marker shader is available.");
        var vertical = CreateLine(parent, shader, "AE East-West Shared Seam", new Color(1f, 0.82f, 0.08f), Subdivisions * 2 + 1);
        var horizontal = CreateLine(parent, shader, "AE North-South Shared Seam", new Color(0.2f, 1f, 0.95f), Subdivisions * 2 + 1);
        for (var sample = 0; sample <= Subdivisions * 2; sample++)
        {
            var coordinateIndex = sample == Subdivisions * 2 ? 1 : sample / Subdivisions;
            var localSample = sample == Subdivisions * 2 ? Subdivisions : sample % Subdivisions;
            var verticalCoordinate = new ExpeditionChunkCoordinate(0, coordinateIndex);
            var verticalHeight = generator.SampleTerrainHeight(worldSeed, verticalCoordinate, Subdivisions, localSample, Subdivisions, settings) - settings.BaseDepth;
            vertical.SetPosition(sample, Anchor + new Vector3(settings.ChunkSize * 0.5f, verticalHeight + 1.2f, (sample / (float)Subdivisions - 0.5f) * settings.ChunkSize));
            var horizontalCoordinate = new ExpeditionChunkCoordinate(coordinateIndex, 0);
            var horizontalHeight = generator.SampleTerrainHeight(worldSeed, horizontalCoordinate, localSample, Subdivisions, Subdivisions, settings) - settings.BaseDepth;
            horizontal.SetPosition(sample, Anchor + new Vector3((sample / (float)Subdivisions - 0.5f) * settings.ChunkSize, horizontalHeight + 1.2f, settings.ChunkSize * 0.5f));
        }
    }

    private static LineRenderer CreateLine(Transform parent, Shader shader, string name, Color color, int positions)
    {
        var lineObject = new GameObject(name);
        lineObject.transform.SetParent(parent, true);
        var line = lineObject.AddComponent<LineRenderer>();
        var material = new Material(shader) { name = $"{name} Material", color = color };
        Materials.Add(material);
        line.sharedMaterial = material;
        line.useWorldSpace = true;
        line.positionCount = positions;
        line.startWidth = 1.4f;
        line.endWidth = 1.4f;
        line.startColor = color;
        line.endColor = color;
        line.numCapVertices = 2;
        return line;
    }

    private static float MeasureMaximumSeamGap(ExpeditionGenerator generator, int worldSeed, ExpeditionGenerationSettings settings)
    {
        var maximum = 0f;
        for (var sample = 0; sample <= Subdivisions; sample++)
        {
            maximum = Mathf.Max(maximum, Mathf.Abs(generator.SampleTerrainHeight(worldSeed, Coordinates[0], Subdivisions, sample, Subdivisions, settings) - generator.SampleTerrainHeight(worldSeed, Coordinates[1], 0, sample, Subdivisions, settings)));
            maximum = Mathf.Max(maximum, Mathf.Abs(generator.SampleTerrainHeight(worldSeed, Coordinates[2], Subdivisions, sample, Subdivisions, settings) - generator.SampleTerrainHeight(worldSeed, Coordinates[3], 0, sample, Subdivisions, settings)));
            maximum = Mathf.Max(maximum, Mathf.Abs(generator.SampleTerrainHeight(worldSeed, Coordinates[0], sample, Subdivisions, Subdivisions, settings) - generator.SampleTerrainHeight(worldSeed, Coordinates[2], sample, 0, Subdivisions, settings)));
            maximum = Mathf.Max(maximum, Mathf.Abs(generator.SampleTerrainHeight(worldSeed, Coordinates[1], sample, Subdivisions, Subdivisions, settings) - generator.SampleTerrainHeight(worldSeed, Coordinates[3], sample, 0, Subdivisions, settings)));
        }
        return maximum;
    }

    private static Mesh BuildMesh(ExpeditionGenerator generator, int worldSeed, ExpeditionChunkCoordinate coordinate, ExpeditionGenerationSettings settings)
    {
        var axis = Subdivisions + 1;
        var vertices = new Vector3[axis * axis];
        var uv = new Vector2[vertices.Length];
        for (var z = 0; z < axis; z++)
        for (var x = 0; x < axis; x++)
        {
            var index = z * axis + x;
            var u = x / (float)Subdivisions;
            var v = z / (float)Subdivisions;
            var height = generator.SampleTerrainHeight(worldSeed, coordinate, x, z, Subdivisions, settings);
            vertices[index] = new Vector3((u - 0.5f) * settings.ChunkSize, height - settings.BaseDepth, (v - 0.5f) * settings.ChunkSize);
            uv[index] = new Vector2(u * 8f, v * 8f);
        }

        var triangles = new int[Subdivisions * Subdivisions * 6];
        var cursor = 0;
        for (var z = 0; z < Subdivisions; z++)
        for (var x = 0; x < Subdivisions; x++)
        {
            var northWest = z * axis + x;
            var northEast = northWest + 1;
            var southWest = northWest + axis;
            var southEast = southWest + 1;
            triangles[cursor++] = northWest; triangles[cursor++] = southWest; triangles[cursor++] = northEast;
            triangles[cursor++] = northEast; triangles[cursor++] = southWest; triangles[cursor++] = southEast;
        }

        var mesh = new Mesh { name = $"AE Diagnostic Expedition Seabed {coordinate.X},{coordinate.Z}", vertices = vertices, triangles = triangles, uv = uv };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Vector3 GridCenter() => Anchor + new Vector3(128f, 0f, 128f);
    private static int TotalVertices() => Meshes.Count * (Subdivisions + 1) * (Subdivisions + 1);
    private static int TotalTriangles() => Meshes.Count * Subdivisions * Subdivisions * 2;
    private static int ReadyColliderCount()
    {
        if (_root == null) return 0;
        var ready = 0;
        foreach (var collider in _root.GetComponentsInChildren<MeshCollider>()) if (collider.sharedMesh != null) ready++;
        return ready;
    }
    private static long EstimatedMeshBytes() => Meshes.Count * ((long)(Subdivisions + 1) * (Subdivisions + 1) * (sizeof(float) * 8) + (long)Subdivisions * Subdivisions * 6 * sizeof(int));

    private static void CleanupRuntimeObjects()
    {
        if (_root != null) UnityEngine.Object.Destroy(_root);
        foreach (var mesh in Meshes) if (mesh != null) UnityEngine.Object.Destroy(mesh);
        foreach (var material in Materials) if (material != null) UnityEngine.Object.Destroy(material);
        Meshes.Clear();
        Materials.Clear();
        _root = null;
        _managedDelta = 0;
        _maximumSeamGap = 0f;
    }
}
