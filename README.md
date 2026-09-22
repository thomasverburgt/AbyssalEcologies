# Abyssal Ecologies

Abyssal Ecologies is an experimental mod for the original **Subnautica (2018)** that adds deterministic, procedurally arranged **micro-biomes** to the base game's world. It is not for Subnautica 2 or Subnautica: Below Zero. A generation seed selects region positions, species variants, environmental clusters, and a central landmark. The same seed always produces the same layout.

The current `0.1.0` vertical slice is intentionally asset-light: it clones, recolors, and rescales base-game prefabs to prove the world-generation and Nautilus registration pipeline. It does **not** yet modify Subnautica's terrain mesh or biome lookup table, and the placeholder species do not yet have unique models, sounds, eggs, scan entries, or AI. Those are the next content milestones after in-game placement validation.

## Project status

**Prototype — not save-safe for normal play.** The deterministic generator, Nautilus registration layer, packaging workflow, and isolated Windows test launcher are implemented. Automated checks pass, and the plugin reaches the Subnautica main menu. Terrain-aware placement and sustained in-save validation are the active engineering milestone.

Current priorities and acceptance evidence are maintained in [docs/WORK_PLAN.md](docs/WORK_PLAN.md). The longer product sequence is in [docs/ROADMAP.md](docs/ROADMAP.md).

## What is implemented

- Three generated region archetypes: Glass Kelp Garden, Ember Trench, and Ghostlight Nursery.
- Three placeholder animal species, three flora variants, and three central landmarks.
- Seeded PCG random generation that is stable across .NET and Unity versions.
- Configurable region count, world radius, depth range, and separation.
- Nautilus coordinated spawns, so generated objects participate in Subnautica's world streaming and save system.
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

On first launch, BepInEx creates `BepInEx\config\rocks.verburgt.subnautica.abyssalecologies.cfg`. Keep the seed unchanged after loading a save: coordinated spawn identifiers remain stable, but changing the seed moves their registered positions and can leave old-save state inconsistent.

Start with a new test save. After the main menu loads, inspect `BepInEx\LogOutput.log` for lines beginning with `Abyssal Ecologies`; they list each generated region center. Use those coordinates for targeted in-game validation.

Do not use this prototype on the only copy of an important save. The generated coordinates are deterministic, but the current vertical slice has not yet terrain-probed every placement in a live save.

## Isolated Windows test launcher

`scripts/Launch-Subnautica-Test.ps1` is a guarded launcher for a copied test installation. Place the script beside the test copy's `Subnautica.exe`, keep a `steam_appid.txt` containing `264710` in that same directory, and invoke the script directly or through a shortcut.

The launcher starts Steam silently only as the platform service, then launches the test-directory executable directly with `-vrmode none -no-stereo-rendering`. It never uses `steam://run/264710`, refuses to start while another `Subnautica.exe` is running, and keeps the test directory as the working directory so the Steam-library executable is not selected.

## Development roadmap

See the [active work plan](docs/WORK_PLAN.md) for the next implementation tasks and the [roadmap](docs/ROADMAP.md) for the staged path from this vertical slice to original creatures, richer biome identity, discoveries, and save-compatible releases.
