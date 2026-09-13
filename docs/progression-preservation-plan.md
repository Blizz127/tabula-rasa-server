# New character to endgame preservation order

The user's 2026-09-12 instruction establishes this implementation order for the
final live Tabula Rasa preservation target. This is an ordered work record;
none of the segments below is currently certified as complete or 1:1.

| Order | Playable segment | Required behavior |
| --- | --- | --- |
| 1 — current | Creation and first login | Family/character names, allowed appearance/races, initial inventory and equipment, training/point state, starting location, first-login and boot-camp options, persistence |
| 2 | Final live boot camp | Original mission/NPC sequence, tutorial events, combat and equipment use, Logos acquisition, rewards, failure/retry behavior, exit and supported skip path |
| 3 | Wilderness and first class advancement | Early missions and encounters, loot/economy, leveling and training, level-5 class choice, clone workflow, travel and instance progression |
| 4 | Tier-two progression to level 15 | Both branches, original missions/regions/instances, inherited and new abilities, level-15 advancement and cloning |
| 5 | Tier-three progression to level 30 | All four branches and their content, progression dependencies, level-30 advancement, signatures and cloning |
| 6 | Tier-four progression to level 50 | All eight final classes, original late leveling content, gear, crafting, groups and required regional/system progression |
| 7 | Endgame and shutdown live state | Final instances, encounters, equipment and rewards, control points/PvP/social systems, final deployed content and documented final events |

Where original server data is permanently lost, the user's 2026-09-13
decision applies: reconstruct from footage, the original map and client data,
labelling every estimated value with its provenance tier, source and
uncertainty (see `AGENTS.md`). The boot camp is the first segment built this way.

Within each segment, recover original data and observable behavior, implement
the required server flow, and verify creation/interaction/reconnect and failure
paths. Shared systems belong to the earliest segment that needs them. Working
unit tests establish software behavior; original artifacts and gameplay
comparison establish fidelity. Do not skip unresolved early progression by
substituting invented rewards, unrestricted gear or debug advancement.

Use static analysis of the recovered versioned client, archived official
material, contemporary direct gameplay footage/guides, and other emulator
source. Record source versions, exact locations or video timestamps, confidence
and conflicts. Keep acquired client/video artifacts and private character data
outside Git. An older guide or emulator's default is not by itself proof of
the final-live rule.

## Current creation findings

- Dated post-rewrite evidence establishes all five Recruit skills at rank 1
  with zero initial unspent points. Creation now persists these ranks together
  with the character and items in one transaction, with failure rollback.
- The starter pistol now receives durability from its own template.
- The first-family six-field creation message now has a handler, with the
  original `None` family state and persisted admission/replay checks.
- Fixed Recruit outfit colors match the original creation preview. Selection
  now checks the original two-field message, rejects overflowing slot values,
  and preserves session selection when ownership lookup or saving fails.
- The full initial inventory/placement, race and appearance eligibility,
  starting point, and first-login/skip state remain under reconstruction.
- Deployment 11 rebuilt boot camp. The archived Bootcamp page marks its own
  mission list obsolete; recover the later sequence before importing missions.
- The recovered later client identifies missions 1990, 1992, 1994, 1995 and
  the retry 2005, 21 objectives and ten NPC dialogue bindings. The current
  boot-camp map has no spawn-pool rows or mission definitions; start/reward/skip
  scripts remain unrecovered. See [the boot-camp audit](bootcamp-client-evidence.md).
- A verified 2026-09-13 sweep established that the original static map holds
  no gameplay actors at all, so first-login position, NPC placements and
  objects must come from captures or observation, not client data.
- The server now speaks the recovered mission-log and NPC objective
  conversation protocol with persistent per-character progress, which the
  tutorial's conversation objectives require. No tutorial content is loaded;
  incomplete definitions are withheld. See
  [mission research](mission-research.md#mission-log-protocol-and-persistence--2026-09-13).

Detailed evidence: [new-character initialization](new-character-client-evidence.md),
[starter equipment](starter-equipment-research.md),
[character progression](character-progression-client-evidence.md), and
[retail accuracy](retail-accuracy.md).
