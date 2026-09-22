using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AbyssalEcologies.Core;

var failures = new List<string>();
Check("same seed is deterministic", SameSeedIsDeterministic);
Check("different seeds differ", DifferentSeedsDiffer);
Check("regions obey invariants", RegionsObeyInvariants);
Check("one hundred seeds obey invariants", OneHundredSeedsObeyInvariants);
Check("manifest serialization is byte stable", ManifestSerializationIsByteStable);
Check("schema 1 fixture loads without drift", SchemaOneFixtureLoadsWithoutDrift);
Check("manifest preserves saved layout over new settings", ManifestPreservesSavedLayout);
Check("corrupt manifest is rejected", CorruptManifestIsRejected);
Check("future manifest schema is rejected", FutureManifestSchemaIsRejected);

if (failures.Count > 0)
{
    Console.Error.WriteLine($"{failures.Count} generator check(s) failed:");
    foreach (var failure in failures) Console.Error.WriteLine($" - {failure}");
    return 1;
}

var preview = new ProceduralWorldGenerator().Generate(new GenerationSettings());
Console.WriteLine($"All generator checks passed. Preview seed: {preview.Seed}");
foreach (var region in preview.Regions)
    Console.WriteLine($" - {region.DisplayName,-24} center={region.Center} placements={region.Placements.Count}");
return 0;

void Check(string name, Action check)
{
    try
    {
        check();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception exception)
    {
        failures.Add($"{name}: {exception.Message}");
    }
}

static void SameSeedIsDeterministic()
{
    var generator = new ProceduralWorldGenerator();
    var first = Flatten(generator.Generate(new GenerationSettings { Seed = 99 }));
    var second = Flatten(generator.Generate(new GenerationSettings { Seed = 99 }));
    Require(first.SequenceEqual(second), "identical seeds produced different layouts");
}

static void DifferentSeedsDiffer()
{
    var generator = new ProceduralWorldGenerator();
    var first = Flatten(generator.Generate(new GenerationSettings { Seed = 100 }));
    var second = Flatten(generator.Generate(new GenerationSettings { Seed = 101 }));
    Require(!first.SequenceEqual(second), "different seeds produced identical layouts");
}

static void RegionsObeyInvariants()
{
    var settings = new GenerationSettings { Seed = 88201, RegionCount = 6 };
    var world = new ProceduralWorldGenerator().Generate(settings);
    AssertWorldInvariants(world, settings);
}

static void OneHundredSeedsObeyInvariants()
{
    var generator = new ProceduralWorldGenerator();
    for (var seed = 0; seed < 100; seed++)
    {
        var settings = new GenerationSettings { Seed = seed, RegionCount = 6 };
        AssertWorldInvariants(generator.Generate(settings), settings);
    }
}

static void AssertWorldInvariants(GeneratedWorld world, GenerationSettings settings)
{
    Require(world.Regions.Count == settings.RegionCount, "wrong region count");
    Require(world.Regions.Take(3).Select(region => region.ArchetypeId).Distinct().Count() == 3, "first archetype cycle contains duplicates");

    foreach (var region in world.Regions)
    {
        var originDistanceSquared = (region.Center.X * region.Center.X) + (region.Center.Z * region.Center.Z);
        Require(originDistanceSquared >= settings.MinimumMapRadius * settings.MinimumMapRadius, "region entered start exclusion zone");
        Require(originDistanceSquared <= settings.MaximumMapRadius * settings.MaximumMapRadius + 1f, "region exceeded map radius");
        Require(region.Center.Y <= -settings.MinimumDepth && region.Center.Y >= -settings.MaximumDepth, "region depth is invalid");
        Require(region.Placements.Count == 41, "unexpected placement count");
        Require(region.Placements.Count(p => p.Kind == PlacementKind.Landmark) == 1, "region must have one landmark");
        Require(region.Placements.All(p => !string.IsNullOrWhiteSpace(p.ContentId)), "placement content id is missing");
    }

    for (var i = 0; i < world.Regions.Count; i++)
    for (var j = i + 1; j < world.Regions.Count; j++)
        Require(world.Regions[i].Center.HorizontalDistanceSquared(world.Regions[j].Center) >= settings.MinimumRegionSeparation * settings.MinimumRegionSeparation, "regions overlap");
}

static void ManifestSerializationIsByteStable()
{
    var world = new ProceduralWorldGenerator().Generate(new GenerationSettings { Seed = 9471 });
    var first = WorldManifestSerializer.Serialize(WorldManifest.FromGeneratedWorld(world));
    var reloaded = WorldManifestSerializer.Deserialize(first);
    var second = WorldManifestSerializer.Serialize(reloaded);

    Require(first == second, "serialize-load-serialize changed manifest bytes");
    Require(Flatten(world).SequenceEqual(Flatten(reloaded.ToGeneratedWorld())), "manifest round trip changed placements");
}

static void SchemaOneFixtureLoadsWithoutDrift()
{
    var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "manifest-v1.json");
    var fixture = File.ReadAllText(fixturePath).Trim();
    var manifest = WorldManifestSerializer.Deserialize(fixture);

    Require(manifest.Seed == 42, "fixture seed changed");
    Require(manifest.Regions.Count == 1, "fixture region count changed");
    Require(manifest.Regions[0].Placements[0].ContentId == "fixture-content", "fixture content identifier changed");
    Require(WorldManifestSerializer.Serialize(manifest) == fixture, "schema 1 fixture did not serialize byte-equivalently");
}

static void ManifestPreservesSavedLayout()
{
    var generator = new ProceduralWorldGenerator();
    var saved = WorldManifest.FromGeneratedWorld(generator.Generate(new GenerationSettings { Seed = 3001 }));
    var changedGlobalLayout = generator.Generate(new GenerationSettings { Seed = 9009 });

    Require(!Flatten(saved.ToGeneratedWorld()).SequenceEqual(Flatten(changedGlobalLayout)), "test seeds unexpectedly produced the same layout");
    Require(saved.ToGeneratedWorld().Seed == 3001, "manifest did not preserve its original seed");
}

static void CorruptManifestIsRejected()
{
    RequireThrows(() => WorldManifestSerializer.Deserialize("{ definitely-not-json"), "corrupt JSON was accepted");
}

static void FutureManifestSchemaIsRejected()
{
    var manifest = WorldManifest.FromGeneratedWorld(new ProceduralWorldGenerator().Generate(new GenerationSettings()));
    manifest.SchemaVersion = WorldManifest.CurrentSchemaVersion + 1;
    RequireThrows(manifest.Validate, "future schema was accepted");
}

static IReadOnlyList<string> Flatten(GeneratedWorld world) => world.Regions
    .SelectMany(region => region.Placements.Select(placement => $"{region.ArchetypeId}|{placement.ContentId}|{placement.Position}|{placement.Scale:0.000}"))
    .ToArray();

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static void RequireThrows(Action action, string message)
{
    try
    {
        action();
    }
    catch
    {
        return;
    }

    throw new InvalidOperationException(message);
}
