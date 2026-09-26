using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AbyssalEcologies.Core;
using Nautilus.Handlers;
using UnityEngine;

namespace AbyssalEcologies.Plugin;

internal sealed class TerrainPlacementResolver
{
    private const float ProbeStartY = 50f;
    private const float ProbeDistance = 1700f;
    private const float MaximumSearchRadius = 48f;
    private const float MaximumWorldRadius = 1450f;
    private const float MinimumWaterSurfaceY = -4f;

    private static readonly string[] ProtectedNameTokens =
    {
        "wreck",
        "aurora",
        "crashedship",
        "precursor",
        "alien_base",
        "alienbase",
        "lifepod",
        "escape pod",
        "escapepod",
        "degasi",
        "abandonedbase"
    };

    private readonly GeneratedWorld _candidateWorld;
    private readonly WaitScreenHandler.WaitScreenTask _task;
    private readonly List<Int3> _probeLoadedBatches = new();
    private readonly List<string> _rejections = new();
    private int _adjustedPlacements;

    public TerrainPlacementResolver(GeneratedWorld candidateWorld, WaitScreenHandler.WaitScreenTask task)
    {
        _candidateWorld = candidateWorld ?? throw new ArgumentNullException(nameof(candidateWorld));
        _task = task ?? throw new ArgumentNullException(nameof(task));
    }

    public GeneratedWorld? Result { get; private set; }
    public int AdjustedPlacements => _adjustedPlacements;
    public int RejectedPlacements => _rejections.Count;
    public IReadOnlyList<string> Rejections => _rejections;

    public IEnumerator Run()
    {
        var streamer = LargeWorldStreamer.main
            ?? throw new InvalidOperationException("LargeWorldStreamer is unavailable during terrain resolution.");
        var readyWaitFrames = 0;
        while (!streamer.IsReady())
        {
            if (++readyWaitFrames > 600)
                throw new InvalidOperationException("LargeWorldStreamer did not become ready within 600 frames.");
            _task.Status = "Waiting for world streaming to become ready";
            yield return null;
        }

        var initiallyLoaded = new HashSet<Int3>(streamer.LoadedBatches());
        var requiredBatches = CollectRequiredBatches(streamer);
        if (requiredBatches.Count > 96)
            throw new InvalidOperationException($"Terrain resolution requested {requiredBatches.Count} batches; the safety limit is 96.");
        var batchNumber = 0;

        try
        {
            foreach (var batch in requiredBatches)
            {
                batchNumber++;
                if (initiallyLoaded.Contains(batch) || !streamer.CheckBatch(batch))
                    continue;

                _task.Status = $"Streaming terrain probe batch {batchNumber}/{requiredBatches.Count}";
                // LargeWorldStreamer.LoadBatchAsync ends by synchronously pumping
                // FinalizeLoadBatchAsync. Runtime batch finalization yields async wait
                // objects, which that pump rejects and retries forever. Follow the
                // same tasked load/finalize sequence used by the game's streamer.
                var batchCells = streamer.cellManager.InitializeBatchCells(batch);
                yield return streamer.LoadBatchTaskedAsync(batchCells, false);
                yield return streamer.FinalizeLoadBatchAsync(batch, false);
                _probeLoadedBatches.Add(batch);
            }

            yield return null;
            _task.Status = "Snapping ecology placements to streamed terrain";
            Result = ResolveWorld();
        }
        finally
        {
            var releasedBatches = 0;
            for (var index = _probeLoadedBatches.Count - 1; index >= 0; index--)
            {
                if (streamer.TryUnloadBatch(_probeLoadedBatches[index]))
                    releasedBatches++;
            }

            Plugin.Log.LogInfo($"Released {releasedBatches}/{_probeLoadedBatches.Count} probe-only terrain batches.");
        }

        Plugin.Log.LogInfo($"Terrain resolution adjusted {_adjustedPlacements} placements and rejected {_rejections.Count}.");
        foreach (var rejection in _rejections.Take(12))
            Plugin.Log.LogWarning(rejection);
        if (_rejections.Count > 12)
            Plugin.Log.LogWarning($"{_rejections.Count - 12} additional placement rejections were omitted from the bounded log.");
    }

    private List<Int3> CollectRequiredBatches(LargeWorldStreamer streamer)
    {
        var result = new List<Int3>();
        var seen = new HashSet<Int3>();

        for (var regionIndex = 0; regionIndex < _candidateWorld.Regions.Count; regionIndex++)
        {
            var region = _candidateWorld.Regions[regionIndex];
            for (var placementIndex = 0; placementIndex < region.Placements.Count; placementIndex++)
            {
                var placement = region.Placements[placementIndex];
                var stableOffset = StableOffset(regionIndex, placementIndex);
                foreach (var sample in PlacementSearchPattern.Around(placement.Position, stableOffset, MaximumSearchRadius))
                {
                    var batch = streamer.GetContainingBatch(new Vector3(sample.X, sample.Y, sample.Z));
                    if (seen.Add(batch))
                        result.Add(batch);
                }
            }
        }

        return result;
    }

    private GeneratedWorld ResolveWorld()
    {
        var regions = new List<GeneratedRegion>(_candidateWorld.Regions.Count);

        for (var regionIndex = 0; regionIndex < _candidateWorld.Regions.Count; regionIndex++)
        {
            var region = _candidateWorld.Regions[regionIndex];
            var placements = new List<GeneratedPlacement>(region.Placements.Count);
            GeneratedPlacement? resolvedLandmark = null;

            for (var placementIndex = 0; placementIndex < region.Placements.Count; placementIndex++)
            {
                var placement = region.Placements[placementIndex];
                if (TryResolvePlacement(regionIndex, placementIndex, placement, out var resolved, out var reason))
                {
                    placements.Add(resolved);
                    if (resolved.Kind == PlacementKind.Landmark)
                        resolvedLandmark = resolved;
                }
                else
                {
                    var message = $"Rejected {region.ArchetypeId}/{placement.ContentId} at {placement.Position}: {reason}.";
                    if (placement.Kind == PlacementKind.Landmark)
                        throw new InvalidOperationException(message);
                    _rejections.Add(message);
                }
            }

            if (resolvedLandmark == null)
                throw new InvalidOperationException($"Region '{region.ArchetypeId}' has no terrain-resolved landmark.");

            regions.Add(new GeneratedRegion(
                region.ArchetypeId,
                region.DisplayName,
                resolvedLandmark.Position,
                region.Radius,
                placements));
        }

        return new GeneratedWorld(_candidateWorld.Seed, regions);
    }

    private bool TryResolvePlacement(
        int regionIndex,
        int placementIndex,
        GeneratedPlacement placement,
        out GeneratedPlacement resolved,
        out string reason)
    {
        var stableOffset = StableOffset(regionIndex, placementIndex);
        var lastReason = "no valid terrain sample";

        foreach (var sample in PlacementSearchPattern.Around(placement.Position, stableOffset, MaximumSearchRadius))
        {
            if (!TryResolveSample(sample, placement.Kind, stableOffset, out var position, out lastReason))
                continue;

            if (!position.Equals(placement.Position))
                _adjustedPlacements++;
            resolved = new GeneratedPlacement(placement.ContentId, placement.Kind, position, placement.EulerAngles, placement.Scale);
            reason = string.Empty;
            return true;
        }

        resolved = null!;
        reason = lastReason;
        return false;
    }

    private static bool TryResolveSample(
        WorldPoint sample,
        PlacementKind kind,
        int stableOffset,
        out WorldPoint position,
        out string reason)
    {
        position = default;
        var horizontalRadius = (sample.X * sample.X) + (sample.Z * sample.Z);
        if (horizontalRadius > MaximumWorldRadius * MaximumWorldRadius)
        {
            reason = "outside the supported map radius";
            return false;
        }

        if (WorldProtectionCatalog.TryFindExclusion(sample, kind == PlacementKind.Landmark ? 35f : 8f, out var areaId))
        {
            reason = $"inside protected area '{areaId}'";
            return false;
        }

        var origin = new Vector3(sample.X, ProbeStartY, sample.Z);
        if (!Physics.Raycast(origin, Vector3.down, out var hit, ProbeDistance, Voxeland.GetTerrainLayerMask(), QueryTriggerInteraction.Ignore))
        {
            reason = "no streamed seabed was found (void or map edge)";
            return false;
        }

        if (hit.point.y > MinimumWaterSurfaceY)
        {
            reason = "terrain surface is above water";
            return false;
        }

        if (IsProtectedBiome(hit.point, out var biome))
        {
            reason = $"protected biome '{biome}'";
            return false;
        }

        if (HasProtectedWorldObject(hit.point, kind == PlacementKind.Landmark ? 45f : 12f, out var protectedObject))
        {
            reason = $"near protected world object '{protectedObject}'";
            return false;
        }

        if (kind == PlacementKind.Creature)
            return TryResolveCreature(hit.point, sample, stableOffset, out position, out reason);

        var maximumSlope = kind == PlacementKind.Landmark ? 35f : 50f;
        var slope = Vector3.Angle(hit.normal, Vector3.up);
        if (slope > maximumSlope)
        {
            reason = $"seabed slope {slope:0.0} exceeds {maximumSlope:0.0} degrees";
            return false;
        }

        var surfaceOffset = kind == PlacementKind.Landmark ? 0.65f : 0.15f;
        var snapped = hit.point + (hit.normal * surfaceOffset);
        position = new WorldPoint(snapped.x, snapped.y, snapped.z);
        reason = string.Empty;
        return true;
    }

    private static bool TryResolveCreature(
        Vector3 seabed,
        WorldPoint sample,
        int stableOffset,
        out WorldPoint position,
        out string reason)
    {
        var clearance = 10f + (Math.Abs(stableOffset) % 17);
        for (var attempt = 0; attempt < 6; attempt++)
        {
            var targetY = Math.Min(-5f, seabed.y + clearance + (attempt * 4f));
            if (targetY - seabed.y < 5f)
                continue;

            var target = new Vector3(sample.X, targetY, sample.Z);
            if (Physics.CheckSphere(target, 2.5f, Voxeland.GetTerrainLayerMask(), QueryTriggerInteraction.Ignore))
                continue;
            if (HasProtectedWorldObject(target, 8f, out _))
                continue;

            position = new WorldPoint(target.x, target.y, target.z);
            reason = string.Empty;
            return true;
        }

        position = default;
        reason = "no water position had five metres of terrain clearance";
        return false;
    }

    internal static bool IsProtectedBiome(Vector3 position, out string biome)
    {
        biome = LargeWorld.main == null ? string.Empty : LargeWorld.main.GetBiome(position) ?? string.Empty;
        var normalized = biome.ToLowerInvariant();
        return normalized.Contains("void") || normalized.Contains("crashedship") || normalized.Contains("aurora") || normalized.Contains("precursor");
    }

    internal static bool HasProtectedWorldObject(Vector3 position, float radius, out string protectedObject)
    {
        foreach (var collider in Physics.OverlapSphere(position, radius, ~0, QueryTriggerInteraction.Collide))
        {
            if (collider.GetComponentInParent<BaseRoot>() != null)
            {
                protectedObject = "player base";
                return true;
            }

            if (collider.GetComponentInParent<EscapePod>() != null)
            {
                protectedObject = "escape pod";
                return true;
            }

            var current = collider.transform;
            while (current != null)
            {
                if (ContainsProtectedToken(current.name))
                {
                    protectedObject = current.name;
                    return true;
                }

                var identifier = current.GetComponent<PrefabIdentifier>();
                if (identifier != null && ContainsProtectedToken(identifier.ClassId))
                {
                    protectedObject = identifier.ClassId;
                    return true;
                }

                current = current.parent;
            }
        }

        protectedObject = string.Empty;
        return false;
    }

    private static bool ContainsProtectedToken(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;
        var normalized = value.ToLowerInvariant();
        return ProtectedNameTokens.Any(normalized.Contains);
    }

    private int StableOffset(int regionIndex, int placementIndex) =>
        unchecked((_candidateWorld.Seed * 397) + (regionIndex * 101) + placementIndex);
}
