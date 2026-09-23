# Abyssal Ecologies work plan

Last updated: 2026-09-22

## Objective

Deliver a save-compatible mod for the original Subnautica (2018), on its current non-Legacy desktop branch, that creates deterministic, procedurally arranged undersea micro-biomes containing original animals, environmental assemblies, and locations of interest without overwriting vanilla terrain or blocking story progression. This project does not target Subnautica 2 or Subnautica: Below Zero.

## Verified baseline

The repository currently provides:

- a stable PCG-based layout generator shared by the plugin and test executable;
- three region archetypes with 41 coordinated placements each;
- nine Nautilus prefab variants using base-game models as placeholders;
- configurable seed, region count, radial bounds, depth bounds, and separation;
- a late-load lifecycle boundary that performs no layout generation at the main menu;
- a schema-1 per-save manifest containing the seed, regions, placements, and content identifiers;
- bounded successful-instance logs and three generated field-check teleport destinations;
- a packaging script that produces an install-ready BepInEx ZIP;
- a guarded Windows launcher for a copied test installation; and
- automated checks for determinism, seed variation, placement counts, exclusion from the starting radius, depth bounds, and inter-region separation.

The solution builds with zero warnings. The checks cover 100 seeds, schema-1 fixture loading, byte-stable manifest round trips, configuration isolation, and rejection of corrupt or future schemas. In-game testing confirmed flora and fauna in all three regions and preserved the same layout through save, full exit, relaunch, and reload. The invalid drillable source used by the Thermal Spire was replaced in 0.2.1 with the already proven Membrain Tree source; that visual correction still needs its focused `goto ae3` check.

## Active milestone: terrain-aware save manifests

This is the critical path. Original art and expanded content remain blocked until generated layouts can be placed, persisted, inspected, and migrated safely.

| Priority | Work item | Deliverable | Acceptance evidence |
| --- | --- | --- | --- |
| Done | Save lifecycle hook | Generation starts only after a save slot and world are ready | Supported Nautilus late-load task; main-menu log reports definitions only |
| P0 | Terrain probing | Candidate flora and landmarks snap to valid surfaces; swimming fauna remain in water volumes | Seed test report contains no buried, airborne, or above-water placements |
| P0 | Exclusion volumes | Reject Aurora, lifepods, wrecks, precursor sites, void, map edge, and player structures | Automated rejection tests plus an in-game inspection checklist |
| Done | Per-save manifest | Persist seed, schema version, regions, placements, and content identifiers | Fixture and round-trip checks plus in-game save/quit/relaunch/reload passed |
| P1 | Developer commands | Print manifest, teleport to region, show bounds, and validate placements | Commands work in a disposable save and produce bounded diagnostic output |
| P1 | Migration framework | Refuse incompatible manifests and migrate explicitly supported schemas | Fixture tests cover current, previous, corrupt, and future schema versions |
| P1 | Performance budget | Stream regions without persistent whole-map objects | Profiling captures frame time, allocations, object count, and unload behavior |

### Milestone acceptance gate

Milestone 1 is complete only when all of the following are true:

1. One hundred generated seeds pass offline invariants.
2. A representative seed set is inspected in a fresh test save at shallow, middle, and deep ranges.
3. No inspected landmark blocks vanilla progression or intersects a protected site.
4. Save, quit, reload, and revisit preserve the same manifest and objects.
5. Changing global configuration does not silently relocate an existing save's generated content.
6. Removing the mod leaves the vanilla save loadable, with limitations documented before public testing.

## Subsequent milestones

### Original fauna

Replace placeholder clones with licensed original meshes, rigs, textures, animations, sounds, icons, eggs, scan entries, and bounded AI. Each animal needs an ecological role, containment behavior, spawn budget, and save/load test.

### Biome identity

Replace placeholder plants and landmarks with modular original assets. Add local lighting, particles, fog, sound, resource relationships, discovery feedback, and recognizable approach silhouettes without globally rewriting vanilla biome metadata.

### Locations of interest

Assemble research camps, fossil beds, natural arches, and small story caches from validated modules. Every layout must guarantee an entrance, return path, reachable interactions, and deterministic progression rewards.

### Compatibility and release

Freeze the manifest schema, test clean installation and upgrade paths, document supported game and dependency versions, test uninstall recovery, and run compatibility checks against common map, world, creature, and performance mods.

## Validation workflow

For every implementation increment:

1. Run `dotnet build .\AbyssalEcologies.slnx -c Release`.
2. Run `dotnet run --project .\tools\AbyssalEcologies.GeneratorChecks -c Release --no-build`.
3. Run `git diff --check` and inspect the exact staged file list.
4. Package with `.\scripts\package.ps1`.
5. Install only into the isolated test copy.
6. Use a disposable save and retain the relevant BepInEx log, manifest, seed, and observed result.

Passing automated checks or reaching the main menu is not sufficient evidence of in-save correctness.

## Scope controls

- Do not modify the Steam-library installation during development.
- Do not test prototypes against the only copy of an important save.
- Do not change a save's generation seed after its manifest has been created.
- Do not represent recolored vanilla clones as finished original animals.
- Do not add progression-critical random rewards without deterministic guarantees.
- Do not mutate Subnautica's baked terrain at runtime unless a proven and recoverable toolchain is established.

## Immediate next increment

Implement terrain probing and protected-site rejection against the loaded world. Flora and landmarks must snap to valid surfaces, swimming fauna must remain in water with usable clearance, and rejected candidates must be deterministically replaced without changing an existing schema-1 manifest. The increment should also complete the in-game save/quit/reload check for the lifecycle and manifest work above.
