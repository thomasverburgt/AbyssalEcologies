using System;
using System.Collections;
using AbyssalEcologies.Core;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Nautilus.Handlers;

namespace AbyssalEcologies.Plugin;

[BepInPlugin(Guid, Name, Version)]
[BepInDependency("com.snmodding.nautilus")]
public sealed class Plugin : BaseUnityPlugin
{
    public const string Guid = "rocks.verburgt.subnautica.abyssalecologies";
    public const string Name = "Abyssal Ecologies";
    public const string Version = "0.4.0";

    internal static ManualLogSource Log { get; private set; } = null!;

    private GenerationSettings _settings = null!;
    private AbyssalEcologiesSaveData _saveData = null!;
    private bool _worldRegistered;
    private string? _activeManifestJson;

    private void Awake()
    {
        Log = Logger;

        try
        {
            _settings = BindSettings();
            _saveData = SaveDataHandler.RegisterSaveDataCache<AbyssalEcologiesSaveData>();
            var definitionCount = ContentRegistrar.RegisterDefinitions();
            DiagnosticCommands.Register();
            WaitScreenHandler.RegisterLateAsyncLoadTask(Name, OnWorldReady, "Preparing manifest-backed micro-biomes");

            Logger.LogInfo($"{Name} {Version} registered {definitionCount} content definitions. World generation is waiting for a loaded save.");
        }
        catch (Exception exception)
        {
            Logger.LogError($"{Name} failed to initialize: {exception}");
        }
    }

    private IEnumerator OnWorldReady(WaitScreenHandler.WaitScreenTask task)
    {
        task.Status = "Loading the per-save ecology manifest";
        if (_worldRegistered)
        {
            var loadedManifestJson = _saveData.Manifest == null ? null : WorldManifestSerializer.Serialize(_saveData.Manifest);
            if (loadedManifestJson == _activeManifestJson)
            {
                Logger.LogInfo("This save's manifest is already registered for the current game session.");
                task.Status = "Existing ecology manifest remains active";
            }
            else
            {
                Logger.LogError("A different save manifest was loaded after coordinated spawns were registered. Restart Subnautica before switching save slots so layouts cannot be mixed.");
                task.Status = "Restart required before switching ecology manifests";
            }

            yield break;
        }

        GeneratedWorld world;
        if (_saveData.Manifest == null)
        {
            task.Status = "Generating protected deterministic micro-biomes";
            world = new ProceduralWorldGenerator().Generate(_settings);
            _saveData.Manifest = WorldManifest.FromGeneratedWorld(world, terrainResolved: false);
            Logger.LogWarning($"Generated safe legacy manifest schema {_saveData.Manifest.SchemaVersion} for this save using seed {world.Seed}. Remote terrain probing is disabled because forced batch streaming is unsafe during Subnautica's late load phase.");
            yield return null;
        }
        else
        {
            world = _saveData.Manifest.ToGeneratedWorld();
            if (_saveData.Manifest.TerrainResolved)
                Logger.LogInfo($"Loaded terrain-resolved manifest schema {_saveData.Manifest.SchemaVersion} for this save using persisted seed {world.Seed}; current global generation settings were ignored.");
            else
                Logger.LogWarning($"Loaded schema {_saveData.Manifest.SchemaVersion} manifest using its validated deterministic coordinates unchanged. Runtime terrain probing is disabled in {Version}.");
        }

        try
        {
            ContentRegistrar.RegisterWorldSpawns(world);
            DiagnosticCommands.SetActive(_saveData.Manifest, world);
            _activeManifestJson = WorldManifestSerializer.Serialize(_saveData.Manifest);
            _worldRegistered = true;
            task.Status = $"Registered {world.Regions.Count} micro-biomes";

            Logger.LogInfo($"Registered {world.Regions.Count} manifest-backed micro-biomes after the save finished loading.");
            foreach (var region in world.Regions)
                Logger.LogInfo($"Region '{region.DisplayName}' centered at {region.Center}; {region.Placements.Count} placements.");
        }
        catch (Exception exception)
        {
            Logger.LogError($"{Name} could not activate the loaded save's manifest: {exception}");
            throw;
        }
    }

    private GenerationSettings BindSettings()
    {
        ConfigEntry<int> seed = Config.Bind("Generation", "Seed", 451230, "Stable world-generation seed. Keep this unchanged for an existing save.");
        ConfigEntry<int> regionCount = Config.Bind("Generation", "RegionCount", 3, new ConfigDescription("Number of generated micro-biomes.", new AcceptableValueRange<int>(1, 12)));
        ConfigEntry<float> minimumRadius = Config.Bind("Generation", "MinimumMapRadius", 550f, "Minimum horizontal distance from world origin.");
        ConfigEntry<float> maximumRadius = Config.Bind("Generation", "MaximumMapRadius", 1350f, "Maximum horizontal distance from world origin.");
        ConfigEntry<float> minimumDepth = Config.Bind("Generation", "MinimumDepth", 180f, "Shallowest generated center depth.");
        ConfigEntry<float> maximumDepth = Config.Bind("Generation", "MaximumDepth", 520f, "Deepest generated center depth.");
        ConfigEntry<float> separation = Config.Bind("Generation", "MinimumRegionSeparation", 420f, "Minimum horizontal separation between region centers.");

        return new GenerationSettings
        {
            Seed = seed.Value,
            RegionCount = regionCount.Value,
            MinimumMapRadius = minimumRadius.Value,
            MaximumMapRadius = maximumRadius.Value,
            MinimumDepth = minimumDepth.Value,
            MaximumDepth = maximumDepth.Value,
            MinimumRegionSeparation = separation.Value
        };
    }
}
