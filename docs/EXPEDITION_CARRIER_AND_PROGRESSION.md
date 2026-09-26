# Expedition carrier and progression arcs

## Product decision

Abyssal Ecologies requires a player-owned **Abyssal Expedition Carrier** *(working title)*: a sector-portable vehicle and persistent mobile base built for long-duration exploration beyond the vanilla home world. It is not an optional cosmetic endgame vehicle. Its identity, state, docked craft, inhabitants, storage, upgrades, and story progress must survive every supported gateway transition and save/restart cycle.

The carrier replaces the need to bring a Cyclops into expedition space. It must eventually transport a docked Seamoth and Prawn Suit while providing the essential functions of a compact base.

## Required carrier capabilities

- pilotable local-sector movement with bounded speed, collision, crush-depth, power, damage, grounding, and recovery behavior;
- atomic transfer through supported expedition gateways;
- one Seamoth berth and one Prawn Suit berth, with safe docking, charging, persistence, and destination placement;
- battery and power-cell charging;
- fabricator, Modification Station, storage, lockers, medical support, and core survival services;
- modular power generation, capacity, efficiency, pressure, navigation, scanner, defensive, and recovery upgrades;
- one or more shipborne Alien Containment habitats with observable living specimens;
- persistent interior inventory, installed modules, power state, damage state, contained life, docked craft, customization, and player position;
- an expedition navigation table showing discovered sectors, gateway links, unresolved signals, wreck clues, and return routes;
- an emergency home-world return and transactional recovery state if a destination sector cannot load safely.

The carrier is one persistent object logically, even if its exterior, interior, and docked craft require separate runtime representations. A gateway transition must serialize and validate the complete carrier state before unloading the current sector. A failed transition restores the carrier, occupants, inventory, and docked craft at the departure arch without duplication or loss.

## Delivery constraints

- The first carrier implementation may use a controlled hybrid interior/exterior architecture rather than a fully physical Cyclops clone.
- Carrier dimensions and gateway clearances become inputs to terrain, cave, arrival-volume, and location generation.
- No gateway may require the carrier to traverse geometry that was validated only for a swimming player.
- Construction inside the carrier uses explicit supported sockets or modules until arbitrary interior building is proven safe.
- Contained organisms and docked craft do not run full simulation while their representation is unloaded; elapsed-time simulation must be deterministic and bounded.
- Power generation, charging, breeding, fabrication, and resource processing must not duplicate output across transitions or reloads.
- The carrier cannot be accepted until destruction/recovery, death/respawn, save interruption, dock failure, blocked arrival, and low-power transition cases have explicit behavior.

## Progression arc 1: living expedition archive

Players are rewarded for finding regional species and maintaining living examples in the carrier's containment habitats. Scanning records knowledge; safely collecting, transporting, housing, and sustaining a living specimen records biological research.

The living archive tracks:

- species and regional morphology;
- sex or breeding compatibility only where the creature grammar supports it;
- habitat, diet, temperature, pressure, light, and social requirements;
- health, welfare, breeding state, and lineage;
- rare traits, mutations, behaviors, and ecological relationships;
- whether a specimen was scanned, observed, hatched, bred, released, or currently housed.

Rewards come from biodiversity, complete ecological sets, rare viable traits, successful breeding, sustained welfare, and responsible release—not merely filling tanks with duplicates. Rewards may include research data, carrier biological modules, habitat improvements, medicines, food-production insights, environmental resistance, new scanner analysis, cosmetics, and clues that can only be derived from living organisms.

The system must prevent transition/reload duplication, avoid permanently losing unique progression when a specimen dies, and distinguish a historical catalogue record from a currently living collection.

## Progression arc 2: sector discovery and cartography

Every safely entered sector, stabilized return route, mapped cave system, catalogued biome, discovered gateway, and surveyed location contributes to expedition cartography.

Discovery rewards are milestone-based and deterministic. They may unlock:

- deeper or more hazardous expedition tiers;
- additional gateway interpretation and long-distance route planning;
- carrier range, pressure, power, scanner, storage, docking, and recovery upgrades;
- improved sector previews and hazard forecasts;
- coordinates for unusual sectors, anomalies, or major wreck sites;
- visual trophies, map layers, titles, and carrier customization.

Raw sector count alone is insufficient. Rewards should value diversity, completed surveys, safe return paths, rare conditions, and resolved locations so repeatedly crossing trivial sectors is not optimal.

## Progression arc 3: the long wreck mystery

Alongside self-contained local wreck incidents, AE contains one very long authored mystery distributed procedurally across the expedition network. Its canonical backbone, key revelations, and ending are authored; the seed determines where compatible evidence appears, how optional branches are arranged, and which local incidents carry parts of the larger trail.

The plot may span dozens or hundreds of sectors and uses:

- multiple wreck classes and expeditions separated by time;
- crew manifests, personal notes, research logs, maintenance records, black boxes, cargo evidence, maps, symbols, transmissions, and biological samples;
- physical contradictions and recurring anomalies that only become meaningful across several discoveries;
- fragments requiring comparison at the carrier's navigation/research table;
- optional interpretations and false leads that never invalidate required evidence;
- major puzzle locations unlocked by accumulated knowledge rather than one random drop.

Required clues use a deterministic clue graph with prerequisites, redundant recovery routes, pacing windows, and maximum-sector guarantees. A save cannot be soft-locked by missing a random wreck, exhausting a resource, taking a different gateway, or losing a note. The expedition log records originals, derived conclusions, unresolved questions, and the physical sectors where evidence was found.

## How the arcs reinforce one another

- Sector surveys reveal signals, wreck candidates, unusual ecologies, and frontier routes.
- Wreck research unlocks carrier systems and explains why some sectors, organisms, or gateways are unusual.
- Living specimens expose biological clues and adaptations that inert scans cannot provide.
- Carrier upgrades permit travel into sectors containing later evidence and more demanding habitats.
- The carrier serves as the physical museum, zoo, laboratory, map room, puzzle board, vehicle hangar, and safe return point for the entire expedition.

None of the three arcs may require blind random chance. Procedural placement controls discovery order and local expression; authored guarantees control progression availability.

## Staged implementation

1. Lock carrier identity, persistent state model, physical envelope, docking envelope, and gateway clearance contracts.
2. Prove a persistent carrier shell and interior across save/restart in a fixed test sector.
3. Add transactional gateway transfer without docked craft.
4. Add power, storage, fabricator, chargers, Modification Station, and recovery behavior.
5. Add Seamoth and Prawn docking, charging, transfer, and failure recovery.
6. Add one persistent containment habitat and a living-specimen record.
7. Add biological requirements, breeding, archive rewards, and additional habitats.
8. Add sector cartography, discovery milestones, and carrier navigation upgrades.
9. Add the deterministic long-mystery clue graph and carrier research interface.
10. Run destructive recovery, duplication, power-loss, blocked-arrival, death, save-interruption, long-distance, and performance tests.
