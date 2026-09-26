# Expedition world architecture

## Decision

Abyssal Ecologies will pursue **COA2: logically unbounded expedition sectors** on the main development line. The original Subnautica map remains a finite, unchanged home world. A deterministic gateway will eventually transfer the player into generated expedition sectors that can continue across sector coordinates without extending vanilla terrain or requiring a seamless global-origin shift.

**COA3: seamless infinite terrain with origin rebasing** is deferred to a later experimental Git branch. COA3 work must not share save data with the supported COA2 implementation and must not be merged until vehicle, base, physics, AI, story, and third-party-mod compatibility risks are independently resolved.

## COA2 invariants

- The vanilla world, story sites, terrain, and save coordinates remain unchanged.
- Expedition output is a pure function of world seed, sector coordinate, chunk coordinate, and generator version.
- A sector contains an 8 by 8 grid of 256-metre chunks by default.
- Only a bounded 3 by 3 or 5 by 5 chunk window may be active around the player.
- Shared terrain vertices are sampled from global integer coordinates so adjacent chunks produce byte-identical seam heights.
- Unmodified chunks are regenerated rather than serialized in full.
- Save data records the seed, generator version, visited sectors, discoveries, resource removals, placed objects, and other player-caused deltas.
- Wrecks, notes, logs, local histories, and anomalous discoveries are generated from versioned authored narrative grammars; a sector's physical evidence and documents must share one deterministic event history.
- Sector transitions keep Unity coordinates bounded. A sector is logically adjacent to its neighbors even when the implementation uses a controlled transfer rather than a continuous global coordinate.
- The supported traversal model is a deterministic network of discoverable arches or equivalent gateways. Every transition has a guaranteed return edge, while frontier gates can reveal new logical sectors indefinitely.
- Player construction in expedition space remains disabled until stable object identity, delta persistence, vehicle transfer, death/respawn, and recovery are proven.
- The persistent Abyssal Expedition Carrier is the supported mobile home, gateway-transition anchor, Seamoth/Prawn transport, living-specimen archive, navigation room, and long-form investigation space. Its full state must transfer atomically.
- Every runtime experiment begins disabled, is confined to the disposable test save, and has an explicit cleanup or rollback path.

## Deterministic address model

The game-independent core owns these addresses:

- `ExpeditionSectorCoordinate(x, z)` identifies a logical sector.
- `ExpeditionChunkCoordinate(x, z)` identifies a chunk in the global expedition grid.
- `ExpeditionGenerator` derives terrain and content seeds for each chunk.
- `SampleVertexHeight(seed, x, z)` derives shared corner samples from integer grid coordinates.
- `GenerateStreamingWindow` returns a bounded, unique neighborhood without creating runtime objects.

The initial biome grammar selects Glass Kelp Garden, Ember Trench, or Ghostlight Nursery for each chunk. Later revisions will use climate fields, depth bands, adjacency weights, transition chunks, rarity, and progression distance rather than independent uniform selection.

## Runtime sequence

1. Enter the expedition through an explicit gateway from the home world.
2. Resolve the destination sector and local chunk from saved expedition state.
3. Generate descriptors for the bounded streaming window.
4. Create pooled additive terrain meshes, colliders, biome dressing, fauna, and locations of interest.
5. Preload the next row of chunks before a local boundary is crossed.
6. Record player-caused deltas before unloading distant chunks.
7. At a sector boundary, perform a controlled coordinate-safe transition and rebuild the local window.
8. Return through the gateway without changing the vanilla home-world coordinates.

## Gateway topology and bounded physical staging

The master expedition seed does not describe one finite physical map. It addresses a logical grid or graph of sector records. Sector `(0,0)` is the first expedition destination. Discoverable arches expose neighboring sector addresses; a north/east/south/west coordinate model is preferred initially because the opposite coordinate provides an unambiguous return link. Later special gates may create rare long-distance or depth-tier connections while still storing a deterministic reverse edge.

The original game's world boundary limits simultaneous Unity coordinates, not the number of logical sectors. AE therefore separates **logical expedition coordinates** from **physical runtime coordinates**:

- one reserved, bounded expedition staging volume is used for the active sector;
- the active sector's local chunks are generated around that stable physical anchor;
- entering an arch records the current sector deltas and local return gateway;
- a transition effect or loading interval hides controlled unloading and pooling;
- the destination sector is generated into the same bounded staging volume;
- the player and supported vehicle are placed at the destination arrival arch;
- returning through that arch regenerates the previous sector from its seed plus saved deltas.

This permits continued exploration without moving farther through the vanilla coordinate space. The player keeps equipment, discoveries, scans, story progress, and supported vehicles; the landscape is replaced only during explicit gateway transitions.

Every ordinary sector must provide:

- one guaranteed arrival/return arch;
- at least one reachable frontier arch until the intended expedition boundary policy says otherwise;
- stable edge identities so save/reload cannot reroll destinations;
- a safe arrival volume, navigable route away from the arch, and recovery destination;
- a deterministic local history, ecology, cave network, and location set.

Previously visited sectors are not kept alive. They are regenerated from the master seed, generator version, and sector address, then patched with compact saved deltas. The save maintains discovered sector addresses, gateway links, current logical sector/local position, scan and story state, depleted or altered objects, and eventually validated player construction.

The current core uses 32-bit coordinates and derived seeds for the initial offline foundation, which already permits an enormous practical address space. Before the expedition save schema is frozen, sector identifiers and seed-channel hashes will be upgraded to stable 64-bit or wider values to make accidental collisions negligible and leave room for long-running saves.

Initial runtime support will transfer the player only. The expedition carrier follows as a purpose-built persistent vehicle/base; Seamoth and Prawn transfer occurs through its validated docking berths. Cyclops transfer is not planned because the carrier fills that role. Dropped-object persistence, death/respawn, carrier recovery, and player construction are separate acceptance gates. Construction that could straddle logical sectors remains prohibited.

Carrier dimensions, docking envelopes, and safe arrival requirements must be fixed before cave, gateway, and location grammars are considered stable. See [EXPEDITION_CARRIER_AND_PROGRESSION.md](EXPEDITION_CARRIER_AND_PROGRESSION.md).

## Delivery slices

1. Deterministic address, seed, seam, and streaming-window core checks. *(implemented)*
2. One temporary additive seabed chunk created and removed by diagnostic commands.
3. Seam-safe neighboring terrain chunks with colliders.
4. Bounded 3 by 3 streaming and pooling around a test anchor.
5. Biome grammar and transition chunks using the existing three biome families.
6. Flora, fauna, landmark, and performance budgets per active chunk.
7. Versioned expedition manifest and compact per-chunk delta persistence.
8. Bidirectional arch network, bounded staging-sector transition, vehicle transfer, death/respawn, and recovery.
9. Deterministic modular locations of interest with navigation guarantees.
10. Long-distance, save/restart, memory, recovery, and compatibility endurance tests.

## Narrative archaeology

Expedition sectors carry discoverable histories rather than isolated random props. A narrative seed selects an authored incident grammar containing a group, purpose, sequence of events, failure or mystery, surviving evidence, and possible unresolved thread. The location assembler then expresses that same history through geometry and placement:

- modular wreck hulls, damaged equipment, abandoned camps, survey platforms, vehicles, cargo fields, and emergency shelters;
- ordered PDA notes, personal logs, research records, maps, warnings, maintenance entries, and incomplete transmissions;
- environmental evidence such as impact trails, breached compartments, scattered supplies, unusual growth, missing personnel, defensive damage, or deliberately sealed rooms;
- deterministic clue chains that can span several nearby chunks or sectors;
- rare anomalies and inexplicable phenomena that remain internally consistent without blocking required progression.

Text is selected and parameterized from reviewed authored fragments, not generated as unconstrained random prose at runtime. Names, dates, roles, locations, causes, and clue order come from one incident record so documents cannot contradict the physical scene. Critical rewards and required conclusions use deterministic guarantees; optional interpretations and weirdness may remain ambiguous.

The novelty director tracks incident families, wreck silhouettes, document voices, anomaly types, and resolution patterns so adjacent sectors do not repeat the same story with different colors.

## First runtime gate

Version 0.12.0's diagnostic-only single-chunk spike passed field testing on 2026-09-26. It generated one original 256-metre collidable seabed mesh at a controlled below-map staging anchor, reported 625 vertices, 1,152 triangles, an estimated 33.0 KiB mesh, and +32.0 KiB managed memory, then removed its runtime root, collider, mesh, and material on command. A save/full restart returned inactive without altering the schema-3 home-world manifest. The deep staging pocket required a flashlight, so later diagnostic slices provide temporary inspection lighting without treating it as production biome lighting.
