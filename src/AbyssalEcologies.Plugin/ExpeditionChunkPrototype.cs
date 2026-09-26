using System;
using AbyssalEcologies.Core;
using UnityEngine;

namespace AbyssalEcologies.Plugin;

internal static class ExpeditionChunkPrototype
{
    public const string ConfirmationPhrase = "CONFIRM_EXPEDITION_CHUNK_SPIKE";
    public const string GotoName = "aechunk";
    public static readonly Vector3 Anchor = new(0f, -700f, 0f);
    public static readonly Vector3 Arrival = new(0f, -642f, 0f);
    private const int Subdivisions = 24;
    private static GameObject? _root;
    private static Mesh? _mesh;
    private static Material? _material;
    private static long _managedDelta;
    private static int _worldSeed;
    private static ExpeditionChunkCoordinate _coordinate;

    public static bool IsActive => _root != null;

    public static string Create(int worldSeed, string confirmation)
    {
        if (confirmation != ConfirmationPhrase)
            return $"AE expedition chunk refused. Usage: ae_chunk_create {ConfirmationPhrase}";
        if (_root != null)
            return "AE expedition chunk refused: the diagnostic chunk is already active. Run ae_chunk_status or ae_chunk_remove.";

        var before = GC.GetTotalMemory(false);
        _worldSeed = worldSeed;
        _coordinate = new ExpeditionChunkCoordinate(0, 0);
        var settings = new ExpeditionGenerationSettings();
        var generator = new ExpeditionGenerator();
        var descriptor = generator.GenerateChunk(worldSeed, _coordinate, settings);
        var root = new GameObject("Abyssal Ecologies Diagnostic Expedition Chunk");
        root.transform.position = Anchor;

        var mesh = BuildMesh(generator, worldSeed, _coordinate, settings);
        var filter = root.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        var renderer = root.AddComponent<MeshRenderer>();
        var shader = Shader.Find("MarmosetUBER") ?? Shader.Find("Standard");
        var material = new Material(shader) { name = "Abyssal Ecologies Diagnostic Seabed Material" };
        if (material.HasProperty("_Color")) material.SetColor("_Color", new Color(0.035f, 0.16f, 0.18f, 1f));
        if (material.HasProperty("_GlowColor")) material.SetColor("_GlowColor", new Color(0.015f, 0.12f, 0.14f, 1f));
        if (material.HasProperty("_GlowStrength")) material.SetFloat("_GlowStrength", 0.25f);
        renderer.sharedMaterial = material;
        var collider = root.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;

        _root = root;
        _mesh = mesh;
        _material = material;
        _managedDelta = GC.GetTotalMemory(false) - before;
        var result = $"AE expedition chunk CREATED: seed={worldSeed}, coordinate={_coordinate}, sector={descriptor.Sector}, biome={descriptor.Biome}, anchor={Anchor}, vertices={mesh.vertexCount}, triangles={mesh.triangles.Length / 3}, collider={(collider.sharedMesh != null ? "ready" : "missing")}, estimatedMesh={EstimateMeshBytes(mesh) / 1024d:0.0} KiB, managedDelta={_managedDelta / 1024d:+0.0;-0.0;0.0} KiB. Run 'goto {GotoName}', inspect/collide, then leave and run ae_chunk_remove.";
        Plugin.Log.LogWarning(result);
        return result;
    }

    public static string Status()
    {
        if (_root == null || _mesh == null)
            return "AE expedition chunk: inactive; no diagnostic runtime terrain exists.";
        var playerDistance = Player.main == null ? -1f : Vector3.Distance(Player.main.transform.position, Anchor);
        return $"AE expedition chunk ACTIVE: seed={_worldSeed}, coordinate={_coordinate}, anchor={Anchor}, vertices={_mesh.vertexCount}, triangles={_mesh.triangles.Length / 3}, collider={(_root.GetComponent<MeshCollider>()?.sharedMesh != null ? "ready" : "missing")}, estimatedMesh={EstimateMeshBytes(_mesh) / 1024d:0.0} KiB, managedDelta={_managedDelta / 1024d:+0.0;-0.0;0.0} KiB, playerDistance={playerDistance:0.0}m.";
    }

    public static string Remove()
    {
        if (_root == null)
            return "AE expedition chunk was not active.";
        if (Player.main != null && Vector3.Distance(Player.main.transform.position, Anchor) < 300f)
            return "AE expedition chunk removal REFUSED: player is within 300m of the test terrain. Run 'warp 0 0 0' first, then retry ae_chunk_remove.";

        UnityEngine.Object.Destroy(_root);
        if (_mesh != null) UnityEngine.Object.Destroy(_mesh);
        if (_material != null) UnityEngine.Object.Destroy(_material);
        _root = null;
        _mesh = null;
        _material = null;
        _managedDelta = 0;
        var result = "AE expedition chunk REMOVED: runtime root, collider, mesh, and material were scheduled for destruction. No manifest or save data was written.";
        Plugin.Log.LogWarning(result);
        return result;
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
            var absoluteHeight = generator.SampleTerrainHeight(worldSeed, coordinate, x, z, Subdivisions, settings);
            vertices[index] = new Vector3((u - 0.5f) * settings.ChunkSize, absoluteHeight - settings.BaseDepth, (v - 0.5f) * settings.ChunkSize);
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

        var mesh = new Mesh { name = "Abyssal Ecologies Diagnostic Expedition Seabed", vertices = vertices, triangles = triangles, uv = uv };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static long EstimateMeshBytes(Mesh mesh) =>
        (long)mesh.vertexCount * (sizeof(float) * 3 + sizeof(float) * 3 + sizeof(float) * 2) + (long)mesh.triangles.Length * sizeof(int);
}
