# Roadmap

## Milestone 0 — deterministic vertical slice (implemented)

Prove a stable seed, separated region placement, prefab registration, world streaming, and a repeatable test harness. Placeholder content is acceptable at this stage.

Exit gate: compile succeeds, generator checks pass, and each configured seed registers the expected number of objects.

## Milestone 1 — terrain-aware placement

- Delay final placement until a save is loaded.
- Probe terrain and water with downward raycasts.
- Reject Aurora, precursor-base, lifepod, wreck, void, and player-base exclusion volumes.
- Persist a generated-world manifest per save slot so later configuration changes cannot silently relocate content.
- Add a developer command that prints, regenerates, and visualizes the manifest in a disposable test save.

Exit gate: at least 100 seed runs produce no floating flora, buried landmarks, blocked story entrances, or placements inside existing bases.

## Milestone 2 — original animals

- Replace the three clone placeholders with licensed original meshes, textures, rigs, animations, sounds, icons, and eggs.
- Give each species a distinct ecological role and bounded AI state machine.
- Add scanner entries, encyclopedia text, animation/audio budgets, containment behavior, and breeding rules.
- Profile schools and large creatures independently.

Exit gate: each species has original assets and behavior, scans correctly, survives save/load, and stays inside its performance budget.

## Milestone 3 — biome identity

- Replace placeholder plants and landmarks with modular original asset bundles.
- Add local fog, particles, soundscapes, lighting, resource loops, food-web relationships, and biome discovery messages.
- Use additive geometry and volumes; do not attempt runtime mutation of Subnautica's baked terrain unless a proven toolchain supports it.

Exit gate: regions are visually and mechanically distinct, readable from approach, and do not overwrite vanilla biome metadata globally.

## Milestone 4 — locations of interest

- Add procedural research camps, natural arches, fossil beds, and small story caches assembled from validated modules.
- Generate navigable layouts with guaranteed entrances and scanner/loot reachability.
- Separate cosmetic variety from progression-critical content; critical rewards use deterministic guarantees rather than random chance.

Exit gate: every generated location has a reachable entrance, a safe return path, and deterministic progression rewards.

## Milestone 5 — compatibility and release

- Freeze a manifest schema and add migrations before public saves exist.
- Test common world, map, creature, and performance mods.
- Package exact dependencies and supported game build information.
- Publish only after clean-install, upgrade, uninstall, and save-recovery tests pass.

