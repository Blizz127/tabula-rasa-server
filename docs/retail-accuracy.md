# Retail accuracy work log

## Target and baseline — 2026-09-12

The user's clarified target is **1:1 preservation of the final live game before
shutdown**, covering the full game and its final content. `AGENTS.md` records this
as the repository's governing requirement. Client **1.16.5.0** is the version
required by `docs/setup.md`; that compatibility requirement still needs comparison
with an original final client build/manifest and final live patch history.
The broad goal remains incomplete: working login and a populated item database do
not establish retail gameplay parity.

Earlier changes below are implementation progress, not certified final-retail
equivalence. They must be rechecked against the clarified preservation standard.
No custom rates, balance changes, replacement quest content, or convenience rules
are part of the requested end state. Unknown behavior must stay visible as a gap
until evidence supports a faithful implementation.

[Final retail evidence](final-retail-target.md) now preserves original official
D16.4/D16.5 announcements and the final event/farewell messages, with capture
dates and hashes. D16.5 is positively identified as live on 17 February 2009;
the exact final executable revision and later server-only changes remain open.
Final-patch mech access and unusual end-of-service rewards must be preserved
when evidenced, even where they differ from earlier retail rules.

Local HEAD and the GitHub default branch both resolve to
`2a3e4bb8f9f153ebf64805cbd420f855f850c78b` (2023-12-27). The existing Compose
network/port overrides and persistent databases predate this work.

## Sources and confidence

- [Rasa.NET](https://github.com/InfiniteRasa/Rasa.NET): authoritative for this
  implementation, not proof of retail behavior. The README explicitly describes
  an incomplete server. Open issues cover abilities, attributes, XP, missions,
  cloning, AI, control points, squads, and travel.
- [Older C++ server, experimental branch](https://github.com/InfiniteRasa/Game-Server/tree/experimental):
  another implementation to compare. Its `src/manifestation.cpp` validates the
  available budget before allocating attributes and sends updated allocation
  points afterward. It uses **two** attribute points per level, unlike this C#
  implementation and the reference below. Do not copy formulas indiscriminately.
- [TaRapedia: Leveling Up](https://tabularasa.fandom.com/wiki/Leveling_Up): indexed
  text states the general award is three attribute points and two skill points.
  It also describes trainer advancement at levels 5/15/30. Full page retrieval
  was blocked; treat the indexed text as corroboration, not a complete versioned
  specification. No trainer gate has been added based only on that excerpt.
- [TaRapedia: Level](https://tabularasa.fandom.com/wiki/Level): indexed text says
  level 50 awards four additional training points beyond the normal two.
- [TaRapedia: Attributes window](https://tabularasa.fandom.com/wiki/Attributes_window):
  indexed text describes a purchased respec token. Negative allocation requests
  are not an appropriate substitute for a respec system.
- [Upstream first-map crash report](https://github.com/InfiniteRasa/Rasa.NET/issues/45):
  a reproducible historical report, not proof that the current observed session
  encountered that crash.

## Implemented in this pass

- Attribute requests must be nonnegative and fit the remaining earned budget.
  Validate all three values before mutation. Wide arithmetic prevents overflow
  from turning large requests into apparently affordable allocations.
- Invalid legacy negative/overspent allocations expose zero spendable points;
  this does not modify existing characters or attempt an unsupported respec.
- Refresh the client's remaining allocation points after allocation or rejection.
- Level-up messages report the change in available points for that level instead
  of reporting the entire accumulated unspent balance as newly earned points.
  Existing skill-point awards, including milestone bonuses, are preserved.

Regression tests cover earned budgets at levels 1/2/5/15/30/50, exact spending,
repeated requests, negative values, overspending, integer overflow, empty requests,
and invalid legacy allocations. These do not prove rendered client behavior.

## Observed content and next work

Read-only SQLite inventory on 2026-09-12:

| Table | Records |
| --- | ---: |
| map_info | 78 |
| creature | 140 |
| creature_stat | 92 |
| spawnpool | 218 |
| npc_package | 2 |
| npc_mission | 2 |
| npc_mission_reward | 0 |
| itemtemplate | 4,985 |
| itemtemplate_armor | 5 |
| itemtemplate_weapon | 2,440 |
| vendor | 20 |
| vendor_item | 109 |
| logos | 166 |
| teleporter | 581 |

Counts show content coverage gaps, not how many records retail should contain.
`MissionManager` currently loads definitions; a complete quest lifecycle still
needs investigation. Priorities for continued work:

1. Complete skill prerequisites: batch validation and class ancestry are now
   implemented (see below), but exact rank-level/Logos prerequisites and signature
   caps still need versioned evidence.
2. Audit stat formulas against client data and patch-era references; validate
   allocations and level-up display in a real client, including reconnect.
3. Continue melee investigation: the shared recovery route and false hit lists
   are corrected (see below), but formulas, timing, and on-hit effects remain.
4. Build mission progression and rewards from identified retail quests, including
   NPC relationships, prerequisites, objective state, persistence, and rewards.
   Do not mass-import guessed quests or treat the older SQL as verified retail.
5. Inspect armor templates, equipment requirements, XP thresholds, trainer
   advancement, cloning, loot, AI, and control-point behavior with separate
   evidence and tests for each implemented mechanic.

## Validation commands

Build an isolated candidate before replacing a live image:

```sh
docker build -t rasa_net:retail-candidate .
docker run --rm --network none rasa_net:retail-candidate dotnet test src/Rasa.Test/Rasa.Test.csproj --no-build --no-restore
```

Tests run without production database mounts. Keep live-client validation and
remaining fidelity work explicit; passing unit tests is not retail certification.

2026-09-12 validation: Docker build succeeded with zero errors and five existing
unused-variable/field warnings. All 26 tests passed (zero failures/skips).
The tested candidate image was promoted to `rasa_net:latest` and the game service
was recreated; auth was left running. Candidate image ID:
`sha256:f0a147ae9d143c4c57ba8ff7e1b7690b2a81b4992d16eaf862dadc18b6f4ad1f`.

Pre-deployment SQLite backups passed `PRAGMA integrity_check` and are stored in
`/home/blizz/backups/rasa-net/20260912T170541Z-retail-allocation`.
Previous image retained as `rasa_net:before-retail-allocation-20260912`.
For code rollback, retag that image as `rasa_net:latest` and recreate only the
game service with `docker compose up -d --no-deps --no-build game`. No schema
migration or character-data rewrite was introduced by this patch.

Post-deployment logs confirm authentication to the auth server, listening on
port 8102, loading world data, and `Server ready!` at 17:05:53 UTC. The running
game container's image ID matches the tested candidate. In-client allocation,
level-up display, and reconnect persistence still require gameplay verification.

## Skill training and attack recovery — subsequent 2026-09-12 pass

Source details: [skill research](skill-research.md) and
[mission research](mission-research.md). These distinguish implementation evidence
from client-verified retail facts and retain conflicting/obsolete source notes.

Changes:

- Training validates the complete batch before modifying player skills: known
  IDs, one entry per skill, matching arrays, rank bounds, no learned-rank
  downgrades, and enough points for all intervening ranks. Rejections resynchronize
  current skills and points rather than throwing for invalid requests.
- Firearms enum corrected to ID 1, matching both existing wire tables and the
  older C++ definition. Live `character_skills` was empty at inspection; no ID 2
  data migration was performed.
- Class ancestry checks use the pinned C++ catalog's 73 IDs and 15 classes,
  corroborated by retail class descriptions and patch-era changes. Characters
  retain ancestor skills and cannot buy from unrelated branches. Class membership
  has medium retail confidence pending client-data comparison; exact per-rank
  level requirements, signature caps, and Logos prerequisites remain incomplete.
- The whole training batch is saved with one EF transaction before live ranks
  change. A failed save rolls back the batch, leaves player state intact, logs
  the failure, and resynchronizes skills/points. SQL schema is unchanged.
- `SkillsPacket` now owns a snapshot. Constructing another player's packet, or
  changing a rank before the send queue drains, cannot replace its data.
- Recovery re-resolves the original target at impact. Untargeted shots and
  removed/dead/replaced actors produce empty hit lists, without false damage
  entries or missing-target dictionary exceptions. Existing live-target damage
  is preserved.
- `WeaponMelee` action 174 explicitly uses weapon recovery, as in
  [the pinned C++ recovery implementation](https://github.com/InfiniteRasa/Game-Server/blob/4a9ab5f1fcdf6a18ab6911c384189cc41ddae651/src/missile.cpp#L299).
  That source also labels melee a placeholder; this does not establish correct
  retail melee formulas, animation timing, or effects.

New tests exercise class inheritance/rejection, rank costs, malformed/duplicate
requests, no partial mutations, skill-packet serialization across players,
SQLite batch persistence across a fresh context, injected-save rollback, empty
attack packets, target disappearance/death/reuse, and ordinary target damage.

Mission research found the two current seeds have incorrect NPC relationships,
missing objectives/rewards, and storage/serialization defects. River Recon has a
concrete older-project reference, but importing it directly would use mismatched
script opcodes and unverified rewards. It remains the next mission implementation
candidate; no live quest rows were changed in this pass.

Validation and deployment: final Docker build completed with zero errors and the
same five unused-variable/field warnings. All **60 tests passed**, zero failed or
skipped, in the isolated container (no live database mounts). This includes the
previous 26 tests plus 34 training, persistence, and missile recovery cases.

The deployed game container matches tested image
`sha256:583ae914f43a367c1a11570163f7cee95bacc283506f79baf117264c878e98f4`.
Pre-deployment SQLite backups passed integrity checks; backups and build/test logs
are in `/home/blizz/backups/rasa-net/20260912T171705Z-retail-training`.
Code rollback image: `rasa_net:before-retail-training-20260912` (retag as
`rasa_net:latest`, then recreate only game with `--no-deps --no-build`).
Live-client training, effects, combat animations, and reconnect UI remain
unverified; the database reload test verifies storage, not the real client's UI.
Startup logs confirm auth connection, world loading, and `Server ready!` at
17:17:16 UTC after this deployment.
