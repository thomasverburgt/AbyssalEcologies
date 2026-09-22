using System;
using AbyssalEcologies.Core;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace AbyssalEcologies.Plugin;

[BepInPlugin(Guid, Name, Version)]
[BepInDependency("com.snmodding.nautilus")]
public sealed class Plugin : BaseUnityPlugin
{
    public const string Guid = "rocks.verburgt.subnautica.abyssalecologies";
    public const string Name = "Abyssal Ecologies";
    public const string Version = "0.1.0";

    internal static ManualLogSource Log { get; private set; } = null!;

    private void Awake()
    {
        Log = Logger;

        try
        {
            var settings = BindSettings();
            var generatedWorld = new ProceduralWorldGenerator().Generate(settings);
            ContentRegistrar.Register(generatedWorld);

            Logger.LogInfo($"{Name} {Version} registered {generatedWorld.Regions.Count} seeded micro-biomes (seed {generatedWorld.Seed}).");
            foreach (var region in generatedWorld.Regions)
                Logger.LogInfo($"Region '{region.DisplayName}' centered at {region.Center}; {region.Placements.Count} placements.");
        }
        catch (Exception exception)
        {
            Logger.LogError($"{Name} failed to initialize: {exception}");
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

