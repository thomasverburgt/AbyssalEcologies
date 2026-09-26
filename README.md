# Abyssal Ecologies

Abyssal Ecologies is an experimental mod for the original **Subnautica (2018)** that adds deterministic, procedurally arranged **micro-biomes** to the base game's world. It is not for Subnautica 2 or Subnautica: Below Zero. A generation seed selects region positions, species variants, environmental clusters, and a central landmark. The same seed always produces the same layout.

The current `0.10.0` vertical slice continues the original-fauna milestone. Glassfin is functionally validated, and Cinder Ray now has a code-generated original manta model, articulated wing/tail rig, thermal-pulse animation, generated icon, synthesized call, PDA scan entry, and distinct hatchable egg. Their locomotion and persistence still use proven vanilla gameplay shells while the creature pipeline is field-tested; Lantern Skate remains a recolored placeholder clone. The mod does **not** modify Subnautica's terrain mesh or biome lookup table.

## Project status

**Prototype — use disposable saves.** Content definitions register at startup, but layout generation waits until a save has loaded. Version 0.10.0 retains the validated schema-3, regeneration, performance, grounding, and Glassfin behavior while adding the Cinder Ray prototype. It never requests remote terrain batches or modifies Subnautica's baked terrain. Existing schema-1 deterministic and schema-2 terrain-resolved manifests migrate without moving saved coordinates; exclusions introduced after a legacy layout was created remain compatibility warnings.

Version 0.3.2 passed its representative in-game regression on 2026-09-24: initial load, all three regions, all flora/fauna/landmarks, the Thermal Spire, save, full restart, reload, and revisit.

Version 0.4.0 passed its diagnostic field test on 2026-09-25: manifest reporting returned three regions and 123 placements, deterministic/static-exclusion validation returned zero errors, the `ae1` teleport instantiated its landmark/flora/fauna, and the bounds command confirmed the player inside the region.

Version 0.5.1 passed its migration field test on 2026-09-25: the schema-1 save migrated to schema 3 without seed, region, or placement drift; save/restart loaded canonically without a second migration; and 26 post-layout Lifepod 12 exclusion findings remained bounded legacy warnings rather than errors.

Version 0.6.0 passed its performance field test on 2026-09-25. Initial load measured 41.1 ms late setup, 3.5 ms spawn registration, and +1.25 MiB managed memory with all 123 placements registered. Streaming AE1 and AE2 increased callbacks from 41 to 82 while live objects fell from 41 to 38, demonstrating unload behavior. A full restart measured 15.2 ms late setup, 3.0 ms registration, and +0.91 MiB with 123/123 placements.

Version 0.7.1 passed its regeneration and boundary field test on 2026-09-25. Temporary rings and center markers appeared and were removed on command; incorrect confirmation left the manifest byte-identical; regeneration created an exact durable backup outside Subnautica's TempSave cache; and save, quit, restart activated seed 451232 with schema 3, 123 placements, zero validation errors, a passing performance budget, and all three AE1 content groups. Version 0.7.0 was not accepted because its TempSave sidecar backup was discarded during save promotion.

Version 0.7.1 also passed a temporary-removal recovery test on 2026-09-25. The backed-up save loaded with the plugin DLLs quarantined and the custom content absent, then returned byte-identically with all AE1 content after the exact DLLs were restored. The mod-absent load emitted missing-prefab errors for saved custom instances, so **do not save while Abyssal Ecologies is missing**. Permanent uninstall is not yet supported or validated.

The first representative seed-58 inspection on 2026-09-25 covered shallow (208 m), middle (343 m), and deep (459 m) regions. Flora attachment, fauna, collision avoidance, and escape paths passed, but all three central landmarks floated above terrain. Version 0.8.4's level foundations passed visual and 9/9 terrain-hit inspection for AE1 and AE3. AE2 remained in a large terrainless void through 0.8.7; a second western candidate also had no seabed within 400 metres. Version 0.8.8 created a bounded collidable seamount, grounded the arch at 9/9 with 0.10 metres of relief, and passed its performance budget, but destroyed the landform when the central landmark cell unloaded while 16 outer-region objects remained. Version 0.8.9 passed the corrected lifecycle test on 2026-09-26: its single session-resident seamount remained while AE2 unloaded, no duplicate appeared on revisit, and a fresh process recreated exactly one landform. The Glass Arch retained 9/9 support hits, 0.10 metres of terrain relief, and a maximum measured support slope of 1.7 degrees.

Version 0.9.0 passed its Glassfin functional field test on 2026-09-26. The original procedural creature and distinct hatchable egg registered and appeared in game; the creature animated, scanned into the databank, and persisted through save/full restart together with the egg and unlocked entry. The restarted session reported six active Glassfins, the 1,177-vertex five-bone model, the procedural egg shell, and a passing performance budget at 18.0 ms setup, 6.0 ms registration, +0.86 MiB, and 123/123 placements. The creature and egg are intentionally prototype-grade and were judged visibly strange and unpolished; this is accepted for pipeline validation, not final-art approval.

Current priorities and acceptance evidence are maintained in [docs/WORK_PLAN.md](docs/WORK_PLAN.md). The longer product sequence is in [docs/ROADMAP.md](docs/ROADMAP.md).

## What is implemented

- Three generated region archetypes: Glass Kelp Garden, Ember Trench, and Ghostlight Nursery.
- Two original procedural fauna prototypes, one placeholder animal species, three flora variants, and three central landmarks.
- Glassfin PDA scanning, encyclopedia entry, icon, synthesized spatial call, hatchable egg, and a low-speed filter-feeding display that does not override the proven locomotion shell.
- Seeded PCG random generation that is stable across .NET and Unity versions.
- Configurable region count, world radius, depth range, and separation.
- A canonical schema-3 per-save manifest loaded before the layout is registered with Nautilus coordinated spawns.
- Explicit, coordinate-preserving migration from schema 1 deterministic and schema 2 terrain-resolved manifests, with rejection of unsupported or contradictory inputs.
- A deterministic replacement-search implementation reserved for future terrain-resolved generation; the experimental runtime resolver is not active in 0.5.1.
- Active static protection around the Aurora, major precursor/Degasi sites, and lifepods. Runtime terrain and nearby-object rejection remains deferred with the experimental resolver.
- Bounded success logs, `goto ae1`/`ae2`/`ae3` field-check destinations, and `ae_manifest`, `ae_bounds`, and `ae_validate` diagnostics.
- An `ae_perf` diagnostic covering late-load setup time, coordinated-spawn registration time, managed-memory change, registered placements, cumulative instantiation callbacks, and currently live custom objects.
- Delayed local landmark anchoring with a nine-point level foundation, plus a bounded additive Glass Kelp seamount and Prism Kelp surface grounding when the generated region occupies open void; no remote cells are loaded or baked terrain modified.
- Temporary non-colliding region rings and center markers through `ae_boundaries [10-300 seconds]` and `ae_boundaries_off`.
- Restart-only disposable-save regeneration through `ae_regenerate NEW_SEED CONFIRM_DISPOSABLE_SAVE_REGENERATION`, with a timestamped byte-for-byte backup of the prior manifest under `BepInEx/config/AbyssalEcologies/manifest-backups`.
- A game-independent generator check executable.

## Requirements

- The original Subnautica (2018), using its current desktop branch rather than the Legacy branch.
- Tobey's BepInEx Pack for Subnautica.
- Nautilus.
- .NET SDK for building. The plugin targets .NET Framework 4.7.2, matching the official Subnautica mod template.

The project follows the current official Nautilus setup: <https://github.com/SubnauticaModding/Nautilus/blob/master/Nautilus/Documentation/guides/dev-setup.md>.

## Build and verify

```powershell
dotnet restore .\AbyssalEcologies.slnx
dotnet build .\AbyssalEcologies.slnx -c Release --no-restore
dotnet run --project .\tools\AbyssalEcologies.GeneratorChecks -c Release
```

The compiled mod is `src\AbyssalEcologies.Plugin\bin\Release\net472\AbyssalEcologies.dll`. Copy it and `AbyssalEcologies.Core.dll` into a dedicated folder under `Subnautica\BepInEx\plugins\AbyssalEcologies`.

To build, test, and create an install-ready ZIP in one step:

```powershell
.\scripts\package.ps1
```

## Configure and test safely

On first launch, BepInEx creates `BepInEx\config\rocks.verburgt.subnautica.abyssalecologies.cfg`. A new save captures those generation settings and deterministic positions in `AbyssalEcologies\AbyssalEcologies.json` beneath its save-slot directory. After that, the saved manifest is authoritative and changing the global seed affects only saves that do not yet have a manifest.

For the 0.6.0 performance test, load the validated schema-3 disposable save and run `ae_perf`. The initial report must pass the 250 ms late-setup, 100 ms spawn-registration, 16 MiB managed-memory, and exact-placement-count budgets, with 123 registered placements and nine content types. Run `goto ae1`, wait for the region to stream, and run `ae_perf` again; repeat with `goto ae2`. The later reports show cumulative instantiation callbacks and currently live objects so unloading behavior can be inspected without treating normal streaming variation as a hard failure.

For the 0.7.1 field test, run `ae_boundaries 90` and inspect the bright radius ring and vertical center marker at a field-check region; then run `ae_boundaries_off`. A missing or incorrect regeneration confirmation must be refused without writing files. On a disposable save only, `ae_regenerate NEW_SEED CONFIRM_DISPOSABLE_SAVE_REGENERATION` writes a timestamped durable backup under the BepInEx configuration directory and stages the replacement in Subnautica's live save cache. Save the game, fully quit, and restart; then `ae_manifest` must report the new seed and `ae_validate` must pass.

Version 0.8.9 passed its lifecycle test by running `goto ae2`, waiting for streaming, and requiring `ae_grounding` to report the Glass Arch `GROUNDED` with 9/9 support hits. The seamount remained while the Glass Arch unloaded, no duplicate appeared after returning to AE2, the Prism Kelp and arch remained seated, and `ae_perf` passed before and after the trip. A full process restart recreated exactly one seamount. The fresh-process report measured 18.3 ms late setup, 6.1 ms spawn registration, +0.89 MiB managed memory, and 123/123 registered placements.

For the 0.9.0 Glassfin field test, load the disposable seed-58 save and run `goto ae2`. After the Glass Kelp Garden streams, require `ae_fauna` to report `enabled=True`, `registered=yes`, and at least one active Glassfin. Visually confirm the cyan original body, forked tail, paired side fins, and two facial filter fans; observe tail/fin motion and the wider fan display when a fish slows. Scan one Glassfin and confirm its **Glassfin** databank entry. Use an Alien Containment Unit only if convenient to verify that the generated egg is accepted and hatches. Run `ae_perf`, save, fully restart, return to AE2, and confirm the model, scan state, movement, and performance remain intact. Set `Fauna.UseOriginalGlassfinPrototype=false` only to exercise the Peeper-derived rollback visual.

That procedure passed for the model, scanner/databank, egg creation, save/restart persistence, and performance. Containment hatching time and final visual polish remain outside the accepted 0.9.0 functional gate.

For the 0.10.0 Cinder Ray field test, run `goto ae1`, wait 12 seconds, and run `ae_fauna`. Require the Cinder Ray line to report `enabled=True`, `registered=yes`, and at least one active instance. Confirm a broad dark-red manta silhouette, independently flapping wings, long articulated tail, and pulsing ember material; scan one and confirm the **Cinder Ray** databank entry. Run `item AbyssalEcologies_cinder_ray_egg` and inspect the faceted glowing egg, then run `ae_perf`. Save/restart persistence remains the acceptance gate.

Do not use this prototype on the only copy of an important save. The 0.3.2 layout passed representative in-game inspection, but terrain-aware placement and permanent uninstall remain incomplete.

Temporary removal is recoverable only with care: fully quit, back up the save, quarantine the two Abyssal Ecologies DLLs, and expect missing-prefab errors while the save is loaded without the mod. Do not save in that state. Quit and restore the exact plugin DLLs before continuing; version 0.7.1 restored the unchanged manifest and generated content in the isolated field test. Permanent uninstall remains unsupported.

Restart Subnautica before switching to a different save slot. Nautilus coordinated-spawn registrations are process-wide; version 0.10.0 safely refuses to mix a second manifest into the active session and stages regeneration only for the next process.

## Isolated Windows test launcher

`scripts/Launch-Subnautica-Test.ps1` is a guarded launcher for a copied test installation. Place the script beside the test copy's `Subnautica.exe`, keep a `steam_appid.txt` containing `264710` in that same directory, and invoke the script directly or through a shortcut.

The launcher starts Steam silently only as the platform service, then launches the test-directory executable directly with `-vrmode none -no-stereo-rendering`. It never uses `steam://run/264710`, refuses to start while another `Subnautica.exe` is running, and keeps the test directory as the working directory so the Steam-library executable is not selected.

## Development roadmap

See the [active work plan](docs/WORK_PLAN.md) for the next implementation tasks and the [roadmap](docs/ROADMAP.md) for the staged path from this vertical slice to original creatures, richer biome identity, discoveries, and save-compatible releases.
