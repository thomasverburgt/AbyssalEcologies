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
- Sector transitions keep Unity coordinates bounded. A sector is logically adjacent to its neighbors even when the implementation uses a controlled transfer rather than a continuous global coordinate.
- Player construction in expedition space remains disabled until stable object identity, delta persistence, vehicle transfer, death/respawn, and recovery are proven.
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

## Delivery slices

1. Deterministic address, seed, seam, and streaming-window core checks. *(implemented)*
2. One temporary additive seabed chunk created and removed by diagnostic commands.
3. Seam-safe neighboring terrain chunks with colliders.
4. Bounded 3 by 3 streaming and pooling around a test anchor.
5. Biome grammar and transition chunks using the existing three biome families.
6. Flora, fauna, landmark, and performance budgets per active chunk.
7. Versioned expedition manifest and compact per-chunk delta persistence.
8. Gateway, sector transition, vehicle transfer, death/respawn, and recovery.
9. Deterministic modular locations of interest with navigation guarantees.
10. Long-distance, save/restart, memory, recovery, and compatibility endurance tests.

## First runtime gate

The next implementation slice is a diagnostic-only single-chunk spike. It must generate one original collidable seabed mesh at a controlled test anchor, report its vertex/triangle/memory counts, and remove it completely on command. It must not alter the schema-3 home-world manifest, register permanent coordinated spawns, write expedition save data, or remain after a process restart.
