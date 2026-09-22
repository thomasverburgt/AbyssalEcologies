using System;
using System.Collections.Generic;
using System.Linq;
using AbyssalEcologies.Core;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using UnityEngine;

namespace AbyssalEcologies.Plugin;

internal static class ContentRegistrar
{
    private static readonly ContentDefinition[] Definitions =
    {
        new("glassfin", "Glassfin", "A translucent filter-feeder drawn to crystalline kelp.", "Peeper", new Color(0.25f, 0.95f, 1f), new Color(0.1f, 0.65f, 1f)),
        new("cinder-ray", "Cinder Ray", "A heat-tolerant ray whose fins scatter ember-like light.", "RabbitRay", new Color(1f, 0.28f, 0.08f), new Color(1f, 0.12f, 0.02f)),
        new("lantern-skate", "Lantern Skate", "A gentle grazer that pulses with cold blue bioluminescence.", "Jellyray", new Color(0.35f, 0.45f, 1f), new Color(0.25f, 0.15f, 1f)),

        new("prism-kelp", "Prism Kelp", "A reflective kelp analogue growing in dense aerial gardens.", "Creepvine", new Color(0.2f, 0.9f, 0.85f), new Color(0.1f, 0.8f, 1f)),
        new("ember-fan", "Ember Fan", "A fan-shaped colony adapted to geothermal water.", "PurpleFan", new Color(1f, 0.22f, 0.03f), new Color(1f, 0.08f, 0.01f)),
        new("ghost-bloom", "Ghost Bloom", "A pale colony that shelters juvenile lantern skates.", "SmallFan", new Color(0.55f, 0.75f, 1f), new Color(0.2f, 0.45f, 1f)),

        new("glass-arch", "Glass Arch", "The mineralized heart of a Glass Kelp Garden.", "CoralShellPlate", new Color(0.2f, 0.9f, 1f), new Color(0.15f, 0.65f, 1f)),
        new("thermal-spire", "Thermal Spire", "A mineral chimney marking an Ember Trench colony.", "DrillableSulphur", new Color(1f, 0.25f, 0.02f), new Color(1f, 0.08f, 0.01f)),
        new("nursery-heart", "Nursery Heart", "A vast bloom at the center of a Ghostlight Nursery.", "MembrainTree", new Color(0.45f, 0.65f, 1f), new Color(0.2f, 0.3f, 1f))
    };

    public static void Register(GeneratedWorld world)
    {
        var placementsByContent = world.Regions
            .SelectMany(region => region.Placements)
            .GroupBy(placement => placement.ContentId)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        foreach (var definition in Definitions)
        {
            if (!placementsByContent.TryGetValue(definition.Id, out var placements))
                continue;

            if (!Enum.TryParse(definition.SourceTechType, ignoreCase: false, out TechType sourceTechType))
            {
                Plugin.Log.LogWarning($"Skipping '{definition.DisplayName}': source TechType '{definition.SourceTechType}' is unavailable in this game build.");
                continue;
            }

            RegisterDefinition(definition, sourceTechType, placements);
        }
    }

    private static void RegisterDefinition(ContentDefinition definition, TechType sourceTechType, IReadOnlyCollection<GeneratedPlacement> placements)
    {
        var classId = $"AbyssalEcologies_{definition.Id.Replace('-', '_')}";
        var prefab = new CustomPrefab(classId, definition.DisplayName, definition.Description);
        var template = new CloneTemplate(prefab.Info, sourceTechType)
        {
            ModifyPrefab = gameObject => ApplyAppearance(gameObject, definition)
        };

        prefab.SetGameObject(template);
        prefab.SetSpawns(placements.Select(ToSpawnLocation).ToArray());
        prefab.Register();
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

