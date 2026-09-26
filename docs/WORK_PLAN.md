# Abyssal Ecologies work plan

Last updated: 2026-09-26

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

The solution builds with zero warnings. The checks cover 100 seeds, static protected areas, deterministic replacement searches, coordinate-preserving schema migration, byte-stable schema-3 round trips, incompatible-schema rejection, world diagnostics, performance-budget pass/fail behavior, and guarded manifest regeneration. Version 0.5.1 passed its migration/save/restart field test with the same seed and all 123 placements. Version 0.6.0 passed its bounded late-setup, registration, managed-memory, placement-count, instantiation-callback, live-object, unload, and full-restart field test. Version 0.7.1 passed durable regeneration, boundaries, validation, performance, regenerated content, and temporary-removal recovery. Seed 58 passed fauna, collision, and escape-path inspection but its landmarks floated. Version 0.8.4's level foundations passed AE1 and AE3 with 9/9 terrain hits. Version 0.8.8's AE2 seamount initially grounded the arch at 9/9 with 0.10 metres of relief and passed performance, but its landmark-owned lifetime destroyed the landform while 16 outer objects remained loaded. Version 0.8.9 passed unload, revisit, duplicate-prevention, and fresh-process recreation validation on 2026-09-26. Version 0.9.0 passed Glassfin model, animation, scanning, databank, egg creation, save/restart persistence, and performance validation on 2026-09-26; its prototype visuals remain explicitly unpolished.

## Validated platform milestone: terrain-aware save manifests

The representative seed-58 platform path is accepted for incremental content work. Its schema, regeneration, performance, grounding, and recovery behavior remain regression gates; the broader 100-seed in-game inspection remains a release gate rather than a blocker for the isolated original-fauna prototype.

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
| Done with limitation | Temporary removal and recovery | Prove a backed-up vanilla save can load without the mod and recover after restoration | Save loaded and content returned byte-identically after DLL restoration on 2026-09-25; mod-absent loads emit missing-prefab errors, must not be saved, and permanent uninstall remains unsupported |
| Done for representative seed 58 | Local landmark and void-region grounding | Anchor broad landmarks and provide bounded surface support for an intentionally retained void layout | 0.8.9 passed AE1/AE3 foundations plus AE2 unload, revisit, no-duplicate, performance, and fresh-process single-recreation tests on 2026-09-26 |

### Milestone acceptance gate

Milestone 1 is complete only when all of the following are true:

1. One hundred generated seeds pass offline invariants.
2. A representative seed set is inspected in a fresh test save at shallow, middle, and deep ranges.
3. No inspected landmark blocks vanilla progression or intersects a protected site.
4. Save, quit, reload, and revisit preserve the same manifest and objects.
5. Changing global configuration does not silently relocate an existing save's generated content.
6. Removing the mod leaves the vanilla save loadable, with limitations documented before public testing. *(Temporary removal passed on 2026-09-25; missing-prefab errors and the no-save restriction are documented. Permanent uninstall remains unsupported.)*

## Subsequent milestones

### Original fauna

Replace placeholder clones with licensed original meshes, rigs, textures, animations, sounds, icons, eggs, scan entries, and bounded AI. Each animal needs an ecological role, containment behavior, spawn budget, and save/load test.

Version 0.9.0 begins this milestone with Glassfin. Its mesh, articulated appendages, procedural skin/icon, feeding animation, and synthesized call are generated entirely by project-owned code. A Peeper-derived shell remains temporarily responsible for locomotion and save integration, and a configuration switch preserves the prior visual as a rollback. Functional field validation passed on 2026-09-26; final art direction and polish did not pass and remain future work.

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

Begin the Lantern Skate original-fauna slice without polishing or redesigning the accepted Glassfin and Cinder Ray prototypes. Replace the Jellyray-derived visible model with project-owned procedural geometry, an articulated rig, texture/icon, animation, sound, scanner/databank entry, and distinct egg while retaining the proven gameplay shell and an explicit rollback switch. Add a bounded ghostlight ecological display, then repeat AE3 streaming, performance, scan, egg, save/restart, and containment checks.
