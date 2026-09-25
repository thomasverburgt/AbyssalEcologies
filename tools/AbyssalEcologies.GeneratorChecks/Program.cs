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
Check("protected areas reject region centers", ProtectedAreasRejectRegionCenters);
Check("replacement search is deterministic", ReplacementSearchIsDeterministic);
Check("manifest serialization is byte stable", ManifestSerializationIsByteStable);
Check("schema 1 fixture migrates without layout drift", SchemaOneFixtureMigratesWithoutLayoutDrift);
Check("schema 2 fixture migrates without layout drift", SchemaTwoFixtureMigratesWithoutLayoutDrift);
Check("schema 3 fixture loads without drift", SchemaThreeFixtureLoadsWithoutDrift);
Check("manifest preserves saved layout over new settings", ManifestPreservesSavedLayout);
Check("corrupt manifest is rejected", CorruptManifestIsRejected);
Check("unsupported old manifest schema is rejected", UnsupportedOldManifestSchemaIsRejected);
Check("contradictory legacy manifest is rejected", ContradictoryLegacyManifestIsRejected);
Check("future manifest schema is rejected", FutureManifestSchemaIsRejected);
Check("world diagnostics accept generated layouts", WorldDiagnosticsAcceptGeneratedLayouts);
Check("world diagnostics reject invalid layouts", WorldDiagnosticsRejectInvalidLayouts);
Check("world diagnostics preserve legacy exclusion findings as warnings", WorldDiagnosticsPreserveLegacyExclusionWarnings);
Check("performance budget accepts bounded measurements", PerformanceBudgetAcceptsBoundedMeasurements);
Check("performance budget rejects overruns and count drift", PerformanceBudgetRejectsOverrunsAndCountDrift);

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
        Require(!WorldProtectionCatalog.TryFindExclusion(region.Center, 140f, out _), "region center entered a protected area");
    }

    for (var i = 0; i < world.Regions.Count; i++)
    for (var j = i + 1; j < world.Regions.Count; j++)
        Require(world.Regions[i].Center.HorizontalDistanceSquared(world.Regions[j].Center) >= settings.MinimumRegionSeparation * settings.MinimumRegionSeparation, "regions overlap");
}

static void ProtectedAreasRejectRegionCenters()
{
    foreach (var area in WorldProtectionCatalog.All)
    {
        Require(WorldProtectionCatalog.TryFindExclusion(new WorldPoint(area.X, -100f, area.Z), 0f, out _), $"protected area '{area.Id}' did not reject its center");
    }

    Require(!WorldProtectionCatalog.TryFindExclusion(new WorldPoint(0f, -100f, 700f), 0f, out _), "known open-water fixture was rejected");
}

static void ReplacementSearchIsDeterministic()
{
    var origin = new WorldPoint(10f, -200f, 20f);
    var first = PlacementSearchPattern.Around(origin, 991, 48f).ToArray();
    var second = PlacementSearchPattern.Around(origin, 991, 48f).ToArray();
    var changed = PlacementSearchPattern.Around(origin, 992, 48f).ToArray();

    Require(first.Length == 25, "replacement search sample count changed");
    Require(first.SequenceEqual(second), "replacement search changed for the same stable offset");
    Require(!first.Skip(1).SequenceEqual(changed.Skip(1)), "replacement search phase ignored its stable offset");
    Require(first[0].Equals(origin), "replacement search did not try the original position first");
}

static void ManifestSerializationIsByteStable()
{
    var world = new ProceduralWorldGenerator().Generate(new GenerationSettings { Seed = 9471 });
    var first = WorldManifestSerializer.Serialize(WorldManifest.FromGeneratedWorld(world, terrainResolved: true));
    var reloaded = WorldManifestSerializer.Deserialize(first);
    var second = WorldManifestSerializer.Serialize(reloaded);

    Require(first == second, "serialize-load-serialize changed manifest bytes");
    Require(Flatten(world).SequenceEqual(Flatten(reloaded.ToGeneratedWorld())), "manifest round trip changed placements");
}

static void SchemaOneFixtureMigratesWithoutLayoutDrift()
{
    var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "manifest-v1.json");
    var manifest = WorldManifestSerializer.Deserialize(File.ReadAllText(fixturePath));

    Require(manifest.Seed == 42, "fixture seed changed");
    Require(manifest.Regions.Count == 1, "fixture region count changed");
    Require(manifest.Regions[0].Placements[0].ContentId == "fixture-content", "fixture content identifier changed");
    Require(manifest.SourceSchemaVersion == 1 && manifest.WasMigrated, "schema 1 migration was not recorded");
    Require(manifest.SchemaVersion == WorldManifest.CurrentSchemaVersion, "schema 1 did not migrate to the current schema");
    Require(manifest.PlacementMode == WorldManifest.DeterministicPlacementMode && !manifest.TerrainResolved, "schema 1 migration chose the wrong placement mode");
    Require(manifest.ExclusionCatalogVersion == 0, "schema 1 migration did not preserve its legacy exclusion policy");
    Require(manifest.Regions[0].Placements[0].Position.Y == -5f, "schema 1 migration changed placement coordinates");
}

static void SchemaTwoFixtureMigratesWithoutLayoutDrift()
{
    var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "manifest-v2.json");
    var manifest = WorldManifestSerializer.Deserialize(File.ReadAllText(fixturePath));

    Require(manifest.SourceSchemaVersion == 2 && manifest.WasMigrated, "schema 2 migration was not recorded");
    Require(manifest.SchemaVersion == WorldManifest.CurrentSchemaVersion, "schema 2 did not migrate to the current schema");
    Require(manifest.TerrainResolved, "schema 2 fixture lost terrain resolution state");
    Require(manifest.PlacementMode == WorldManifest.TerrainResolvedPlacementMode, "schema 2 migration chose the wrong placement mode");
    Require(manifest.ExclusionCatalogVersion == 0, "schema 2 migration did not preserve its legacy exclusion policy");
    Require(manifest.Regions[0].Placements[0].Position.Y == -205f, "schema 2 fixture placement changed");
}

static void SchemaThreeFixtureLoadsWithoutDrift()
{
    var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "manifest-v3.json");
    var fixture = File.ReadAllText(fixturePath).Trim();
    var manifest = WorldManifestSerializer.Deserialize(fixture);

    Require(manifest.SourceSchemaVersion == 3 && !manifest.WasMigrated, "schema 3 fixture was treated as migrated");
    Require(manifest.PlacementMode == WorldManifest.DeterministicPlacementMode, "schema 3 fixture placement mode changed");
    Require(manifest.ExclusionCatalogVersion == WorldManifest.CurrentExclusionCatalogVersion, "schema 3 exclusion catalog changed");
    Require(WorldManifestSerializer.Serialize(manifest) == fixture, "schema 3 fixture did not serialize byte-equivalently");
}

static void ManifestPreservesSavedLayout()
{
    var generator = new ProceduralWorldGenerator();
    var saved = WorldManifest.FromGeneratedWorld(generator.Generate(new GenerationSettings { Seed = 3001 }), terrainResolved: true);
    var changedGlobalLayout = generator.Generate(new GenerationSettings { Seed = 9009 });

    Require(!Flatten(saved.ToGeneratedWorld()).SequenceEqual(Flatten(changedGlobalLayout)), "test seeds unexpectedly produced the same layout");
    Require(saved.ToGeneratedWorld().Seed == 3001, "manifest did not preserve its original seed");
}

static void CorruptManifestIsRejected()
{
    RequireThrows(() => WorldManifestSerializer.Deserialize("{ definitely-not-json"), "corrupt JSON was accepted");
}

static void UnsupportedOldManifestSchemaIsRejected()
{
    var fixture = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "manifest-v1.json"));
    RequireThrows(() => WorldManifestSerializer.Deserialize(fixture.Replace("\"schemaVersion\":1", "\"schemaVersion\":0")), "unsupported old schema was accepted");
}

static void ContradictoryLegacyManifestIsRejected()
{
    var fixture = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "manifest-v1.json")).TrimEnd();
    var contradictory = fixture.Substring(0, fixture.Length - 1) + ",\"terrainResolved\":true}";
    RequireThrows(() => WorldManifestSerializer.Deserialize(contradictory), "contradictory schema 1 manifest was accepted");
}

static void FutureManifestSchemaIsRejected()
{
    var manifest = WorldManifest.FromGeneratedWorld(new ProceduralWorldGenerator().Generate(new GenerationSettings()), terrainResolved: true);
    manifest.SchemaVersion = WorldManifest.CurrentSchemaVersion + 1;
    RequireThrows(manifest.Validate, "future schema was accepted");
}

static void WorldDiagnosticsAcceptGeneratedLayouts()
{
    var generator = new ProceduralWorldGenerator();
    for (var seed = 0; seed < 100; seed++)
    {
        var world = generator.Generate(new GenerationSettings { Seed = seed });
        var report = WorldDiagnostics.Validate(world);
        Require(report.IsValid, $"seed {seed}: {string.Join(" | ", report.Errors)}");
        Require(report.RegionCount == 3, "diagnostics changed the region count");
        Require(report.PlacementCount == 123, "diagnostics changed the placement count");
    }
}

static void WorldDiagnosticsRejectInvalidLayouts()
{
    var invalidPlacement = new GeneratedPlacement("bad", PlacementKind.Landmark, new WorldPoint(500f, -20f, 500f), new WorldPoint(), -1f);
    var invalidRegion = new GeneratedRegion("bad", "Bad Region", new WorldPoint(500f, -20f, 500f), 10f, new[] { invalidPlacement });
    var report = WorldDiagnostics.Validate(new GeneratedWorld(1, new[] { invalidRegion }));
    Require(!report.IsValid, "diagnostics accepted an invalid layout");
    Require(report.Errors.Count >= 2, "diagnostics did not report bounded invariant failures");
}

static void WorldDiagnosticsPreserveLegacyExclusionWarnings()
{
    var center = new WorldPoint(-1120f, -250f, -685f);
    var placements = new List<GeneratedPlacement>
    {
        new("legacy-landmark", PlacementKind.Landmark, center, new WorldPoint(), 1f)
    };
    for (var index = 1; index < 41; index++)
        placements.Add(new GeneratedPlacement("legacy-content", PlacementKind.Flora, center, new WorldPoint(), 1f));
    var world = new GeneratedWorld(1, new[] { new GeneratedRegion("legacy", "Legacy Region", center, 100f, placements) });

    var currentPolicy = WorldDiagnostics.Validate(world, enforceCurrentExclusions: true);
    var legacyPolicy = WorldDiagnostics.Validate(world, enforceCurrentExclusions: false);
    Require(!currentPolicy.IsValid, "current exclusion policy accepted a protected layout");
    Require(legacyPolicy.IsValid, "legacy exclusion policy converted compatibility findings into errors");
    Require(legacyPolicy.Warnings.Count > 0, "legacy exclusion policy did not report warnings");
}

static void PerformanceBudgetAcceptsBoundedMeasurements()
{
    var measurement = new WorldPerformanceMeasurement(40d, 12d, 2L * 1024L * 1024L, 123, 123);
    var report = PerformanceBudget.Evaluate(measurement);
    Require(report.Passed, string.Join(" | ", report.Failures));
}

static void PerformanceBudgetRejectsOverrunsAndCountDrift()
{
    var measurement = new WorldPerformanceMeasurement(
        PerformanceBudget.MaximumLateSetupMilliseconds + 1d,
        PerformanceBudget.MaximumRegistrationMilliseconds + 1d,
        PerformanceBudget.MaximumManagedMemoryDeltaBytes + 1L,
        122,
        123);
    var report = PerformanceBudget.Evaluate(measurement);
    Require(!report.Passed, "performance budget accepted an overrun");
    Require(report.Failures.Count == 4, "performance budget did not report every overrun");
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
