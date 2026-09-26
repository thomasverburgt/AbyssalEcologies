using System;
using System.Linq;
using System.Text;
using AbyssalEcologies.Core;
using Nautilus.Commands;
using Nautilus.Handlers;
using UnityEngine;

namespace AbyssalEcologies.Plugin;

internal static class DiagnosticCommands
{
    private static WorldManifest? _manifest;
    private static GeneratedWorld? _world;
    private static WorldPerformanceMeasurement? _performance;
    private static int _registeredContentTypeCount;
    private static Func<int, string, string>? _regenerationHandler;

    public static void Register()
    {
        ConsoleCommandsHandler.RegisterConsoleCommands(typeof(DiagnosticCommands));
        ConsoleCommandsHandler.AddGotoTeleportPosition(ExpeditionChunkPrototype.GotoName, ExpeditionChunkPrototype.Arrival);
    }

    public static void SetActive(WorldManifest manifest, GeneratedWorld world)
    {
        _manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
        _world = world ?? throw new ArgumentNullException(nameof(world));
    }

    public static void SetPerformance(WorldPerformanceMeasurement performance, int registeredContentTypeCount)
    {
        _performance = performance ?? throw new ArgumentNullException(nameof(performance));
        _registeredContentTypeCount = registeredContentTypeCount;
    }

    public static void SetRegenerationHandler(Func<int, string, string> handler) =>
        _regenerationHandler = handler ?? throw new ArgumentNullException(nameof(handler));

    [ConsoleCommand("ae_help")]
    public static string Help() =>
        $"Abyssal Ecologies diagnostics: ae_manifest; ae_bounds [region 1-12, or 0 for all]; ae_boundaries [10-300 seconds]; ae_boundaries_off; ae_validate; ae_perf; ae_fauna; ae_grounding; ae_probe REGION; ae_regenerate NEW_SEED CONFIRM_DISPOSABLE_SAVE_REGENERATION; ae_chunk_create {ExpeditionChunkPrototype.ConfirmationPhrase}; ae_chunk_status; ae_chunk_remove; goto ae1/ae2/ae3/{ExpeditionChunkPrototype.GotoName}.";

    [ConsoleCommand("ae_chunk_create")]
    public static string CreateExpeditionChunk(string confirmation = "")
    {
        if (_world == null)
            return "AE expedition chunk refused: load a disposable save first.";
        return ExpeditionChunkPrototype.Create(_world.Seed, confirmation);
    }

    [ConsoleCommand("ae_chunk_status")]
    public static string ExpeditionChunkStatus() => ExpeditionChunkPrototype.Status();

    [ConsoleCommand("ae_chunk_remove")]
    public static string RemoveExpeditionChunk() => ExpeditionChunkPrototype.Remove();

    [ConsoleCommand("ae_fauna")]
    public static string Fauna()
    {
        var result = ContentRegistrar.GetFaunaStatus();
        Plugin.Log.LogInfo(result);
        return result;
    }

    [ConsoleCommand("ae_manifest")]
    public static string Manifest()
    {
        if (_manifest == null || _world == null)
            return "Abyssal Ecologies: no save manifest is active. Load a save first.";

        var placementCount = _world.Regions.Sum(region => region.Placements.Count);
        var builder = new StringBuilder();
        builder.Append($"AE manifest: schema={_manifest.SchemaVersion}, sourceSchema={_manifest.SourceSchemaVersion}, migrated={_manifest.WasMigrated}, generator={_manifest.GeneratorVersion}, seed={_world.Seed}, placementMode={_manifest.PlacementMode}, exclusionCatalog={_manifest.ExclusionCatalogVersion}, terrainResolved={_manifest.TerrainResolved}, regions={_world.Regions.Count}, placements={placementCount}.");
        for (var index = 0; index < _world.Regions.Count; index++)
        {
            var region = _world.Regions[index];
            var landmark = region.Placements.FirstOrDefault(placement => placement.Kind == PlacementKind.Landmark)?.ContentId ?? "missing";
            builder.AppendLine();
            builder.Append($"{index + 1}: {region.DisplayName}; goto ae{index + 1}; center={region.Center}; radius={region.Radius:0.0}; placements={region.Placements.Count}; landmark={landmark}.");
        }

        var result = builder.ToString();
        Plugin.Log.LogInfo(result);
        return result;
    }

    [ConsoleCommand("ae_bounds")]
    public static string Bounds(int region = 0)
    {
        if (_world == null)
            return "Abyssal Ecologies: no save manifest is active. Load a save first.";
        if (region < 0 || region > _world.Regions.Count)
            return $"Abyssal Ecologies: region must be 0-{_world.Regions.Count}; 0 reports every region.";

        var first = region == 0 ? 0 : region - 1;
        var last = region == 0 ? _world.Regions.Count - 1 : first;
        var playerPosition = Player.main == null ? (Vector3?)null : Player.main.transform.position;
        var builder = new StringBuilder();

        for (var index = first; index <= last; index++)
        {
            var item = _world.Regions[index];
            if (builder.Length > 0) builder.AppendLine();
            builder.Append($"ae{index + 1} {item.DisplayName}: center={item.Center}, radius={item.Radius:0.0}, x=[{item.Center.X - item.Radius:0.0},{item.Center.X + item.Radius:0.0}], z=[{item.Center.Z - item.Radius:0.0},{item.Center.Z + item.Radius:0.0}]");
            if (playerPosition.HasValue)
            {
                var dx = playerPosition.Value.x - item.Center.X;
                var dz = playerPosition.Value.z - item.Center.Z;
                var distance = Mathf.Sqrt((dx * dx) + (dz * dz));
                builder.Append($", playerDistance={distance:0.0}, inside={(distance <= item.Radius ? "yes" : "no")}");
            }
            builder.Append('.');
        }

        var result = builder.ToString();
        Plugin.Log.LogInfo(result);
        return result;
    }

    [ConsoleCommand("ae_validate")]
    public static string Validate()
    {
        if (_manifest == null || _world == null)
            return "Abyssal Ecologies: no save manifest is active. Load a save first.";

        try
        {
            _manifest.Validate();
        }
        catch (Exception exception)
        {
            var manifestFailure = $"AE validation FAIL: manifest structure is invalid: {exception.Message}";
            Plugin.Log.LogError(manifestFailure);
            return manifestFailure;
        }

        var enforceCurrentExclusions = _manifest.ExclusionCatalogVersion >= WorldManifest.CurrentExclusionCatalogVersion;
        var report = WorldDiagnostics.Validate(_world, enforceCurrentExclusions);
        if (report.IsValid)
        {
            var legacyWarnings = report.Warnings.Count == 0
                ? "zero deterministic/static-exclusion errors"
                : $"zero errors and {report.Warnings.Count} preserved legacy exclusion warning(s)";
            var success = $"AE validation PASS: schema {_manifest.SchemaVersion} ({_manifest.PlacementMode}, exclusion catalog {_manifest.ExclusionCatalogVersion}), seed {_world.Seed}, {report.RegionCount} regions, {report.PlacementCount} placements, {legacyWarnings}.";
            Plugin.Log.LogInfo(success);
            foreach (var warning in report.Warnings.Take(8))
                Plugin.Log.LogWarning($"Preserved legacy layout: {warning}");
            if (report.Warnings.Count > 8)
                Plugin.Log.LogWarning($"{report.Warnings.Count - 8} additional preserved legacy exclusion warnings omitted.");
            return success;
        }

        var boundedErrors = string.Join(" | ", report.Errors.Take(8));
        var omitted = report.Errors.Count > 8 ? $" | {report.Errors.Count - 8} additional errors omitted" : string.Empty;
        var failure = $"AE validation FAIL: {report.Errors.Count} error(s): {boundedErrors}{omitted}.";
        Plugin.Log.LogError(failure);
        return failure;
    }

    [ConsoleCommand("ae_perf")]
    public static string Performance()
    {
        if (_performance == null)
            return "Abyssal Ecologies: performance data is unavailable until a save finishes loading.";

        var budget = PerformanceBudget.Evaluate(_performance);
        var runtime = ContentRegistrar.GetRuntimeMetrics();
        var status = budget.Passed ? "PASS" : "FAIL";
        var memoryMiB = _performance.ManagedMemoryDeltaBytes / (1024d * 1024d);
        var builder = new StringBuilder();
        builder.Append($"AE performance {status}: lateSetup={_performance.LateSetupMilliseconds:0.0}/{PerformanceBudget.MaximumLateSetupMilliseconds:0}ms, registration={_performance.RegistrationMilliseconds:0.0}/{PerformanceBudget.MaximumRegistrationMilliseconds:0}ms, managedDelta={memoryMiB:+0.00;-0.00;0.00}/{PerformanceBudget.MaximumManagedMemoryDeltaBytes / (1024 * 1024)}MiB, registered={runtime.RegisteredPlacementCount}/{_performance.ExpectedPlacementCount}, contentTypes={_registeredContentTypeCount}, instantiationCallbacks={runtime.InstantiationCallbacks}, activeCustomObjects={runtime.ActiveInstanceCount}.");

        if (runtime.InstantiationCounts.Count > 0)
        {
            builder.AppendLine();
            builder.Append("Callbacks by content: ");
            builder.Append(string.Join(", ", runtime.InstantiationCounts.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}={pair.Value}")));
            builder.Append('.');
        }

        if (!budget.Passed)
        {
            builder.AppendLine();
            builder.Append(string.Join(" | ", budget.Failures));
        }

        var result = builder.ToString();
        if (budget.Passed)
            Plugin.Log.LogInfo(result);
        else
            Plugin.Log.LogError(result);
        return result;
    }

    [ConsoleCommand("ae_grounding")]
    public static string Grounding()
    {
        if (_world == null)
            return "Abyssal Ecologies: no save manifest is active. Load a save first.";

        var metrics = ContentRegistrar.GetLandmarkGroundingMetrics();
        var builder = new StringBuilder();
        builder.Append($"AE landmark grounding: attempted={metrics.AttemptedCount}/3, grounded={metrics.GroundedCount}, failed={metrics.FailedCount}.");
        foreach (var pair in metrics.Results.OrderBy(pair => pair.Key))
        {
            builder.AppendLine();
            builder.Append($"{pair.Key}: {(pair.Value.Grounded ? "GROUNDED" : "FAILED")}, verticalAdjustment={pair.Value.VerticalAdjustment:+0.0;-0.0;0.0}m, {pair.Value.Detail}.");
        }

        var result = builder.ToString();
        if (metrics.FailedCount == 0)
            Plugin.Log.LogInfo(result);
        else
            Plugin.Log.LogWarning(result);
        return result;
    }

    [ConsoleCommand("ae_probe")]
    public static string Probe(int region = 0)
    {
        if (_world == null)
            return "Abyssal Ecologies: no save manifest is active. Load a save first.";
        if (region < 1 || region > _world.Regions.Count)
            return $"AE terrain probe refused: REGION must be 1-{_world.Regions.Count}.";

        var item = _world.Regions[region - 1];
        var landmark = item.Placements.FirstOrDefault(placement => placement.Kind == PlacementKind.Landmark);
        if (landmark == null)
            return $"AE terrain probe failed: region {region} has no landmark placement.";

        var x = landmark.Position.X;
        var y = landmark.Position.Y;
        var z = landmark.Position.Z;
        var localOrigin = new Vector3(x, y + 60f, z);
        var fullOrigin = new Vector3(x, 10f, z);
        var terrainMask = Voxeland.GetTerrainLayerMask();
        var localTerrainHits = Physics.RaycastAll(localOrigin, Vector3.down, 320f, terrainMask, QueryTriggerInteraction.Ignore);
        var localAllHits = Physics.RaycastAll(localOrigin, Vector3.down, 320f, ~0, QueryTriggerInteraction.Ignore);
        var fullTerrainHits = Physics.RaycastAll(fullOrigin, Vector3.down, 1800f, terrainMask, QueryTriggerInteraction.Ignore);
        var fullAllHits = Physics.RaycastAll(fullOrigin, Vector3.down, 1800f, ~0, QueryTriggerInteraction.Ignore);
        Array.Sort(fullAllHits, (left, right) => left.distance.CompareTo(right.distance));

        var builder = new StringBuilder();
        builder.Append($"AE terrain probe ae{region} {item.DisplayName}: landmark=({x:0.0},{y:0.0},{z:0.0}), terrainMask={terrainMask}, localTerrainHits={localTerrainHits.Length}, localAllHits={localAllHits.Length}, fullTerrainHits={fullTerrainHits.Length}, fullAllHits={fullAllHits.Length}.");
        foreach (var hit in fullAllHits.Take(12))
        {
            var collider = hit.collider;
            var layer = collider == null ? -1 : collider.gameObject.layer;
            var layerName = layer < 0 ? "missing" : LayerMask.LayerToName(layer);
            var terrainLayer = layer >= 0 && ((1 << layer) & terrainMask) != 0;
            var colliderName = collider == null ? "missing" : collider.name;
            var rootName = collider == null ? "missing" : collider.transform.root.name;
            var slope = Vector3.Angle(hit.normal, Vector3.up);
            builder.AppendLine();
            builder.Append($"hit y={hit.point.y:0.0}, distance={hit.distance:0.0}, slope={slope:0.0}, layer={layer}:{layerName}, terrainMask={(terrainLayer ? "yes" : "no")}, collider={colliderName}, root={rootName}.");
        }
        if (fullAllHits.Length > 12)
        {
            builder.AppendLine();
            builder.Append($"{fullAllHits.Length - 12} additional all-layer hits omitted.");
        }

        var result = builder.ToString();
        Plugin.Log.LogInfo(result);
        return result;
    }

    [ConsoleCommand("ae_boundaries")]
    public static string ShowBoundaries(int lifetimeSeconds = BoundaryVisualizer.DefaultLifetimeSeconds)
    {
        if (_world == null)
            return "Abyssal Ecologies: no save manifest is active. Load a save first.";

        try
        {
            var count = BoundaryVisualizer.Show(_world, lifetimeSeconds);
            var result = $"AE boundaries visible for {count} region(s) for {lifetimeSeconds} seconds. Bright rings mark horizontal radii; vertical lines mark centers. Use ae_boundaries_off to remove them early.";
            Plugin.Log.LogInfo(result);
            return result;
        }
        catch (Exception exception)
        {
            var failure = $"AE boundaries failed: {exception.Message}";
            Plugin.Log.LogError(failure);
            return failure;
        }
    }

    [ConsoleCommand("ae_boundaries_off")]
    public static string HideBoundaries()
    {
        var hidden = BoundaryVisualizer.Hide();
        var result = hidden ? "AE boundaries removed." : "AE boundaries were not active.";
        Plugin.Log.LogInfo(result);
        return result;
    }

    [ConsoleCommand("ae_regenerate")]
    public static string Regenerate(int seed = int.MinValue, string confirmation = "")
    {
        if (_manifest == null || _world == null)
            return "Abyssal Ecologies: no save manifest is active. Load a save first.";
        if (_regenerationHandler == null)
            return "Abyssal Ecologies: manifest regeneration is unavailable.";
        if (seed == int.MinValue || confirmation != ManifestRegeneration.ConfirmationPhrase)
            return $"AE regeneration refused. This disposable-save operation replaces the layout on next restart. Usage: ae_regenerate NEW_SEED {ManifestRegeneration.ConfirmationPhrase}";

        return _regenerationHandler(seed, confirmation);
    }
}
