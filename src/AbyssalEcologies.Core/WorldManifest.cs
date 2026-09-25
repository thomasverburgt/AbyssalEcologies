using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace AbyssalEcologies.Core;

[DataContract]
public sealed class WorldManifest
{
    public const int CurrentSchemaVersion = 3;
    public const int OldestSupportedSchemaVersion = 1;
    public const int CurrentGeneratorVersion = 1;
    public const string DeterministicPlacementMode = "deterministic";
    public const string TerrainResolvedPlacementMode = "terrain-resolved";

    [DataMember(Name = "schemaVersion", Order = 1)]
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    [DataMember(Name = "generatorVersion", Order = 2)]
    public int GeneratorVersion { get; set; } = CurrentGeneratorVersion;

    [DataMember(Name = "seed", Order = 3)]
    public int Seed { get; set; }

    [DataMember(Name = "regions", Order = 4)]
    public List<ManifestRegion> Regions { get; set; } = new();

    [DataMember(Name = "placementMode", Order = 5)]
    public string PlacementMode { get; set; } = string.Empty;

    [DataMember(Name = "terrainResolved", Order = 6, EmitDefaultValue = false)]
    public bool TerrainResolved { get; set; }

    public int SourceSchemaVersion { get; internal set; } = CurrentSchemaVersion;
    public bool WasMigrated => SourceSchemaVersion != SchemaVersion;

    public static WorldManifest FromGeneratedWorld(GeneratedWorld world, bool terrainResolved)
    {
        if (world == null) throw new ArgumentNullException(nameof(world));

        return new WorldManifest
        {
            SchemaVersion = CurrentSchemaVersion,
            Seed = world.Seed,
            PlacementMode = terrainResolved ? TerrainResolvedPlacementMode : DeterministicPlacementMode,
            TerrainResolved = terrainResolved,
            Regions = world.Regions.Select(region => new ManifestRegion
            {
                ArchetypeId = region.ArchetypeId,
                DisplayName = region.DisplayName,
                Center = ManifestPoint.FromWorldPoint(region.Center),
                Radius = region.Radius,
                Placements = region.Placements.Select(placement => new ManifestPlacement
                {
                    ContentId = placement.ContentId,
                    Kind = placement.Kind,
                    Position = ManifestPoint.FromWorldPoint(placement.Position),
                    EulerAngles = ManifestPoint.FromWorldPoint(placement.EulerAngles),
                    Scale = placement.Scale
                }).ToList()
            }).ToList()
        };
    }

    public GeneratedWorld ToGeneratedWorld()
    {
        Validate();
        return new GeneratedWorld(
            Seed,
            Regions.Select(region => new GeneratedRegion(
                region.ArchetypeId,
                region.DisplayName,
                region.Center.ToWorldPoint(),
                region.Radius,
                region.Placements.Select(placement => new GeneratedPlacement(
                    placement.ContentId,
                    placement.Kind,
                    placement.Position.ToWorldPoint(),
                    placement.EulerAngles.ToWorldPoint(),
                    placement.Scale)).ToArray())).ToArray());
    }

    public void Validate()
    {
        if (SchemaVersion != CurrentSchemaVersion)
            throw new InvalidDataException($"Manifest schema {SchemaVersion} was not migrated to canonical schema {CurrentSchemaVersion}.");
        if (GeneratorVersion != CurrentGeneratorVersion)
            throw new InvalidDataException($"Unsupported generator version {GeneratorVersion}; expected {CurrentGeneratorVersion}.");
        if (PlacementMode != DeterministicPlacementMode && PlacementMode != TerrainResolvedPlacementMode)
            throw new InvalidDataException($"Unsupported placement mode '{PlacementMode}'.");
        if (TerrainResolved != (PlacementMode == TerrainResolvedPlacementMode))
            throw new InvalidDataException("Manifest placement mode contradicts its terrain-resolution flag.");
        if (Regions == null || Regions.Count == 0)
            throw new InvalidDataException("Manifest contains no regions.");

        foreach (var region in Regions)
        {
            if (region == null || string.IsNullOrWhiteSpace(region.ArchetypeId) || string.IsNullOrWhiteSpace(region.DisplayName))
                throw new InvalidDataException("Manifest contains an invalid region.");
            if (region.Center == null || region.Radius <= 0f || region.Placements == null || region.Placements.Count == 0)
                throw new InvalidDataException($"Manifest region '{region.ArchetypeId}' is incomplete.");

            foreach (var placement in region.Placements)
            {
                if (placement == null || string.IsNullOrWhiteSpace(placement.ContentId) || placement.Position == null || placement.EulerAngles == null || placement.Scale <= 0f)
                    throw new InvalidDataException($"Manifest region '{region.ArchetypeId}' contains an invalid placement.");
            }
        }
    }
}

[DataContract]
public sealed class ManifestRegion
{
    [DataMember(Name = "archetypeId", Order = 1)]
    public string ArchetypeId { get; set; } = string.Empty;

    [DataMember(Name = "displayName", Order = 2)]
    public string DisplayName { get; set; } = string.Empty;

    [DataMember(Name = "center", Order = 3)]
    public ManifestPoint Center { get; set; } = new();

    [DataMember(Name = "radius", Order = 4)]
    public float Radius { get; set; }

    [DataMember(Name = "placements", Order = 5)]
    public List<ManifestPlacement> Placements { get; set; } = new();
}

[DataContract]
public sealed class ManifestPlacement
{
    [DataMember(Name = "contentId", Order = 1)]
    public string ContentId { get; set; } = string.Empty;

    [DataMember(Name = "kind", Order = 2)]
    public PlacementKind Kind { get; set; }

    [DataMember(Name = "position", Order = 3)]
    public ManifestPoint Position { get; set; } = new();

    [DataMember(Name = "eulerAngles", Order = 4)]
    public ManifestPoint EulerAngles { get; set; } = new();

    [DataMember(Name = "scale", Order = 5)]
    public float Scale { get; set; }
}

[DataContract]
public sealed class ManifestPoint
{
    [DataMember(Name = "x", Order = 1)]
    public float X { get; set; }

    [DataMember(Name = "y", Order = 2)]
    public float Y { get; set; }

    [DataMember(Name = "z", Order = 3)]
    public float Z { get; set; }

    public static ManifestPoint FromWorldPoint(WorldPoint point) => new() { X = point.X, Y = point.Y, Z = point.Z };
    public WorldPoint ToWorldPoint() => new(X, Y, Z);
}

public static class WorldManifestSerializer
{
    private static readonly DataContractJsonSerializer Serializer = new(typeof(WorldManifest));

    public static string Serialize(WorldManifest manifest)
    {
        if (manifest == null) throw new ArgumentNullException(nameof(manifest));
        manifest.Validate();

        using var stream = new MemoryStream();
        Serializer.WriteObject(stream, manifest);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static WorldManifest Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new InvalidDataException("Manifest JSON is empty.");

        try
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var manifest = Serializer.ReadObject(stream) as WorldManifest
                ?? throw new InvalidDataException("Manifest JSON did not contain a world manifest.");
            WorldManifestMigrator.MigrateToCurrent(manifest);
            manifest.Validate();
            return manifest;
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new InvalidDataException("Manifest JSON is invalid.", exception);
        }
    }
}

public static class WorldManifestMigrator
{
    public static void MigrateToCurrent(WorldManifest manifest)
    {
        if (manifest == null) throw new ArgumentNullException(nameof(manifest));

        var sourceSchema = manifest.SchemaVersion;
        manifest.SourceSchemaVersion = sourceSchema;
        if (sourceSchema < WorldManifest.OldestSupportedSchemaVersion || sourceSchema > WorldManifest.CurrentSchemaVersion)
            throw new InvalidDataException($"Unsupported manifest schema {sourceSchema}; supported range is {WorldManifest.OldestSupportedSchemaVersion}-{WorldManifest.CurrentSchemaVersion}.");

        switch (sourceSchema)
        {
            case 1:
                if (manifest.TerrainResolved)
                    throw new InvalidDataException("Schema 1 manifests cannot claim terrain-resolved placements.");
                manifest.PlacementMode = WorldManifest.DeterministicPlacementMode;
                manifest.SchemaVersion = WorldManifest.CurrentSchemaVersion;
                break;
            case 2:
                if (!manifest.TerrainResolved)
                    throw new InvalidDataException("Schema 2 manifests must contain terrain-resolved placements.");
                manifest.PlacementMode = WorldManifest.TerrainResolvedPlacementMode;
                manifest.SchemaVersion = WorldManifest.CurrentSchemaVersion;
                break;
            case WorldManifest.CurrentSchemaVersion:
                break;
            default:
                throw new InvalidDataException($"No migration path exists for manifest schema {sourceSchema}.");
        }
    }
}
