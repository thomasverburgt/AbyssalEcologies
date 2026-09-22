using System;
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
    public const string Version = "0.2.0";

    internal static ManualLogSource Log { get; private set; } = null!;

    private GenerationSettings _settings = null!;
    private AbyssalEcologiesSaveData _saveData = null!;
    private bool _worldRegistered;

    private void Awake()
    {
        Log = Logger;

        try
        {
            _settings = BindSettings();
            _saveData = SaveDataHandler.RegisterSaveDataCache<AbyssalEcologiesSaveData>();
            var definitionCount = ContentRegistrar.RegisterDefinitions();
            WaitScreenHandler.RegisterLateLoadTask(Name, OnWorldReady, "Preparing manifest-backed micro-biomes");

            Logger.LogInfo($"{Name} {Version} registered {definitionCount} content definitions. World generation is waiting for a loaded save.");
        }
        catch (Exception exception)
        {
            Logger.LogError($"{Name} failed to initialize: {exception}");
        }
    }

    private void OnWorldReady(WaitScreenHandler.WaitScreenTask task)
    {
        task.Status = "Loading the per-save ecology manifest";
        if (_worldRegistered)
        {
            Logger.LogWarning("Ignoring a duplicate world-ready event; this session's placements are already registered.");
            return;
        }

        try
        {
            GeneratedWorld world;
            if (_saveData.Manifest == null)
            {
                world = new ProceduralWorldGenerator().Generate(_settings);
                _saveData.Manifest = WorldManifest.FromGeneratedWorld(world);
                Logger.LogInfo($"Generated manifest schema {WorldManifest.CurrentSchemaVersion} for this save using seed {world.Seed}.");
            }
            else
            {
                world = _saveData.Manifest.ToGeneratedWorld();
                Logger.LogInfo($"Loaded manifest schema {_saveData.Manifest.SchemaVersion} for this save using persisted seed {world.Seed}; current global generation settings were ignored.");
            }

            ContentRegistrar.RegisterWorldSpawns(world);
            _worldRegistered = true;
            task.Status = $"Registered {world.Regions.Count} micro-biomes";

            Logger.LogInfo($"Registered {world.Regions.Count} manifest-backed micro-biomes after the save finished loading.");
            foreach (var region in world.Regions)
                Logger.LogInfo($"Region '{region.DisplayName}' centered at {region.Center}; {region.Placements.Count} placements.");
        }
        catch (Exception exception)
        {
            Logger.LogError($"{Name} could not activate the loaded save's manifest: {exception}");
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
