using System;
using System.Collections.Generic;
using System.Linq;
using AbyssalEcologies.Core;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Handlers;
using UnityEngine;

namespace AbyssalEcologies.Plugin;

internal static class ContentRegistrar
{
    private const int InstanceLogLimitPerContent = 3;
    private static readonly Dictionary<string, TechType> RegisteredTechTypes = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, int> LoggedInstanceCounts = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, int> InstantiationCounts = new(StringComparer.Ordinal);
    private static readonly List<WeakReference> LiveInstances = new();
    private static int _registeredPlacementCount;

    private static readonly ContentDefinition[] Definitions =
    {
        new("glassfin", "Glassfin", "A translucent filter-feeder drawn to crystalline kelp.", "Peeper", new Color(0.25f, 0.95f, 1f), new Color(0.1f, 0.65f, 1f)),
        new("cinder-ray", "Cinder Ray", "A heat-tolerant ray whose fins scatter ember-like light.", "RabbitRay", new Color(1f, 0.28f, 0.08f), new Color(1f, 0.12f, 0.02f)),
        new("lantern-skate", "Lantern Skate", "A gentle grazer that pulses with cold blue bioluminescence.", "Jellyray", new Color(0.35f, 0.45f, 1f), new Color(0.25f, 0.15f, 1f)),

        new("prism-kelp", "Prism Kelp", "A reflective kelp analogue growing in dense aerial gardens.", "Creepvine", new Color(0.2f, 0.9f, 0.85f), new Color(0.1f, 0.8f, 1f)),
        new("ember-fan", "Ember Fan", "A fan-shaped colony adapted to geothermal water.", "PurpleFan", new Color(1f, 0.22f, 0.03f), new Color(1f, 0.08f, 0.01f)),
        new("ghost-bloom", "Ghost Bloom", "A pale colony that shelters juvenile lantern skates.", "SmallFan", new Color(0.55f, 0.75f, 1f), new Color(0.2f, 0.45f, 1f)),

        new("glass-arch", "Glass Arch", "The mineralized heart of a Glass Kelp Garden.", "CoralShellPlate", new Color(0.2f, 0.9f, 1f), new Color(0.15f, 0.65f, 1f)),
        new("thermal-spire", "Thermal Spire", "A towering heat-bright colony marking an Ember Trench.", "MembrainTree", new Color(1f, 0.25f, 0.02f), new Color(1f, 0.08f, 0.01f)),
        new("nursery-heart", "Nursery Heart", "A vast bloom at the center of a Ghostlight Nursery.", "MembrainTree", new Color(0.45f, 0.65f, 1f), new Color(0.2f, 0.3f, 1f))
    };

    public static int RegisterDefinitions()
    {
        foreach (var definition in Definitions)
        {
            if (!Enum.TryParse(definition.SourceTechType, ignoreCase: false, out TechType sourceTechType))
            {
                Plugin.Log.LogWarning($"Skipping '{definition.DisplayName}': source TechType '{definition.SourceTechType}' is unavailable in this game build.");
                continue;
            }

            RegisterDefinition(definition, sourceTechType);
        }

        return RegisteredTechTypes.Count;
    }

    public static SpawnRegistrationMetrics RegisterWorldSpawns(GeneratedWorld world)
    {
        LoggedInstanceCounts.Clear();
        InstantiationCounts.Clear();
        LiveInstances.Clear();
        _registeredPlacementCount = 0;

        var placementsByContent = world.Regions
            .SelectMany(region => region.Placements)
            .GroupBy(placement => placement.ContentId)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var registeredContentTypeCount = 0;

        foreach (var pair in placementsByContent)
        {
            if (!RegisteredTechTypes.TryGetValue(pair.Key, out var techType))
            {
                Plugin.Log.LogWarning($"Manifest content '{pair.Key}' has no registered prefab definition; skipping {pair.Value.Length} placements.");
                continue;
            }

            var contentId = pair.Key;
            var spawnInfos = pair.Value.Select(placement => ToSpawnInfo(techType, contentId, placement)).ToList();
            CoordinatedSpawnsHandler.RegisterCoordinatedSpawns(spawnInfos);
            _registeredPlacementCount += pair.Value.Length;
            registeredContentTypeCount++;
            Plugin.Log.LogInfo($"Registered {pair.Value.Length} coordinated spawns for '{pair.Key}'.");
        }

        for (var index = 0; index < world.Regions.Count; index++)
        {
            var region = world.Regions[index];
            var commandName = $"ae{index + 1}";
            ConsoleCommandsHandler.AddGotoTeleportPosition(commandName, new Vector3(region.Center.X, region.Center.Y + 8f, region.Center.Z));
            Plugin.Log.LogInfo($"Field-check teleport: 'goto {commandName}' -> {region.DisplayName} {region.Center}.");
        }

        return new SpawnRegistrationMetrics(_registeredPlacementCount, registeredContentTypeCount);
    }

    public static RuntimeSpawnMetrics GetRuntimeMetrics()
    {
        var activeCount = 0;
        for (var index = LiveInstances.Count - 1; index >= 0; index--)
        {
            var reference = LiveInstances[index];
            var gameObject = reference.Target as GameObject;
            if (!reference.IsAlive || gameObject == null)
                LiveInstances.RemoveAt(index);
            else
                activeCount++;
        }

        return new RuntimeSpawnMetrics(
            _registeredPlacementCount,
            InstantiationCounts.Values.Sum(),
            activeCount,
            new Dictionary<string, int>(InstantiationCounts, StringComparer.Ordinal));
    }

    private static void RegisterDefinition(ContentDefinition definition, TechType sourceTechType)
    {
        var classId = $"AbyssalEcologies_{definition.Id.Replace('-', '_')}";
        var prefab = new CustomPrefab(classId, definition.DisplayName, definition.Description);
        var template = new CloneTemplate(prefab.Info, sourceTechType)
        {
            ModifyPrefab = gameObject => ApplyAppearance(gameObject, definition)
        };

        prefab.SetGameObject(template);
        prefab.Register();
        RegisteredTechTypes.Add(definition.Id, prefab.Info.TechType);
        Plugin.Log.LogInfo($"Registered '{definition.Id}' definition from proven source TechType '{definition.SourceTechType}'.");
    }

    private static SpawnLocation ToSpawnLocation(GeneratedPlacement placement)
    {
        var position = new Vector3(placement.Position.X, placement.Position.Y, placement.Position.Z);
        var angles = new Vector3(placement.EulerAngles.X, placement.EulerAngles.Y, placement.EulerAngles.Z);
        var scale = Vector3.one * placement.Scale;
        return new SpawnLocation(position, angles, scale);
    }

    private static void ApplyAppearance(GameObject gameObject, ContentDefinition definition)
    {
        gameObject.name = definition.DisplayName;
        foreach (var renderer in gameObject.GetComponentsInChildren<Renderer>(true))
        {
            foreach (var material in renderer.materials)
            {
                if (material.HasProperty("_Color"))
                    material.SetColor("_Color", definition.Tint);
                if (material.HasProperty("_GlowColor"))
                    material.SetColor("_GlowColor", definition.Glow);
                if (material.HasProperty("_GlowStrength"))
                    material.SetFloat("_GlowStrength", 1.4f);
                if (material.HasProperty("_GlowStrengthNight"))
                    material.SetFloat("_GlowStrengthNight", 2.2f);
            }
        }
    }

    private static SpawnInfo ToSpawnInfo(TechType techType, string contentId, GeneratedPlacement placement)
    {
        var location = ToSpawnLocation(placement);
        return new SpawnInfo(
            techType,
            location.Position,
            Quaternion.Euler(location.EulerAngles),
            location.Scale,
            gameObject => LogSuccessfulInstance(contentId, gameObject));
    }

    private static void LogSuccessfulInstance(string contentId, GameObject gameObject)
    {
        InstantiationCounts.TryGetValue(contentId, out var totalCount);
        InstantiationCounts[contentId] = totalCount + 1;
        LiveInstances.Add(new WeakReference(gameObject));

        LoggedInstanceCounts.TryGetValue(contentId, out var count);
        if (count >= InstanceLogLimitPerContent)
            return;

        LoggedInstanceCounts[contentId] = count + 1;
        Plugin.Log.LogInfo($"Instantiated '{contentId}' at {gameObject.transform.position} (sample {count + 1}/{InstanceLogLimitPerContent}).");
    }

    internal sealed class SpawnRegistrationMetrics
    {
        public SpawnRegistrationMetrics(int placementCount, int contentTypeCount)
        {
            PlacementCount = placementCount;
            ContentTypeCount = contentTypeCount;
        }

        public int PlacementCount { get; }
        public int ContentTypeCount { get; }
    }

    internal sealed class RuntimeSpawnMetrics
    {
        public RuntimeSpawnMetrics(int registeredPlacementCount, int instantiationCallbacks, int activeInstanceCount, IReadOnlyDictionary<string, int> instantiationCounts)
        {
            RegisteredPlacementCount = registeredPlacementCount;
            InstantiationCallbacks = instantiationCallbacks;
            ActiveInstanceCount = activeInstanceCount;
            InstantiationCounts = instantiationCounts;
        }

        public int RegisteredPlacementCount { get; }
        public int InstantiationCallbacks { get; }
        public int ActiveInstanceCount { get; }
        public IReadOnlyDictionary<string, int> InstantiationCounts { get; }
    }

    private sealed class ContentDefinition
    {
        public ContentDefinition(string id, string displayName, string description, string sourceTechType, Color tint, Color glow)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            SourceTechType = sourceTechType;
            Tint = tint;
            Glow = glow;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string SourceTechType { get; }
        public Color Tint { get; }
        public Color Glow { get; }
    }
}
