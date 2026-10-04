using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AbyssalEcologies.Core;
using UnityEngine;

namespace AbyssalEcologies.Plugin;

internal static class ExpeditionChunkPrototype
{
    public const string ConfirmationPhrase = "CONFIRM_EXPEDITION_BIOME_GRAMMAR";
    public const string GotoName = "aechunk";
    public const string EastGotoName = "aechunk_e";
    public const string WestGotoName = "aechunk_w";
    public const string NorthGotoName = "aechunk_n";
    public const string SouthGotoName = "aechunk_s";
    public static readonly Vector3 Anchor = new(0f, -700f, 0f);
    public static readonly Vector3 Arrival = new(0f, -662f, 0f);
    public static readonly Vector3 EastArrival = new(512f, -642f, 0f);
    public static readonly Vector3 WestArrival = new(-512f, -642f, 0f);
    public static readonly Vector3 NorthArrival = new(0f, -642f, 512f);
    public static readonly Vector3 SouthArrival = new(0f, -642f, -512f);

    private const int Subdivisions = 24;
    private const int StreamingRadius = 1;
    private const int PoolSize = 9;
    private const int FingerprintCacheLimit = 64;
    private const float StagingVerticalRange = 320f;
    private static readonly ExpeditionGenerator Generator = new();
    private static readonly ExpeditionGenerationSettings Settings = new();
    private static readonly Dictionary<ExpeditionChunkCoordinate, ChunkSlot> Active = new();
    private static readonly Dictionary<ExpeditionChunkCoordinate, ulong> GeometryFingerprints = new();
    private static readonly Queue<ExpeditionChunkCoordinate> FingerprintOrder = new();
    private static readonly List<ChunkSlot> Pool = new();
    private static readonly List<Material> Materials = new();
    private static readonly List<LineRenderer> SeamLines = new();
    private static GameObject? _root;
    private static ExpeditionChunkCoordinate _center;
    private static long _managedMemoryBefore;
    private static long _managedDelta;
    private static int _worldSeed;
    private static int _assignmentCount;
    private static int _reusedAssignmentCount;
    private static int _retiredCount;
    private static int _windowUpdateCount;
    private static int _determinismMismatchCount;
    private static int _lastRetainedCount;
    private static int _lastAddedCount;
    private static int _lastRemovedCount;
    private static float _maximumSeamGap;
    private static double _lastUpdateMilliseconds;
    private static double _maximumUpdateMilliseconds;
    private static bool _faulted;
    private static string _lastFault = string.Empty;

    public static bool IsActive => _root != null;

    public static string Create(int worldSeed, string confirmation)
    {
        if (confirmation != ConfirmationPhrase)
            return $"AE expedition streaming refused. Usage: ae_chunk_create {ConfirmationPhrase}";
        if (_root != null)
            return "AE expedition streaming refused: diagnostic terrain is already active. Run ae_chunk_status or ae_chunk_remove.";

        _managedMemoryBefore = GC.GetTotalMemory(false);
        _worldSeed = worldSeed;
        _center = new ExpeditionChunkCoordinate(0, 0);
        _root = new GameObject("Abyssal Ecologies Diagnostic 3x3 Streaming Window");
        _root.transform.position = Anchor;
        try
        {
            CreatePool(_root.transform);
            CreateSeamMarkers(_root.transform);
            ApplyWindow(_center, initial: true);
            _root.AddComponent<ExpeditionStreamingController>();
            _managedDelta = GC.GetTotalMemory(false) - _managedMemoryBefore;
        }
        catch (Exception exception)
        {
            CleanupRuntimeObjects();
            var failure = $"AE expedition streaming creation FAILED and was rolled back: {exception.Message}";
            Plugin.Log.LogError($"{failure} {exception}");
            return failure;
        }

        var result = $"AE expedition streaming CREATED: seed={worldSeed}, center={_center}, active={Active.Count}/{PoolSize}, coordinates={ActiveCoordinateText()}, biomes={ActiveBiomeText()}, transitionChunks={TransitionChunkCount()}/{PoolSize}, vertices={TotalVertices()}, triangles={TotalTriangles()}, colliders={ReadyColliderCount()}/{PoolSize}, maxSeamGap={_maximumSeamGap:0.000000}m, poolSlots={Pool.Count}, assignments={_assignmentCount}, reused={_reusedAssignmentCount}, retired={_retiredCount}, lastTransition={_lastRetainedCount} retained/{_lastAddedCount} added/{_lastRemovedCount} removed, fingerprints={GeometryFingerprints.Count}/{FingerprintCacheLimit}, seamMarkers={SeamLines.Count}, estimatedMesh={EstimatedMeshBytes() / 1024d:0.0} KiB, managedDelta={_managedDelta / 1024d:+0.0;-0.0;0.0} KiB, initialUpdate={_lastUpdateMilliseconds:0.0}ms. Run 'goto {GotoName}', travel across chunk boundaries, and inspect ae_chunk_status.";
        Plugin.Log.LogWarning(result);
        return result;
    }

    public static string Status()
    {
        Tick();
        if (_root == null)
            return "AE expedition streaming: inactive; no diagnostic runtime terrain exists.";

        var playerCoordinate = Player.main == null ? "unavailable" : CoordinateForPosition(Player.main.transform.position).ToString();
        var state = _faulted ? $"FAULTED({_lastFault})" : "ACTIVE";
        return $"AE expedition streaming {state}: seed={_worldSeed}, center={_center}, playerChunk={playerCoordinate}, active={Active.Count}/{PoolSize}, coordinates={ActiveCoordinateText()}, biomes={ActiveBiomeText()}, transitionChunks={TransitionChunkCount()}/{PoolSize}, vertices={TotalVertices()}, triangles={TotalTriangles()}, colliders={ReadyColliderCount()}/{PoolSize}, maxSeamGap={_maximumSeamGap:0.000000}m, poolSlots={Pool.Count}, assignments={_assignmentCount}, reused={_reusedAssignmentCount}, retired={_retiredCount}, windowUpdates={_windowUpdateCount}, lastTransition={_lastRetainedCount} retained/{_lastAddedCount} added/{_lastRemovedCount} removed, determinismMismatches={_determinismMismatchCount}, fingerprints={GeometryFingerprints.Count}/{FingerprintCacheLimit}, seamMarkers={SeamLines.Count}, estimatedMesh={EstimatedMeshBytes() / 1024d:0.0} KiB, managedDelta={_managedDelta / 1024d:+0.0;-0.0;0.0} KiB, lastUpdate={_lastUpdateMilliseconds:0.0}ms, maxUpdate={_maximumUpdateMilliseconds:0.0}ms.";
    }

    public static string Remove()
    {
        if (_root == null)
            return "AE expedition streaming was not active.";
        if (Player.main != null && Mathf.Abs(Player.main.transform.position.y - Anchor.y) < StagingVerticalRange)
            return "AE expedition streaming removal REFUSED: player remains at expedition depth. Run 'warp 0 0 0' first, then retry ae_chunk_remove.";

        var slots = Pool.Count;
        var retired = _retiredCount;
        var reused = _reusedAssignmentCount;
        CleanupRuntimeObjects();
        var result = $"AE expedition streaming REMOVED atomically: {slots} pooled chunk roots, colliders, meshes, materials, lights, and seam markers were scheduled for destruction. Session totals: reused={reused}, retired={retired}. No manifest or save data was written.";
        Plugin.Log.LogWarning(result);
        return result;
    }

    internal static void Tick()
    {
        if (_root == null || _faulted || Player.main == null) return;
        var playerPosition = Player.main.transform.position;
        if (Mathf.Abs(playerPosition.y - Anchor.y) >= StagingVerticalRange) return;
        var coordinate = CoordinateForPosition(playerPosition);
        if (coordinate.Equals(_center)) return;

        try
        {
            ApplyWindow(coordinate, initial: false);
            var result = $"AE expedition streaming WINDOW: center={_center}, active={Active.Count}/{PoolSize}, coordinates={ActiveCoordinateText()}, biomes={ActiveBiomeText()}, transitionChunks={TransitionChunkCount()}/{PoolSize}, colliders={ReadyColliderCount()}/{PoolSize}, maxSeamGap={_maximumSeamGap:0.000000}m, assignments={_assignmentCount}, reused={_reusedAssignmentCount}, retired={_retiredCount}, windowUpdates={_windowUpdateCount}, lastTransition={_lastRetainedCount} retained/{_lastAddedCount} added/{_lastRemovedCount} removed, determinismMismatches={_determinismMismatchCount}, update={_lastUpdateMilliseconds:0.0}ms.";
            Plugin.Log.LogInfo(result);
        }
        catch (Exception exception)
        {
            _faulted = true;
            _lastFault = exception.Message;
            Plugin.Log.LogError($"AE expedition streaming update FAILED and has been suspended: {exception}");
        }
    }

    private static void CreatePool(Transform parent)
    {
        var shader = Shader.Find("Unlit/Color") ?? Shader.Find("MarmosetUBER") ?? Shader.Find("Standard") ?? throw new InvalidOperationException("No compatible terrain shader is available.");
        for (var index = 0; index < PoolSize; index++)
        {
            var chunk = new GameObject($"AE Pooled Expedition Chunk {index + 1}");
            chunk.transform.SetParent(parent, false);
            var mesh = new Mesh { name = $"AE Pooled Expedition Mesh {index + 1}" };
            var filter = chunk.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = chunk.AddComponent<MeshRenderer>();
            var material = new Material(shader) { name = $"AE Pooled Expedition Material {index + 1}" };
            renderer.sharedMaterial = material;
            var collider = chunk.AddComponent<MeshCollider>();

            var lightObject = new GameObject($"AE Temporary Inspection Light {index + 1}");
            lightObject.transform.SetParent(chunk.transform, false);
            lightObject.transform.localPosition = new Vector3(0f, 75f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 215f;
            light.intensity = 1.35f;
            light.color = new Color(0.62f, 0.9f, 1f);
            light.shadows = LightShadows.None;

            var slot = new ChunkSlot(chunk, mesh, renderer, collider, material);
            Pool.Add(slot);
            Materials.Add(material);
            chunk.SetActive(false);
        }
    }

    private static void CreateSeamMarkers(Transform parent)
    {
        var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color") ?? throw new InvalidOperationException("No compatible seam-marker shader is available.");
        SeamLines.Add(CreateLine(parent, shader, "AE West Streaming Seam", new Color(1f, 0.82f, 0.08f)));
        SeamLines.Add(CreateLine(parent, shader, "AE East Streaming Seam", new Color(1f, 0.82f, 0.08f)));
        SeamLines.Add(CreateLine(parent, shader, "AE North Streaming Seam", new Color(0.2f, 1f, 0.95f)));
        SeamLines.Add(CreateLine(parent, shader, "AE South Streaming Seam", new Color(0.2f, 1f, 0.95f)));
    }

    private static LineRenderer CreateLine(Transform parent, Shader shader, string name, Color color)
    {
        var lineObject = new GameObject(name);
        lineObject.transform.SetParent(parent, false);
        var line = lineObject.AddComponent<LineRenderer>();
        var material = new Material(shader) { name = $"{name} Material", color = color };
        Materials.Add(material);
        line.sharedMaterial = material;
        line.useWorldSpace = true;
        line.positionCount = Subdivisions * 3 + 1;
        line.startWidth = 1.4f;
        line.endWidth = 1.4f;
        line.startColor = color;
        line.endColor = color;
        line.numCapVertices = 2;
        return line;
    }

    private static void ApplyWindow(ExpeditionChunkCoordinate center, bool initial)
    {
        var stopwatch = Stopwatch.StartNew();
        var transition = Generator.PlanStreamingTransition(_worldSeed, Active.Keys, center, StreamingRadius, Settings);
        _lastRetainedCount = transition.Retained.Count;
        _lastAddedCount = transition.Added.Count;
        _lastRemovedCount = transition.Removed.Count;
        var available = new Queue<ChunkSlot>();
        foreach (var coordinate in transition.Removed)
        {
            if (!Active.TryGetValue(coordinate, out var slot))
                throw new InvalidOperationException($"Transition attempted to retire missing {coordinate}.");
            Active.Remove(coordinate);
            slot.Root.SetActive(false);
            available.Enqueue(slot);
            _retiredCount++;
        }

        foreach (var descriptor in transition.Added)
        {
            ChunkSlot slot;
            if (available.Count > 0)
                slot = available.Dequeue();
            else
                slot = Pool.FirstOrDefault(item => !item.Assigned) ?? throw new InvalidOperationException("The nine-slot terrain pool was exhausted.");
            AssignSlot(slot, descriptor);
            Active.Add(descriptor.Coordinate, slot);
        }

        if (available.Count != 0)
            throw new InvalidOperationException("Transition retired more pool slots than it added.");
        if (Active.Count != PoolSize || Active.Values.Distinct().Count() != PoolSize)
            throw new InvalidOperationException($"Streaming bound failed: active={Active.Count}, uniqueSlots={Active.Values.Distinct().Count()}, expected={PoolSize}.");
        var readyColliders = ReadyColliderCount();
        if (readyColliders != PoolSize)
            throw new InvalidOperationException($"Collider readiness failed: ready={readyColliders}, expected={PoolSize}.");

        _center = center;
        UpdateSeamMarkers(center);
        _maximumSeamGap = MeasureMaximumSeamGap();
        if (!initial) _windowUpdateCount++;
        stopwatch.Stop();
        _lastUpdateMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
        _maximumUpdateMilliseconds = Math.Max(_maximumUpdateMilliseconds, _lastUpdateMilliseconds);
    }

    private static void AssignSlot(ChunkSlot slot, ExpeditionChunkDescriptor descriptor)
    {
        var wasAssigned = slot.Assigned;
        slot.Root.SetActive(false);
        slot.Collider.enabled = false;
        slot.Collider.sharedMesh = null;
        var fingerprint = BuildMesh(slot.Mesh, descriptor.Coordinate);
        if (GeometryFingerprints.TryGetValue(descriptor.Coordinate, out var priorFingerprint))
        {
            if (fingerprint != priorFingerprint) _determinismMismatchCount++;
        }
        else
        {
            GeometryFingerprints.Add(descriptor.Coordinate, fingerprint);
            FingerprintOrder.Enqueue(descriptor.Coordinate);
            while (FingerprintOrder.Count > FingerprintCacheLimit)
                GeometryFingerprints.Remove(FingerprintOrder.Dequeue());
        }

        ApplyTerrainMaterial(slot.Material, descriptor);
        slot.Root.name = $"AE Pooled Chunk {descriptor.Coordinate.X},{descriptor.Coordinate.Z} [{descriptor.Biome}; {descriptor.TransitionEdges}]";
        slot.Root.transform.localPosition = new Vector3(descriptor.Coordinate.X * Settings.ChunkSize, 0f, descriptor.Coordinate.Z * Settings.ChunkSize);
        slot.Coordinate = descriptor.Coordinate;
        slot.Biome = descriptor.Biome;
        slot.TransitionEdges = descriptor.TransitionEdges;
        slot.Assigned = true;
        slot.Root.SetActive(true);
        slot.Collider.enabled = true;
        slot.Collider.sharedMesh = slot.Mesh;
        if (slot.Collider.sharedMesh == null)
            throw new InvalidOperationException($"Unity rejected the collider mesh for {descriptor.Coordinate}.");
        _assignmentCount++;
        if (wasAssigned) _reusedAssignmentCount++;
    }

    private static ulong BuildMesh(Mesh mesh, ExpeditionChunkCoordinate coordinate)
    {
        var axis = Subdivisions + 1;
        var vertices = new Vector3[axis * axis];
        var uv = new Vector2[vertices.Length];
        ulong fingerprint = 1469598103934665603UL;
        for (var z = 0; z < axis; z++)
        for (var x = 0; x < axis; x++)
        {
            var index = z * axis + x;
            var u = x / (float)Subdivisions;
            var v = z / (float)Subdivisions;
            var height = Generator.SampleTerrainHeight(_worldSeed, coordinate, x, z, Subdivisions, Settings);
            vertices[index] = new Vector3((u - 0.5f) * Settings.ChunkSize, height - Settings.BaseDepth, (v - 0.5f) * Settings.ChunkSize);
            uv[index] = new Vector2(u * 8f, v * 8f);
            fingerprint = MixFingerprint(fingerprint, unchecked((uint)height.GetHashCode()));
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

        mesh.Clear();
        mesh.name = $"AE Expedition Seabed {coordinate.X},{coordinate.Z}";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return fingerprint;
    }

    private static ulong MixFingerprint(ulong hash, uint value)
    {
        unchecked
        {
            hash ^= value;
            return hash * 1099511628211UL;
        }
    }

    private static void ApplyTerrainMaterial(Material material, ExpeditionChunkDescriptor descriptor)
    {
        var color = BiomeColor(descriptor.Biome);
        var glow = BiomeGlow(descriptor.Biome);
        if (descriptor.IsTransition)
        {
            var neighborColors = new List<Color>(4);
            if ((descriptor.TransitionEdges & ExpeditionTransitionEdges.West) != 0) neighborColors.Add(BiomeColor(descriptor.WestBiome));
            if ((descriptor.TransitionEdges & ExpeditionTransitionEdges.East) != 0) neighborColors.Add(BiomeColor(descriptor.EastBiome));
            if ((descriptor.TransitionEdges & ExpeditionTransitionEdges.North) != 0) neighborColors.Add(BiomeColor(descriptor.NorthBiome));
            if ((descriptor.TransitionEdges & ExpeditionTransitionEdges.South) != 0) neighborColors.Add(BiomeColor(descriptor.SouthBiome));
            var neighbor = neighborColors.Aggregate(Color.black, (sum, item) => sum + item) / neighborColors.Count;
            color = Color.Lerp(color, neighbor, 0.38f);
            glow = Color.Lerp(glow, Color.white, 0.22f);
        }
        material.name = $"AE Expedition Material {descriptor.Coordinate.X},{descriptor.Coordinate.Z} [{descriptor.Biome}; {descriptor.TransitionEdges}]";
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_GlowColor")) material.SetColor("_GlowColor", glow);
        if (material.HasProperty("_GlowStrength")) material.SetFloat("_GlowStrength", descriptor.IsTransition ? 0.7f : 0.35f);
    }

    private static Color BiomeColor(ExpeditionBiome biome) => biome switch
    {
        ExpeditionBiome.EmberTrench => new Color(0.88f, 0.16f, 0.035f, 1f),
        ExpeditionBiome.GlassKelpGarden => new Color(0.025f, 0.82f, 0.61f, 1f),
        _ => new Color(0.07f, 0.3f, 1f, 1f)
    };

    private static Color BiomeGlow(ExpeditionBiome biome) => biome switch
    {
        ExpeditionBiome.EmberTrench => new Color(0.32f, 0.055f, 0.01f, 1f),
        ExpeditionBiome.GlassKelpGarden => new Color(0.015f, 0.2f, 0.18f, 1f),
        _ => new Color(0.035f, 0.12f, 0.32f, 1f)
    };

    private static void UpdateSeamMarkers(ExpeditionChunkCoordinate center)
    {
        UpdateVerticalSeam(SeamLines[0], center.X - 1, center.Z);
        UpdateVerticalSeam(SeamLines[1], center.X, center.Z);
        UpdateHorizontalSeam(SeamLines[2], center.Z - 1, center.X);
        UpdateHorizontalSeam(SeamLines[3], center.Z, center.X);
    }

    private static void UpdateVerticalSeam(LineRenderer line, int leftChunkX, int centerZ)
    {
        for (var sample = 0; sample <= Subdivisions * 3; sample++)
        {
            var chunkOffset = sample == Subdivisions * 3 ? 2 : sample / Subdivisions;
            var localSample = sample == Subdivisions * 3 ? Subdivisions : sample % Subdivisions;
            var coordinate = new ExpeditionChunkCoordinate(leftChunkX, centerZ - 1 + chunkOffset);
            var height = Generator.SampleTerrainHeight(_worldSeed, coordinate, Subdivisions, localSample, Subdivisions, Settings) - Settings.BaseDepth;
            var x = (leftChunkX + 0.5f) * Settings.ChunkSize;
            var z = (centerZ - 1.5f + sample / (float)Subdivisions) * Settings.ChunkSize;
            line.SetPosition(sample, Anchor + new Vector3(x, height + 1.2f, z));
        }
    }

    private static void UpdateHorizontalSeam(LineRenderer line, int northChunkZ, int centerX)
    {
        for (var sample = 0; sample <= Subdivisions * 3; sample++)
        {
            var chunkOffset = sample == Subdivisions * 3 ? 2 : sample / Subdivisions;
            var localSample = sample == Subdivisions * 3 ? Subdivisions : sample % Subdivisions;
            var coordinate = new ExpeditionChunkCoordinate(centerX - 1 + chunkOffset, northChunkZ);
            var height = Generator.SampleTerrainHeight(_worldSeed, coordinate, localSample, Subdivisions, Subdivisions, Settings) - Settings.BaseDepth;
            var x = (centerX - 1.5f + sample / (float)Subdivisions) * Settings.ChunkSize;
            var z = (northChunkZ + 0.5f) * Settings.ChunkSize;
            line.SetPosition(sample, Anchor + new Vector3(x, height + 1.2f, z));
        }
    }

    private static float MeasureMaximumSeamGap()
    {
        var maximum = 0f;
        foreach (var coordinate in Active.Keys)
        {
            var east = new ExpeditionChunkCoordinate(coordinate.X + 1, coordinate.Z);
            if (Active.ContainsKey(east))
            {
                for (var sample = 0; sample <= Subdivisions; sample++)
                    maximum = Mathf.Max(maximum, Mathf.Abs(Generator.SampleTerrainHeight(_worldSeed, coordinate, Subdivisions, sample, Subdivisions, Settings) - Generator.SampleTerrainHeight(_worldSeed, east, 0, sample, Subdivisions, Settings)));
            }

            var south = new ExpeditionChunkCoordinate(coordinate.X, coordinate.Z + 1);
            if (Active.ContainsKey(south))
            {
                for (var sample = 0; sample <= Subdivisions; sample++)
                    maximum = Mathf.Max(maximum, Mathf.Abs(Generator.SampleTerrainHeight(_worldSeed, coordinate, sample, Subdivisions, Subdivisions, Settings) - Generator.SampleTerrainHeight(_worldSeed, south, sample, 0, Subdivisions, Settings)));
            }
        }
        return maximum;
    }

    private static ExpeditionChunkCoordinate CoordinateForPosition(Vector3 position)
    {
        var relativeX = position.x - Anchor.x;
        var relativeZ = position.z - Anchor.z;
        return new ExpeditionChunkCoordinate(
            Mathf.FloorToInt((relativeX + Settings.ChunkSize * 0.5f) / Settings.ChunkSize),
            Mathf.FloorToInt((relativeZ + Settings.ChunkSize * 0.5f) / Settings.ChunkSize));
    }

    private static string ActiveCoordinateText() => string.Join(
        "/",
        Active.Keys.OrderBy(coordinate => coordinate.Z).ThenBy(coordinate => coordinate.X).Select(coordinate => $"({coordinate.X},{coordinate.Z})"));

    private static string ActiveBiomeText() => string.Join(
        "/",
        Active.Values
            .OrderBy(slot => slot.Coordinate.Z)
            .ThenBy(slot => slot.Coordinate.X)
            .Select(slot => $"({slot.Coordinate.X},{slot.Coordinate.Z})={slot.Biome}[{slot.TransitionEdges}]"));

    private static int TransitionChunkCount() => Active.Values.Count(slot => slot.TransitionEdges != ExpeditionTransitionEdges.None);

    private static int TotalVertices() => Active.Count * (Subdivisions + 1) * (Subdivisions + 1);
    private static int TotalTriangles() => Active.Count * Subdivisions * Subdivisions * 2;
    private static int ReadyColliderCount() => Active.Values.Count(slot => slot.Collider.sharedMesh != null && slot.Root.activeSelf);
    private static long EstimatedMeshBytes() => Active.Count * ((long)(Subdivisions + 1) * (Subdivisions + 1) * (sizeof(float) * 8) + (long)Subdivisions * Subdivisions * 6 * sizeof(int));

    private static void CleanupRuntimeObjects()
    {
        if (_root != null) UnityEngine.Object.Destroy(_root);
        foreach (var slot in Pool) if (slot.Mesh != null) UnityEngine.Object.Destroy(slot.Mesh);
        foreach (var material in Materials) if (material != null) UnityEngine.Object.Destroy(material);
        Active.Clear();
        GeometryFingerprints.Clear();
        FingerprintOrder.Clear();
        Pool.Clear();
        Materials.Clear();
        SeamLines.Clear();
        _root = null;
        _managedMemoryBefore = 0;
        _managedDelta = 0;
        _assignmentCount = 0;
        _reusedAssignmentCount = 0;
        _retiredCount = 0;
        _windowUpdateCount = 0;
        _determinismMismatchCount = 0;
        _lastRetainedCount = 0;
        _lastAddedCount = 0;
        _lastRemovedCount = 0;
        _maximumSeamGap = 0f;
        _lastUpdateMilliseconds = 0d;
        _maximumUpdateMilliseconds = 0d;
        _faulted = false;
        _lastFault = string.Empty;
    }

    private sealed class ChunkSlot
    {
        public ChunkSlot(GameObject root, Mesh mesh, MeshRenderer renderer, MeshCollider collider, Material material)
        {
            Root = root;
            Mesh = mesh;
            Renderer = renderer;
            Collider = collider;
            Material = material;
        }

        public GameObject Root { get; }
        public Mesh Mesh { get; }
        public MeshRenderer Renderer { get; }
        public MeshCollider Collider { get; }
        public Material Material { get; }
        public ExpeditionChunkCoordinate Coordinate { get; set; }
        public ExpeditionBiome Biome { get; set; }
        public ExpeditionTransitionEdges TransitionEdges { get; set; }
        public bool Assigned { get; set; }
    }
}

internal sealed class ExpeditionStreamingController : MonoBehaviour
{
    private float _nextUpdate;

    private void Update()
    {
        if (Time.unscaledTime < _nextUpdate) return;
        _nextUpdate = Time.unscaledTime + 0.2f;
        ExpeditionChunkPrototype.Tick();
    }
}
