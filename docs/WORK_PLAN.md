# Abyssal Ecologies work plan

Last updated: 2026-09-25

## Objective

Deliver a save-compatible mod for the original Subnautica (2018), on its current non-Legacy desktop branch, that creates deterministic, procedurally arranged undersea micro-biomes containing original animals, environmental assemblies, and locations of interest without overwriting vanilla terrain or blocking story progression. This project does not target Subnautica 2 or Subnautica: Below Zero.

## Verified baseline

The repository currently provides:

- a stable PCG-based layout generator shared by the plugin and test executable;
- three region archetypes with 41 coordinated placements each;
- nine Nautilus prefab variants using base-game models as placeholders;
- configurable seed, region count, radial bounds, depth bounds, and separation;
- a late-load lifecycle boundary that performs no layout generation at the main menu;
- a schema-3 per-save manifest containing the seed, placement mode, regions, placements, and content identifiers, with explicit migration from schemas 1 and 2;
- bounded successful-instance logs and three generated field-check teleport destinations;
- a packaging script that produces an install-ready BepInEx ZIP;
- a guarded Windows launcher for a copied test installation; and
- automated checks for determinism, seed variation, placement counts, exclusion from the starting radius, depth bounds, and inter-region separation.

The solution builds with zero warnings. The checks cover 100 seeds, static protected areas, deterministic replacement searches, coordinate-preserving schema migration, byte-stable schema-3 round trips, incompatible-schema rejection, world diagnostics, performance-budget pass/fail behavior, and guarded manifest regeneration. Version 0.5.1 passed its migration/save/restart field test with the same seed and all 123 placements. Version 0.6.0 passed its bounded late-setup, registration, managed-memory, placement-count, instantiation-callback, live-object, unload, and full-restart field test. Version 0.7.0's boundaries passed, but its sidecar backup was discarded when Subnautica promoted TempSave. Version 0.7.1 passed the corrected durable-backup, required-save, restart activation, validation, performance, and regenerated-content field checks. Terrain-aware placement remains unresolved because both forced remote batch-loading paths destabilized Subnautica's late load phase.

## Active milestone: terrain-aware save manifests

This is the critical path. Original art and expanded content remain blocked until generated layouts can be placed, persisted, inspected, and migrated safely.

| Priority | Work item | Deliverable | Acceptance evidence |
| --- | --- | --- | --- |
| Done | Save lifecycle hook | Generation starts only after a save slot and world are ready | Supported Nautilus late-load task; main-menu log reports definitions only |
| Deferred | Terrain probing | Candidate flora and landmarks snap to valid surfaces; swimming fauna remain in water volumes | Experimental resolver retained; both forced remote batch-loading paths failed live testing and are disabled |
| Partial | Exclusion volumes | Reject Aurora, lifepods, wrecks, precursor sites, void, map edge, and player structures | Static exclusions are active and the representative layout passed inspection; runtime terrain/object rejection is deferred |
| Done | Per-save manifest | Persist seed, schema version, regions, placements, and content identifiers | Fixture and round-trip checks plus in-game save/quit/relaunch/reload passed |
| Done | Developer commands | Print manifest, teleport to region, report bounds, and validate placements | Shared-core checks and the 0.4.0 in-game field test passed on 2026-09-25 |
| Done | Migration framework | Refuse incompatible manifests and migrate explicitly supported schemas | Offline fixtures pass; schema 1 to schema 3 save/restart field migration passed without coordinate drift on 2026-09-25 |
| Done | Performance budget | Stream regions without persistent whole-map objects | `ae_perf` passed load, AE1/AE2 streaming, unload, and full-restart checks on 2026-09-25; 123/123 placements remained registered |
| Done | Disposable regeneration and boundaries | Visualize region extents and safely stage a new deterministic layout | 0.7.1 passed refusal/no-write, temporary visualization, exact durable backup, save promotion, restart activation, validation, performance, and regenerated-content checks on 2026-09-25 |

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

Complete the isolated uninstall/recovery acceptance test. Back up the seed-451232 save, quit fully, quarantine only the Abyssal Ecologies plugin binaries, confirm the vanilla save remains loadable with generated objects absent, then restore the exact 0.7.1 binaries and confirm the same schema-3 manifest, seed, placements, and generated content return unchanged.
