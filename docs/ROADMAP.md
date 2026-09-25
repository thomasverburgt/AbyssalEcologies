# Roadmap

This document describes the product sequence. See [WORK_PLAN.md](WORK_PLAN.md) for current priorities, deliverables, and acceptance evidence.

## Milestone 0 — deterministic vertical slice (implemented)

Prove a stable seed, separated region placement, prefab registration, world streaming, and a repeatable test harness. Placeholder content is acceptable at this stage.

Exit gate: compile succeeds, generator checks pass, and each configured seed registers the expected number of objects.

## Milestone 1 — terrain-aware placement

- Delay final placement until a save is loaded. *(implemented in 0.2.0)*
- Probe terrain and water with downward raycasts. *(experimental resolver retained, but remote batch streaming was disabled in 0.3.2 after live-load failures)*
- Reject Aurora, precursor-base, lifepod, wreck, void, and player-base exclusion volumes. *(static exclusions active in 0.3.2; live terrain/object checks deferred with the resolver)*
- Persist a generated-world manifest per save slot so later configuration changes cannot silently relocate content. *(canonical schema 3, schema 1/2 migration, and legacy exclusion-policy preservation implemented and field-validated in 0.5.1)*
- Add developer diagnostics that print the manifest, report region bounds, validate placements, and expose field-check teleports. *(implemented and field-validated in 0.4.0)*
- Add bounded late-load and streaming performance instrumentation. *(implemented and field-validated in 0.6.0 on 2026-09-25, including unload and full-restart checks)*
- Add explicitly guarded manifest regeneration and temporary in-world boundary visualization for disposable saves. *(implemented and field-validated in 0.7.1 on 2026-09-25; 0.7.0's non-durable TempSave backup was rejected and corrected)*
- Ground landmarks with bounded local raycasts only after their cells naturally stream. *(implemented in 0.8.0 after all three seed-58 landmarks floated; field validation pending, with remote batch loading still disabled)*

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
- Support a deliberate permanent-uninstall path; 0.7.1 validates temporary removal and exact restoration only, with missing-prefab errors and a strict no-save limitation while absent.
- Publish only after clean-install, upgrade, uninstall, and save-recovery tests pass.
