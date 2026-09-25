# Abyssal Ecologies

Abyssal Ecologies is an experimental mod for the original **Subnautica (2018)** that adds deterministic, procedurally arranged **micro-biomes** to the base game's world. It is not for Subnautica 2 or Subnautica: Below Zero. A generation seed selects region positions, species variants, environmental clusters, and a central landmark. The same seed always produces the same layout.

The current `0.5.0` vertical slice is intentionally asset-light: it clones, recolors, and rescales base-game prefabs to prove the world-generation and Nautilus registration pipeline. It does **not** modify Subnautica's terrain mesh or biome lookup table, and the placeholder species do not yet have unique models, sounds, eggs, scan entries, or AI.

## Project status

**Prototype — use disposable saves.** Content definitions register at startup, but layout generation waits until a save has loaded. Version 0.5.0 creates a canonical schema-3 manifest with an explicit deterministic placement mode and static protected-area exclusions. Existing schema-1 deterministic and schema-2 terrain-resolved manifests migrate to schema 3 without moving their saved coordinates. Live terrain snapping remains unresolved because both tested remote batch-loading paths destabilized Subnautica's late-load phase.

Version 0.3.2 passed its representative in-game regression on 2026-09-24: initial load, all three regions, all flora/fauna/landmarks, the Thermal Spire, save, full restart, reload, and revisit.

Version 0.4.0 passed its diagnostic field test on 2026-09-25: manifest reporting returned three regions and 123 placements, deterministic/static-exclusion validation returned zero errors, the `ae1` teleport instantiated its landmark/flora/fauna, and the bounds command confirmed the player inside the region.

Current priorities and acceptance evidence are maintained in [docs/WORK_PLAN.md](docs/WORK_PLAN.md). The longer product sequence is in [docs/ROADMAP.md](docs/ROADMAP.md).

## What is implemented

- Three generated region archetypes: Glass Kelp Garden, Ember Trench, and Ghostlight Nursery.
- Three placeholder animal species, three flora variants, and three central landmarks.
- Seeded PCG random generation that is stable across .NET and Unity versions.
- Configurable region count, world radius, depth range, and separation.
- A canonical schema-3 per-save manifest loaded before the layout is registered with Nautilus coordinated spawns.
- Explicit, coordinate-preserving migration from schema 1 deterministic and schema 2 terrain-resolved manifests, with rejection of unsupported or contradictory inputs.
- A deterministic replacement-search implementation reserved for future terrain-resolved generation; the experimental runtime resolver is not active in 0.5.0.
- Active static protection around the Aurora, major precursor/Degasi sites, and lifepods. Runtime terrain and nearby-object rejection remains deferred with the experimental resolver.
- Bounded success logs, `goto ae1`/`ae2`/`ae3` field-check destinations, and `ae_manifest`, `ae_bounds`, and `ae_validate` diagnostics.
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

For the 0.5.0 upgrade test, load the validated schema-1 disposable save. The log should report migration from schema 1 to schema 3 without changing its seed or coordinates. Run `ae_manifest` and confirm `schema=3`, `sourceSchema=1`, `migrated=True`, `seed=451230`, and `placementMode=deterministic`; then run `ae_validate`. Save, fully restart, reload the same slot, and confirm `sourceSchema=3`, `migrated=False`, and the same region coordinates.

Do not use this prototype on the only copy of an important save. The 0.3.2 layout passed representative in-game inspection, but terrain-aware placement and uninstall testing remain incomplete.

Restart Subnautica before switching to a different save slot. Nautilus coordinated-spawn registrations are process-wide; version 0.5.0 safely refuses to mix a second manifest into the active session, but it cannot replace the first layout without a restart.

## Isolated Windows test launcher

`scripts/Launch-Subnautica-Test.ps1` is a guarded launcher for a copied test installation. Place the script beside the test copy's `Subnautica.exe`, keep a `steam_appid.txt` containing `264710` in that same directory, and invoke the script directly or through a shortcut.

The launcher starts Steam silently only as the platform service, then launches the test-directory executable directly with `-vrmode none -no-stereo-rendering`. It never uses `steam://run/264710`, refuses to start while another `Subnautica.exe` is running, and keeps the test directory as the working directory so the Steam-library executable is not selected.

## Development roadmap

See the [active work plan](docs/WORK_PLAN.md) for the next implementation tasks and the [roadmap](docs/ROADMAP.md) for the staged path from this vertical slice to original creatures, richer biome identity, discoveries, and save-compatible releases.
