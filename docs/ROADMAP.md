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
- Ground landmarks with bounded local raycasts only after their cells naturally stream. *(0.8.4 level foundations passed AE1/AE3; 0.8.9's duplicate-guarded AE2 seamount passed unload, revisit, no-duplicate, performance, and fresh-process single-recreation validation on 2026-09-26; remote batch loading remains disabled)*

Exit gate: at least 100 seed runs produce no floating flora, buried landmarks, blocked story entrances, or placements inside existing bases.

## Milestone 2 — original animals

- Replace the three clone placeholders with licensed original meshes, textures, rigs, animations, sounds, icons, and eggs. *(Glassfin functionally validated in 0.9.0; Cinder Ray functionally validated in 0.10.0; Lantern Skate functionally validated in 0.11.0; all visuals remain prototype-grade rather than final art)*
- Give each species a distinct ecological role and bounded AI state machine. *(Glassfin low-speed filter-feeding display implemented without overriding its temporary proven locomotion shell)*
- Add scanner entries, encyclopedia text, animation/audio budgets, containment behavior, and breeding rules. *(Glassfin scanner, databank entry, spatial call, distinct egg, persistence, and performance passed the 0.9.0 functional field test; containment hatching remains pending)*
- Profile schools and large creatures independently.

Exit gate: each species has original assets and behavior, scans correctly, survives save/load, and stays inside its performance budget.

## Milestone 3 — logically unbounded expedition sectors

- Preserve the finite vanilla world as the unchanged home map and enter generated space through an explicit gateway.
- Generate deterministic sector and chunk addresses from a per-save expedition seed.
- Stream and pool only a bounded local chunk window.
- Generate additive seam-safe terrain, colliders, biome dressing, fauna, and locations from chunk descriptors.
- Persist generator version, discoveries, removed resources, placed objects, and other player deltas rather than complete generated chunks.
- Use discoverable bidirectional arches and a reusable bounded physical staging sector to keep Unity coordinates bounded while logical sector coordinates continue outward.
- Keep seamless origin-rebased COA3 work isolated on a later experimental branch.

Exit gate: sustained multi-sector travel, return trips, save/restart, vehicle transfer, death/respawn, bounded memory, deterministic regeneration, and recovery all pass without changing the vanilla home world.

## Milestone 4 — expedition carrier and progression

- Build a persistent sector-portable carrier with a compact base interior, Seamoth/Prawn docking, charging, storage, fabrication, Modification Station, power, navigation, recovery, and shipborne Alien Containment.
- Reward a living archive of discovered regional organisms, viable traits, ecological sets, breeding, welfare, and responsible release.
- Reward sector discovery, route stabilization, biome diversity, cave mapping, and completed surveys through carrier and navigation progression.
- Deliver a very long authored wreck mystery whose deterministic clue graph is distributed across procedural sectors without random soft locks.
- Make carrier, docked-craft, containment, inventory, power, damage, discovery, and investigation state transactional across gateways and save/restart.

Exit gate: the carrier and all three progression arcs survive repeated gateway travel, failure recovery, docked-vehicle transfer, specimen persistence, clue recovery, save interruption, and long-distance play without loss or duplication.

## Milestone 5 — biome identity

- Replace placeholder plants and landmarks with modular original asset bundles.
- Add local fog, particles, soundscapes, lighting, resource loops, food-web relationships, and biome discovery messages.
- Use additive geometry and volumes; do not attempt runtime mutation of Subnautica's baked terrain unless a proven toolchain supports it.

Exit gate: regions are visually and mechanically distinct, readable from approach, and do not overwrite vanilla biome metadata globally.

## Milestone 6 — locations of interest

- Add procedural wrecks, research camps, survey sites, natural arches, fossil beds, and story caches assembled from validated modules.
- Generate navigable layouts with guaranteed entrances and scanner/loot reachability.
- Generate coherent incident histories expressed through linked PDA notes, logs, physical evidence, environmental storytelling, and deterministic clue chains.
- Add rare internally consistent anomalies and weird discoveries that reward exploration without gating required progression.
- Separate cosmetic variety from progression-critical content; critical rewards use deterministic guarantees rather than random chance.

Exit gate: every generated location has a reachable entrance, a safe return path, internally consistent evidence and documents, and deterministic progression rewards.

## Milestone 7 — compatibility and release

- Freeze a manifest schema and add migrations before public saves exist.
- Test common world, map, creature, and performance mods.
- Package exact dependencies and supported game build information.
- Support a deliberate permanent-uninstall path; 0.7.1 validates temporary removal and exact restoration only, with missing-prefab errors and a strict no-save limitation while absent.
- Publish only after clean-install, upgrade, uninstall, and save-recovery tests pass.
