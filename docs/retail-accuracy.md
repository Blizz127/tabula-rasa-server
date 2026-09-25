# Retail accuracy work log

## Target and baseline — 2026-09-12

The user's clarified target is **1:1 preservation of the final live game before
shutdown**, covering the full game and its final content. `AGENTS.md` records this
as the repository's governing requirement. Client **1.16.5.0** is the version
required by `docs/setup.md`. An acquired client executable now confirms that
embedded version, with client tables available for static inspection; see
[artifact provenance](client-artifacts.md). An independent official manifest
and exact final server configuration remain missing.
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

The initial local HEAD and GitHub default branch both resolved to
`2a3e4bb8f9f153ebf64805cbd420f855f850c78b` (2023-12-27). The existing Compose
network/port overrides and persistent databases predate this work.

## Content and combat, 2026-09-16

Five missions later in the Wilderness and the Divide were seeded from the client's own conversations, and the
resistance curve finally reached combat.

- **Seeded** (all objectives already conversation-bound in the client, rewards TaRapedia's recorded values), 36
  missions in total by the end of the session: 431, 442, 444, 549, 836, 427, 682 (Wilderness), 332, 347, 382, 796,
  1743 (Divide), nine Palisades missions (199100-199107 givers) and six Valverde ones (199200-199205).
  431 Distress On The River, 442 Quarantine, 444 Unity Among Men, 549 Failure to Launch, 836 Incoming!,
  427 Lurking In The Shadows (Proctor Fulgor, creature 76), 682 Childhood's End (Arioch Xanx, creature 77),
  332 Ammo Express, 347 Cleansing the Toxins: Part II, 382 Retrieval for Recon, 796 Behind Closed Doors,
  1743 Report to Liaison Noonan.
- **NPCs created** (18 in total): 199000-199003 Divide (Lt. Sebastian, Shaman Horea, Field Dr. Dawson, Receptive
  Liaison Brice), 199100-199107 Palisades, 199200-199205 Valverde. Name id
  from the client's `creaturenamelanguage` (original), level/zone//loc from TaRapedia (inferred, dated), appearance
  an analogue under OD-45. The pipeline is the answer to the 660 missions whose giver is not in the world seed.
- **Defect found and fixed**: `npc_mission_reward` carries experience and credits in the same `credits` column, which
  the first batch got wrong (the loader refused all five for "Experience reward amount 0 is not positive").
- **Combat**: the resistance conversion the client itself carries (`shared/damageresistance.pyo`, cross-checked against
  Deployment 14's published table on all eight points) was recovered, tested - and unused. The equipment pass now sums
  each worn item's resist list per damage type onto the player, and a landed hit is scaled by the target's resistance
  before armour absorbs it. The rounding the live server used stays a recorded parameter
  (GAP-D14-RESISTANCE-ROUNDING).
- **Provenance**: 125 manifest rows added across the day's slices plus the `community_db` source kind (TaRapedia, the
  Ellatha mission DB) and the W3 slice; two gaps opened for what the sources do not settle
  (GAP-W3-COUNTER-OBJECTIVES, GAP-W3-NPC-POSITION-COVERAGE) and one for the rounding above.
- **Not verified in-game**: none of the new missions, NPCs or the resistance change has been played yet; the audit
  that did catch something was the world position one, which flagged the two NPCs whose TaRapedia /loc has no navmesh
  under it.

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
   implemented (see below). Final-client evidence now establishes signature caps
   and their exclusion from ordinary purchases; exact grant and rank-level/Logos
   prerequisites still need reconstruction.
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
  initially had medium retail confidence pending client-data comparison. The
  subsequent artifact pass below confirms the complete catalog and signature
  caps; exact per-rank level requirements and Logos prerequisites remain incomplete.
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

## Original client evidence and corrective pass — 2026-09-12

The [acquired client](client-artifacts.md) has embedded executable version
1.16.5.0 and recoverable Python 2.4 client code and generated tables. Selected
members were validated against ZIP sizes/CRCs and hashed; static inspection did
not execute game code. Its community-upload provenance remains distinct from
an independently authenticated official final distribution.

Implemented corrections:

- [Skill evidence](final-client-skill-evidence.md) confirms all 73 skill IDs,
  their class ownership and ancestry. Eight signature skills have maximum rank
  one and no ordinary training controls. Ordinary purchases now reject these
  signature grants/increases and invalid higher ranks; their original grant
  mechanism remains missing. Existing point-award arithmetic was not changed.
- Tactical Evasion (skill 54) now advertises ability 10000005 after training,
  matching the generated requirement table and actual client action. This
  repairs its skill-to-ability mapping; server ability effects remain incomplete.
  The live skill table contained zero rows before deployment, so no saved
  ability-ID repair was required on this server.
- [Mission persistence](mission-research.md) now supports multiple missions per
  character and filters reads by account and character slot. Generated SQLite
  and MySQL migrations preserve existing rows and widen mission-category
  representation. The original client contains category 10000044.
- [Mission packets](final-client-mission-evidence.md) preserve change time,
  distinct X/Y/Z markers, nullable timers, and three-value generic counters.
  Signed compact integer encoding/decoding now handles negative values without
  corrupting packet structure, while retaining all bits of unsigned IDs.
- [Normal logout](death-retail-evidence.md) now advertises and enforces ten
  seconds with cancellation and a monotonic deadline. Immediate quit/socket
  disconnect retention is still missing. Death/recovery research now has
  original trauma constants and client protocol evidence, but recovery gameplay
  was not activated from incomplete trigger/health evidence.

The final combined Docker build succeeded with zero errors and the same five
pre-existing unused-variable/field warnings. **All 105 tests passed**, zero
failed or skipped, without network or production database mounts. Separate
isolated MySQL checks covered generated Char migration SQL and category
widening; their scope is recorded in the mission research document.

Tested and deployed image:
`sha256:e9a6eb5c6a13f36b53743c2c69130a1d43391a4f51585d981edccabe29f838ab`,
retained as `rasa_net:retail-mission-candidate`. Only the game service was
recreated. It authenticated to the existing auth service, loaded world data,
and reported `Server ready!` at **17:45:26 UTC**. The running image matches the
tested candidate; the auth container's image is unchanged.

Verified SQLite backups, build/test logs, and before/after metadata are in
`/home/blizz/backups/rasa-net/20260912T174516Z-retail-mission`.
All three backups passed integrity checks. Post-startup Char and World databases
also passed integrity checks, with unchanged non-migration table counts. The
new composite mission key and both SQLite migration records were confirmed;
existing mission categories remain 1 and 2.

Rollback image: `rasa_net:before-retail-mission-20260912`. This pass changes the
schema: an image-only rollback is insufficient once new data uses multiple
missions or wider categories. Stop game writes, preserve any subsequent data,
and use the consistent pre-upgrade Char/World backups when restoring the old
schema and image. Do not truncate categories or discard missions to force a
downgrade; do not restore the independently running auth database unnecessarily.

The full preservation goal remains active. Passing these checks establishes
the corrected implementation, not final-retail equivalence. Quest lifecycle,
death/recovery, complete ability effects, content, final events, and real-client
comparison still require substantial reconstruction and verification.

## Ability requests, NPC dialogue, and disconnect lifecycle — 2026-09-12

The [ability-use audit](ability-use-client-evidence.md) adds original-client
request shapes and action-failure signatures. All 73 catalog rows and the
complete skill/ability/Logos join passed independent raw-bytecode comparison;
the resulting 53 active C# ability requirements matched with zero differences.
The server now checks learned rank, class ancestry, signature caps, and all
required Logos before queueing a skill ability. Lower learned ranks remain
usable and crouching remains allowed. Non-skill actions require their own
authorization paths; item/mech/polymorph support is still incomplete.

Ability requests now preserve optional entity/location/None targets, full
64-bit entity/item identifiers, and optional yaw. Rejected skill requests send
the client's supported `UserActionFailed` tuple with no guessed localized
message. Recognized ability effects, resource costs, cooldowns, targeting,
interruptions, and Lightning/Sprint placeholder formulas still require work.

[Original mission tables](river-recon-client-evidence.md) establish River Recon
429's objective text IDs, dialogue keys and narrative sequence, plus mission
321's Machina objective/counter label. They also prove that local Rogers was
assigned the dying patrol member's conversation package. The fresh seed now
uses package 116. Paired generated data migrations correct only the known
`npc_package` row 100/value 726 combination, retaining other values and NPCs.
No reward, prerequisite, objective trigger, NPC position, or mission assignment
was guessed from client text. The migration's `Down` intentionally does not
restore a known bad package or overwrite a row that was already correct.

The [disconnect lifecycle](death-retail-evidence.md) now retains an actor after
socket closure when a server-processed logout request still has time remaining.
The original deadline continues, world combat remains active, and world removal
and the existing character snapshot run at the end. Repeated callbacks cannot
repeat cleanup; closed connections stop accepting input/output, retained actors
continue occupying their accounts, and character replacement is gated to the
selection state. Loading/normal-logout races have isolated regression coverage.
Loss without a pending logout performs intended emulator cleanup; its exact
retail grace period and reconnect policy remain unknown. Health, death, and
active-effect persistence are still absent from the existing character snapshot.

[Character progression research](character-progression-client-evidence.md)
records the original trainer/class-selection and clone requests and the 5/15/30
tier text. The server-supplied training eligibility and reward totals are not
contained in those UI messages. Signature grants, precise point accounting,
and complete trainer/clone transactions remain open, with no invented numeric
replacement introduced.

Validation: the final Docker build completed with zero errors and five existing
unused-variable/field warnings. All **135 tests passed**, with zero failures or
skips, in an isolated container with no network or production database mounts.
This includes 16 new ability/protocol cases, seven disconnect cases, and seven
NPC package cases. The generated package-correction SQL also passed isolated
MySQL fixture checks; detailed logs and scope are linked in the mission record.

Tested/deployed image:
`sha256:39c7ffc84dd58fc269771d29fa27e4c41a4c8373fc516619590ab57f0a5f3fd3`,
retained as `rasa_net:retail-ability-candidate`. Only game was recreated. Startup
confirmed authentication to auth, world loading, and `Server ready!` at
**18:08:28 UTC**. The running image matches the tested candidate and auth's
image is unchanged.

Fresh SQLite backups, candidate build/test logs, focused records and before/after
deployment metadata are in
`/home/blizz/backups/rasa-net/20260912T180817Z-retail-ability`.
All three backups passed integrity checks. Post-startup Char/World checks also
passed. Non-migration table counts were unchanged; Rogers's package is now 116,
Witherspoon remains 208, and the expected World migration record was added.

Rollback image: `rasa_net:before-retail-ability-20260912`. The package correction
is compatible with that previous application, so code rollback can retain the
corrected package. If reverting data is specifically necessary, use the verified
pre-deployment World backup; the data migration's `Down` is intentionally empty.
Do not restore independently active auth/character data unnecessarily.

Operational observation: the previous game container restarted three times
around **18:00 UTC**, before this candidate was deployed. Its retained stdout
contains **three `Out of memory.` lines**, each preceding a restart's startup
sequence, in
`/home/blizz/backups/rasa-net/20260912T180817Z-retail-ability/rasa-game-before-ability.log`
(also retained at `/tmp/rasa-game-before-ability.log`). No stack trace identifies
which allocation or code path failed. The earlier search omitted that phrase;
the logs must not be described as containing no failure evidence. Bounded
historical Docker-event and accessible kernel-journal queries supplied no
additional cause. The replacement remained at restart count zero in a read-only
follow-up around **18:13 UTC**, with approximately 315 MiB container memory and
zero `oom` / `oom_kill` events in its current cgroup. Those replacement-container
observations do not explain the prior process failures. Keep their allocation
source open for investigation; do not infer a kernel OOM kill or a framing
attack from these logs alone.
Neither unit tests nor successful startup prove sustained availability or
original-client gameplay. Full preservation remains active and incomplete.

## Sprint, Lightning base damage, and malformed frames — 2026-09-12

[Sprint reconstruction](sprint-client-evidence.md) joins the final client's
literal action properties, original effect consumers and official live notes.
The five ranks now use movement multipliers **1.2/1.3/1.4/1.5/1.6**, with
activation costs and two-second drain amounts **30/27/25/20/18 CHI**. The
previous experimental short duration and speed formula are removed. Effect
updates account for every map-loop delta. Sprint attachment carries the
original consumer's required scalar argument, and the original right-click
effect-cancel request now removes the actor's own Sprint. Duplicate and
unaffordable activations do not add another effect or spend resources.

Normal adrenaline capacity is **1000**, inferred from all eight original
signature descriptions specifying 100% adrenaline and their corresponding
1000-CHI action costs, independently corroborated by Sprint's percentage
conversion. It no longer uses an unrelated Power/stat formula. Remaining
adrenaline gain/decay, starting-resource rules, modifiers, precise first-tick
phase and repeated-activation toggle behavior are explicitly unverified.
The client duration fields are retained as long internal caps while the
authored open-ended presentation has no countdown; final server cap behavior
still requires direct evidence.

[Lightning base damage](lightning-client-evidence.md) now follows the selected
rank and experience-level scaling. The fixed 233–311 sample is replaced by
original base bounds 180–240 at rank 1 and 240–300 at ranks 2–5, scaled by
`int(base * 2 ** ((level - 1) / 8.0))`. This completes only the base-range
correction. Arcs, Sonic damage, stun/storm effects, costs, timing, targeting,
and the full damage/modifier pipeline remain required combat work.

[Network runtime evidence](runtime-network-evidence.md) records an isolated
reproduction: an oversized four-byte length header caused the previous socket
callback to terminate its process with `Out of memory.`. Frames outside the
existing receive-buffer bounds now close their connection with resources
returned. Word lengths are unsigned, coalesced/fragmented frames are preserved,
and failed decryption is rejected. Protocol decoding now isolates malformed
messages inside their declared frame and handles them at the client boundary.
Declared decompression/field lengths no longer cause eager unchecked allocation.
Independent review also corrected endpoint access after socket disposal and
ownership transfer before synchronous receive continuation.

These tests establish a crash path and its correction, not the cause of the
three historical restarts. Full malformed-client handling, sustained runtime
observation and original-client session validation remain distinct work.

The [continued mission audit](river-recon-client-evidence.md) retained
conflicting historical River Recon rewards and the original live 1.4 notes
documenting 855 replaced mission rewards. Capture date does not establish the
data's revision. No conflicting quest amounts or items were imported.

The combined .NET 5 candidate built with zero errors and the same five existing
unused-variable/field warnings. **All 191 tests passed**, zero failed/skipped.
An independent source comparison between the candidate image and the reviewed
workspace found no differences (excluding generated `bin`/`obj` directories).

The game service was recreated from tested image
`sha256:f798038e3a0e3a514295bf2afc388cce8b0229f091f2cfb8ece861f8fb4a17c6`
at **18:33:06 UTC** and reported `Server ready!` at **18:33:16 UTC**. Its first
post-deployment check was running with zero restarts and no unhandled/OOM
startup lines. Auth's image and start time are unchanged. No new schema/data
migration is included in this pass.

All three SQLite backups passed integrity checks. Backups, private deployment
configuration, reviewed source, build/test logs, source comparison, retained
old/startup logs and before/after metadata are in
`/home/blizz/backups/rasa-net/20260912T183251Z-retail-combat/`.
Rollback image: `rasa_net:before-retail-combat-20260912`. Retag it as
`rasa_net:latest` and recreate only game with `--no-deps --no-build`; this code
rollback needs no database restoration. The broader preservation goal remains
active, including the explicit fidelity limitations in the linked reports.

## Action lifecycle and death notifications — 2026-09-12

The [original action lifecycle](action-lifecycle-client-evidence.md) establishes
Lightning's **500 ms windup, 700 ms recovery and 1200 ms subsequent reuse**.
The same action ID shares reuse across all ranks: normally the actor remains
busy until 1200 ms from starting and can cast Lightning again at 2400 ms.
The server now implements these stages with monotonic deadlines, charges the
original rank costs **25/50/75/100/150 Power** at successful recovery, and
revalidates the actor, learned rank/Logos, available Power and original target
identity before resolving. Spending at recovery is an explicit ordering
inference; original-server resource transaction boundaries remain unverified.

Rejected predictions receive both current-action cancellation and unresolved
request cleanup, with actual remaining reuse time to correct a late client
prediction. An identical request for an already accepted current pair is
ignored so the original client's first-pending-request removal does not discard
the accepted cast. Matching interrupts cancel unfinished casts without applying
damage or spending Power; interruption after resolution retains reuse.
Movement does not interrupt Lightning, matching its original class flags.

Cancelled legacy object/weapon queue entries no longer perform successful
recovery early. Object cancellation explicitly releases the corresponding
pending user while retaining unrelated objects' users. Main-loop elapsed time
now uses a monotonic clock, preserving the existing cadence while preventing
calendar-clock changes from altering durations. Full legacy reload/action
interactions still require integration with the original interruption flags.

The same audit found a preceding [Sprint gap](sprint-client-evidence.md): effect
attachment alone did not remove the original client's unresolved action.
Successful Sprint now sends its inherited self-target recovery acknowledgement
after attachment. Effect announcement is deferred to that recovery so it is
announced once. Exact historical unused hit-data encoding still needs a capture.

The [Lightning effect audit](lightning-effects-client-evidence.md) preserves
original optional arc/Sonic/stun/storm properties and adds typed immutable
damage/arc/storm packet data. Lightning recovery now serializes each actual
hit's amount, flags and effect lists. It does not yet select arc victims or
apply those extra mechanics. Native body-distance range, line of sight,
damageable objects and wargames remain server validation/behavior gaps.

The [death audit](player-death-client-evidence.md) corrects creature lethal-hit
notifications. The killing recovery now carries `deathBlow`, followed by a
victim `ActorKilled` notification covering observers who could not see the
source. The old state-only notification skipped the client's death announcement
and cleanup. Tests exercise source-only, shared and victim-only visibility and
two pending shots where the first kills the target.

Original player death/recovery codecs are now recorded and implemented as
unconnected foundations. Player lethal damage still has the existing placeholder
recovery behavior; it must be replaced together with a working original recovery
path. Hospital IDs cannot be copied from local teleporter IDs: the audit records
specific mismatches with the original graveyard table. Eligibility, relocation,
restored resources, death persistence and re-login remain necessary work.

The combined .NET 5 candidate built with zero errors and the same five existing
unused-variable/field warnings. **All 250 tests passed**, zero failed/skipped.
The candidate's source matched the reviewed workspace, excluding generated
`bin`/`obj` directories. An independent documentation audit found no material
contradictions between the implemented behavior and the stated evidence gaps.
These checks validate this implementation; they do not certify original-client
behavior or complete final-live fidelity.

The game service was recreated from tested image
`sha256:895a13fbcf52626516d16bb2d62a6d644ea55e7697b7da28c2ad636a5c7d6f2c`
at **19:06:20 UTC** and reported `Server ready!` at **19:06:30 UTC**. Its initial
post-deployment check was running with zero restarts and no unhandled/OOM/fatal
startup lines. Auth's image and start time are unchanged. This pass includes no
schema/data migration.

All three fresh SQLite backups passed integrity checks. Backups, private
deployment configuration, reviewed source, build/test logs, source comparison,
retained old/startup logs and before/after metadata are in
`/home/blizz/backups/rasa-net/20260912T190212Z-retail-lifecycle/`.
Rollback image: `rasa_net:before-retail-lifecycle-20260912`. Retag it as
`rasa_net:latest` and recreate only game with `--no-deps --no-build`; this code
rollback needs no database restoration. The full preservation goal remains
active with the limitations recorded above and in the linked evidence reports.

## Weapon lifecycle, inventory conservation and combat reports — 2026-09-12

The [weapon action reconstruction](weapon-actions-client-evidence.md) now uses
all 135 original draw/stow/reload timing rows. A captured weapon action tracks
windup, recovery, shared action-ID reuse and manual/autofire origin. Reload
revalidates its original weapon and inventory before transferring ammunition,
and a delayed resolution preserves the entire unpredicted recovery interval.
Eligible interruptions cancel unfinished reloads without consuming reserve
stacks or applying a successful recovery. Draw/stow readiness at recovery end
remains an explicit inference pending native animation strike evidence.

Reload conserves ammunition across multiple stacks and preserves loaded rounds.
Magazine, reserve stacks and emptied inventory links commit atomically, with
expected-count and account/character/location checks. Inventory movement now
updates the in-memory owner and slot alongside the persisted destination;
withdrawing an item from home storage no longer writes a personal item under
character ID zero. Character IDs are kept distinct from roster slots.

Autofire continues when the first action draws or reloads, maintains one
sequence per client, retries busy actions at the observed client cadence and
advances the global timer list once per elapsed interval. Its prior per-map
invocation incorrectly changed timing with the number of occupied maps.
The current database's uniform 1500 ms reload values, shot/refire values,
keepalive grace, modifiers, heat/jam and complete attack admission remain
unverified mechanics/data; the original action catalog alone does not prove
those server-supplied values.

The [combat report audit](combat-damage-client-evidence.md) propagates the
equipped weapon's actual damage type and separates absorbed armor from final
damage in resolved reports. The existing universal armor-first damage policy
still lacks the original type-specific rules, piercing, resistance and modifier
pipeline. The recovered resistance conversion agrees with official live D14
examples and is preserved as an unused helper awaiting authoritative inputs
and ordering.

Initial actor attributes now use the original constructor's
`normalMax/currentMax/current` order. Queued attribute, health and armor updates
retain their values. Body/Mind no longer keep an initial zero through stat
calculation, and recalculation preserves remaining armor instead of replacing
it with a regeneration accumulator. These consistency repairs do not establish
the emulator's stat growth or regeneration formulas as final-live rules.

The [hospital investigation](hospital-recovery-evidence.md) recovers six exact
Wilderness hospital/safe-zone map markers, and distinguishes waypoint,
graveyard, marker-entity and marker-text identity. It also recovers supplied
friendly/acquired/PvP-safe state and the different burial/hospital UI requests.
The marker catalog is not activated as respawn destinations: original respawn
coordinates, graveyard joins, eligibility, resource restoration and persistence
remain missing. Full player death and the broader final-live preservation goal
remain incomplete.

The final .NET 5 candidate built with zero errors and the same five existing
unused-variable/field warnings. **All 315 tests passed**, zero failed/skipped.
Its source matched the reviewed workspace excluding generated `bin`/`obj`.
Independent review found and closed the late reload-recovery and character-ID/
inventory-location issues; regression tests cover those paths and reopened
SQLite state. The prior full integration run's two stale damage-report
expectations were corrected against the original absorption contract and the
final suite includes full Lightning report parsing. Original-client sessions
and the unimplemented mechanics above remain separate fidelity verification.

Tested image
`sha256:c0649d7af72072c54b5e3ad9f9dc95d0c081d7e83262d38134d5416165dbba18`
replaced the game service at **19:34:01 UTC**, with `Server ready!` at
**19:34:10 UTC**. Initial verification found it running with zero restarts and
no unhandled/OOM/fatal startup lines. Auth's image and start time are unchanged.
This pass includes no schema migration or bulk world-data rewrite.

All three fresh SQLite backups passed integrity checks. The private deployment
configuration, reviewed source/docs, build/test logs, source comparison, old
and startup logs, and before/after metadata are retained in
`/home/blizz/backups/rasa-net/20260912T193344Z-retail-weapon/`.
Rollback image: `rasa_net:before-retail-weapon-20260912`. Retag it as
`rasa_net:latest` and recreate game with `--no-deps --no-build`; code rollback
requires no database restoration. The full final-live preservation goal
remains active.


## Primary attacks and inventory sessions — 2026-09-12

The [original primary attack catalog](weapon-attack-client-evidence.md) now
supplies all 198 action-1/action-174 rows to a captured execution lifecycle.
Manual requests retain their action pair, entity-or-location target, and
alternate flag. Admission checks the equipped primary pair, readiness, jam,
ammunition, current action and shared reuse. An unsupported specialized or
alternate request cannot execute an ordinary primary shot in its place.

The attack resolves after its original windup, remains busy through recovery,
and observes action-ID reuse across arguments. The 1/66 no-reuse flag and
174/32 literal 4/5/2 ms stages remain intact. Autofire schedules its first and
subsequent repeats from these stages instead of uniform template refire data.
Movement preserves these original primary actions. Interruption before
resolution causes no magazine debit or damage; later interruption preserves
already-spent ammunition and reuse.

Resolved attacks use the requested eligible target rather than the actor's
separate tracking target. Missing/dead/friendly targets become blind shots,
and an entity number reused by another object cannot receive a captured shot.
Ammo commits conditionally against the expected stored count and the captured
weapon's account/character/drawer ownership before memory changes. The original
server's precise ammo debit/impact ordering remains an inference. Native
geometry, LOS, target categories beyond the existing creature path, damage
modifiers and specialized/alternate behavior remain required reconstruction.

The [inventory session fixes](inventory-session-evidence.md) filter private
items by selected character before entity publication, retain the active drawer
through login and swaps, and persist empty weapon selections. Invalid stored
rows no longer prevent valid items from loading. Equipment, appearance and
weapon-information packets snapshot their queued values so later mutations do
not alter earlier updates. Map changes retire the prior inventory entities and
rebuild fixed-capacity lists before republishing items; reusing the character
object no longer grows those lists or skips occupied slots. Initial weapon
appearance is reconciled with the selected item before actor publication, and
an empty selection clears appearance/readiness. Existing second-hue persistence
and original packet-order details remain evidence gaps.

The [world equipment audit](world-equipment-client-audit.md) found exact numeric
matches for all 2,946 weapon classes, 3,377 armor classes and 30,225 template/class
mappings. No bulk rewrite is warranted. The weapon archetype field now reads
the original template ID rather than the class-row ID. All 2,440 weapon template
records still have the same 22 fields, and original server-supplied instance
stats cannot be reconstructed from the class table alone. The audit preserves
4,341 original nulls currently flattened to database zero as a separate gap.

Generic item requirements were incorrectly joined by template ID. The original
client reads those 5,293 requirement rows by item class; this distinction changes
expected requirements for 19,576 loaded templates. Correcting the loader uses
the existing original-matching rows without migrating stored world data.
Skill/race requirement rows and equipment slot mappings also match the original
tables; complete equipment eligibility still needs its own implementation audit.


The final candidate built with zero errors and the same five existing unused
variable/field warnings. **All 380 tests passed**, with zero failures/skips,
inside that candidate image without production database mounts or networking.
The image's source exactly matched the reviewed workspace excluding generated
`bin`/`obj`. Independent review closed first-autofire repeat scheduling and
repeated map-load/initial appearance defects before the final build. The final
suite includes 27 primary lifecycle cases, 18 inventory session cases and two
real-loader requirement cases alongside the prior regressions.
These tests verify implementation behavior; they do not supply missing original
server evidence or certify a complete final-live client session.


Tested game image
`sha256:ba9950bab9556f4932c973822a5730dedcc360bcfaf07517a69b7f15497668e2`
was deployed at **20:05:38 UTC** and reported `Server ready!` at
**20:05:48 UTC**. Initial checks show the expected image running with zero
restarts and no error/unhandled/OOM/fatal startup lines. Auth's image and start
time are unchanged. This pass has no schema migration or bulk world-data rewrite.

All three fresh SQLite backups passed integrity checks. Private deployment
configuration, source/docs, review patch, build/test logs, source comparison,
and before/after metadata are retained in
`/home/blizz/backups/rasa-net/20260912T200524Z-retail-attack/`.
Rollback image: `rasa_net:before-retail-attack-20260912`. Retag it as
`rasa_net:latest` and recreate only game with `--no-deps --no-build`; code
rollback needs no database restoration. The full final-live preservation goal
remains active, including original-client session verification and the gaps
in the linked evidence reports.


## Equipment eligibility, race and item destruction — 2026-09-12

The [equipment runtime](equipment-runtime-evidence.md) now checks original
current-attribute, inclusive level, race, positive skill-minimum and condition
rules. Integer condition preserves the original floor-division quirks and
rejects exactly zero. Both equipment endpoints validate a living avatar and
generated class slot before changing inventory. Personal and direct Home
swaps/unequips now commit both exact locations atomically, preserving character
IDs versus account-home owner zero. Failed writes leave inventory, pending
weapon work and queued success notifications untouched.

The [item-state audit](item-condition-client-evidence.md) found a missing
`RaceId` message. Initial actor data now publishes it before control/equipment,
allowing the original client's race checks to operate. It also found two
opposite trade-flag interpretations: the template loader stored a negative flag
in a positive property, and ItemInfo wrote that property as a negative flag.
Those mistakes canceled for populated ItemInfo rows but inverted tooltips.
The loader, ItemInfo and tooltip now agree; missing-template defaults preserve
the prior wire value and remain explicit placeholders. ItemInfo snapshots all
existing fields when queued.

The [destruction repair](item-consumption-evidence.md) conserves partial and
full item quantities in personal/home inventories. Expected count, registered
instance, account, owner, location and exact item identity are checked before
commit. Full removal updates the count and removes the correct inventory link
atomically; memory and client updates follow success. Excess quantities cannot
wrap, malformed wide values cannot narrow into small deletions, and a decoded
zero quantity is a no-op. Original item wear, repair economics and retention
policies are not inferred from these consistency fixes.

The full final-live goal remains incomplete. Clan equipment routes, storage
permissions/access, binding and uniqueness, remaining inventory operations,
complete mech equipment, original-client sessions and the broader mechanics/
content gaps remain tracked in the linked reports.


Final review also corrected the appearance-save failure path: a provider or EF
save error is logged without aborting stat/equipment refresh after a committed
swap. An isolated trigger-induced failure verifies that both the new armor
maximum and equipment notification still reflect the committed item. Malformed
equipment field types now use the connection's handled message exception.


The final .NET 5 candidate built with zero errors and the same five existing
unused-variable/field warnings. **All 469 tests passed**, with zero failures or
skips, inside the final image without production database mounts or networking.
Its source exactly matched the reviewed workspace excluding generated
`bin`/`obj`. Independent review confirmed the original eligibility predicates,
account/character/Home ownership, transaction rollback and the repaired
appearance-error path. Tests do not certify original-client sessions or supply
missing final-live mechanics and server policy evidence.


Tested image
`sha256:f27ad2110ccb77352b7cb23a714442d51fbb12fd14957e54cb8fb9410b3ca2e6`
replaced the game service at **20:31:30 UTC** and reported `Server ready!` at
**20:31:39 UTC**. Initial verification found the expected image running with
zero restarts and no error/unhandled/OOM/fatal startup lines. Auth's image and
start time are unchanged. No schema migration or bulk world-data rewrite was
introduced.

All three fresh SQLite backups passed integrity checks. Private deployment
configuration, reviewed source/docs, patches, build/test logs, source comparison
and before/after metadata are retained in
`/home/blizz/backups/rasa-net/20260912T203118Z-retail-equipment/`.
Rollback image: `rasa_net:before-retail-equipment-20260912`. Retag it as
`rasa_net:latest` and recreate only game with `--no-deps --no-build`; this code
rollback needs no database restoration. The complete final-live preservation
goal remains active.

## 2026-09-12: original lockbox credit request and conserved transfers

Static inspection of the 1.16.5.0 client proves that the lockbox UI accepts any
positive integer amount, deposits it unchanged and negates it for withdrawal.
The emulator's explicit temporary 500-credit workaround has been removed now
that compact signed decoding is repaired. Wallet and account-bank updates commit
in one transaction with ownership and expected-balance comparisons; failures
leave both persisted balances and session state unchanged. A withdrawal no
longer passes through the loot reward notification helper. No retail bank cap
or original error message is inferred from the existing storage limits.

Provenance, exact original function/offset references, implementation boundaries
and outstanding fidelity gaps: [lockbox credit evidence](lockbox-credit-client-evidence.md).

The final .NET 5 image built with zero errors and the same five existing unused
variable/field warnings. **All 520 tests passed**, with no failures or skips,
inside that image without production database mounts or networking. Source
comparison matched the reviewed workspace excluding generated `bin`/`obj`.
These checks verify the correction; original-service capture comparison and
complete final-live fidelity remain outstanding.

Tested image
`sha256:913a80537fdc87667aa8c33803ef606bb50a5e3fa52cc8c7de8cbc26b393689c`
replaced the game service at **20:43:25 UTC** and reported `Server ready!` at
**20:43:36 UTC**. Startup verification found zero restarts and no error,
unhandled, fatal or OOM log lines. Auth's image and start time are unchanged.
No schema migration or bulk world-data rewrite was introduced.

All three fresh SQLite backups passed integrity checks. Private deployment
configuration, reviewed source/docs, patch, build/test logs, source comparison
and before/after metadata are retained in
`/home/blizz/backups/rasa-net/20260912T204316Z-retail-credit/`.
Rollback image: `rasa_net:before-retail-credit-20260912`; retag it as
`rasa_net:latest` and recreate only game with `--no-deps --no-build`.
Code rollback requires no database restoration. The complete final-live
preservation goal remains active.

## 2026-09-12: original lockbox tab prices and purchase correction

Recovered the original five-row lockbox tab table and its client consumers.
Tabs contain 96 slots each; additional tabs cost 100,000, 1,000,000, 10,000,000
and 100,000,000 wallet credits. The previous purchase handler mistakenly added
the price through a positive signed adjustment. It now deducts the recovered
price and unlocks only the next tab in one account-scoped transaction. The
original living-avatar and affordability predicates are enforced, and stale
or failed purchases publish no payment or unlock. Existing bank credits are
preserved when changing tab ownership.

Evidence and outstanding full-bank fidelity requirements:
[lockbox tab reconstruction](lockbox-tab-client-evidence.md).

The final .NET 5 image built with zero errors and the same five existing unused
variable/field warnings. **All 541 tests passed**, with no failures or skips,
in the final image without production database mounts or networking. Source
comparison matched the reviewed workspace excluding generated `bin`/`obj`.
These checks verify the implementation, not complete original-service fidelity.

Tested image
`sha256:947f2ae3a05a032a4d1355edf1a9ba7181bd085e2f7abc53ae6a1760f2acab2c`
replaced the game service at **20:51:29 UTC**, reporting `Server ready!` at
**20:51:39 UTC**. Verification at 20:52:04 UTC found the expected image running,
zero restarts and no error/unhandled/fatal/OOM log lines. Auth's image and start
time are unchanged. No schema migration or historical balance rewrite occurred.

All three fresh SQLite backups passed integrity checks. Private deployment
configuration, reviewed source/docs, patch, build/test logs, source comparison
and before/after metadata are retained in
`/home/blizz/backups/rasa-net/20260912T205118Z-retail-tabs/`.
Rollback image: `rasa_net:before-retail-tabs-20260912`; retag it as
`rasa_net:latest` and recreate only game with `--no-deps --no-build`.
Code rollback requires no database restoration. Full final-live preservation
remains incomplete and the goal stays active.

## 2026-09-12: original storage quantities and atomic item placement

Original client calls confirm that personal/Home moves carry a selected quantity.
The old handlers ignored it, and the Home withdrawal decoder discarded long
quantities. The four requests now share strict decoding; whole moves and swaps
commit both item locations before publishing changes. Selected partial amounts
split into empty slots with one transaction covering the count decrease, new
item and placement. Persisted instance data survives a split and reload.

The original 96-slot tab ranges, personal categories, living-avatar predicate and
no-lockbox flag govern admission. Home operations compare persisted tab ownership,
including equipment transfers. Original combining rules for occupied stacks
remain unverified and incomplete; this is not a claim of full inventory fidelity.

Provenance, exact original consumers, validation and remaining requirements:
[inventory placement reconstruction](inventory-placement-client-evidence.md).

The final .NET 5 image built with zero errors and the same five existing unused
variable/field warnings. **All 577 tests passed**, with no failures or skips,
in the final image without production database mounts or networking. Source
comparison matched the reviewed workspace excluding generated `bin`/`obj`.
The tests prove the corrected storage invariants and packet decoding; original
service capture comparison and complete inventory fidelity remain outstanding.

Tested image
`sha256:cd22fb83bc58e2bc111f1ff73d9a3048302a2131c605ad0bd124444d4442f9fd`
replaced the game service at **21:06:08 UTC**, reporting `Server ready!` at
**21:06:17 UTC**. Verification at 21:06:48 UTC found the expected image running,
zero restarts and no error/unhandled/fatal/OOM log lines. Auth's image and start
time are unchanged. No schema migration or historical item relocation occurred.

All three fresh SQLite backups passed integrity checks. Private deployment
configuration, reviewed source/docs, patch, build/test logs, source comparison
and before/after metadata are retained in
`/home/blizz/backups/rasa-net/20260912T210552Z-retail-placement/`.
Rollback image: `rasa_net:before-retail-placement-20260912`; retag it as
`rasa_net:latest` and recreate only game with `--no-deps --no-build`.
Code rollback requires no database restoration. The full preservation goal
remains active while original systems and verification are incomplete.

## 2026-09-12: starter equipment character diagnosis

A level-1 Recruit with no saved skills could not satisfy the original Firearms
1 and Motor Assist Armor 1 equipment requirements. The user's requested
character update trained those two skills using the existing point budget;
three points remain. A verifier using the deployed server rules checked the
saved result and all four owned equipment items. No gameplay source, global
requirements, or automatic starter grants changed. See the
[starter equipment investigation](starter-equipment-research.md).

The same tested game image restarted at **21:23:43 UTC** and reported ready at
**21:23:52 UTC**. Verification at **21:24:26 UTC** found zero restarts and no
error/unhandled/fatal/OOM log lines; auth's image and start time are unchanged.
Private integrity-checked backups and operation records are retained in
`/home/blizz/backups/rasa-net/20260912T212342Z-blizz-training/`.

## 2026-09-12: Recruit initialization and atomic creation

The user's progression order is now recorded in `AGENTS.md` and
[the preservation sequence](progression-preservation-plan.md): creation and
final live boot camp first, then successive class tiers through endgame.

A recovered September 2008 wiki revision explicitly establishes all five
Recruit skills at rank 1. Original-client catalog mappings, an archived
official Recruit page and a contemporary level-1 image corroborate the result.
Creation now persists Firearms, Hand to Hand, Motor Assist Armor, Lightning
and Sprint at rank 1, leaving zero unspent points. Lightning retains its Power
Logos requirement. Character, appearance, initial skills/items, family-name
change and first bank tab commit together before creation success is sent.
Later characters preserve existing training and account bank state. Starter
items now receive their own maximum durability; the pistol previously used a
different template. Full starter loadout and tutorial reward fidelity remain open.

The diagnosed historical character received only its three remaining missing
initial ranks after a guarded comparison against its known state. Fresh
snapshot verification with the candidate's actual initializer and requirement
checker passed before and after. Only the skills table changed; no earned
progression, inventory or Logos was rewritten.

Evidence, artifact hashes, dated revisions and remaining first-segment gaps:
[new-character initialization](new-character-client-evidence.md).

The .NET 5 candidate built with zero errors and the same five existing unused
variable/field warnings. **All 584 tests passed**, with no failures or skips,
inside the final image without production database mounts or networking.
Source comparison matched the reviewed workspace excluding generated `bin`/`obj`.
Seven new integration cases exercise creation, persistent state and rollback;
passing tests establish implementation behavior, not full original fidelity.

Tested image
`sha256:b319d6018f9f450743315e5f13b4988d07776d14588c96f2a438178c6d07af09`
replaced game at **22:11:21 UTC**, reporting ready at **22:11:30 UTC**.
Verification at **22:11:50 UTC** found the expected image running, zero restarts
and no error/unhandled/fatal/OOM log lines. Auth's image and start time are
unchanged. No schema migration was required.

All three fresh SQLite backups passed integrity checks. Reviewed source/docs,
private deployment configuration, repair scripts, before/after snapshots,
table hashes, build/test logs and service metadata are retained in
`/home/blizz/backups/rasa-net/20260912T220953Z-retail-creation/`.
Rollback image: `rasa_net:before-retail-creation-20260912`; retag it as
`rasa_net:latest` and recreate only game with `--no-deps --no-build`.
Code rollback does not require restoring the character database. The full
creation/tutorial segment and overall preservation target remain incomplete.

## 2026-09-12: original first-family creation message

Original client code sends `CreateCharacter` (436) with six fields while the
account has no family name, and `RequestCreateCharacterInSlot` (512) with seven
fields for later characters. The first form had no server handler. Both now
reach the same transactional initialization; selection transmits `None` for
an unchosen family so the client can take its original first-family branch.
Persisted account/slot checks reject replay or duplicate initial grants even
when cached account state is stale. Both wire shapes are checked before reading
their fields, and oversized slot integers cannot wrap into an existing pod.

[Creation evidence](new-character-client-evidence.md#first-family-protocol)
records exact client consumers, opcode data, the slotless first-request
compatibility choice and remaining original-session verification. The 95
original starter appearance mappings were audited with no DB differences.

The rebuilt tutorial's four mission IDs, 19 objectives and nine dialogue-package
bindings are now preserved in [the boot-camp catalog](evidence/bootcamp-client-catalog.json)
and explained in [the boot-camp audit](bootcamp-client-evidence.md). The original
map was acquired and CRC/hash checked. This research does not insert speculative
quests or move characters into an unpopulated tutorial: exact spawns, reward
amounts, triggers, start position and skip behavior remain unresolved.

The .NET 5 candidate built with zero errors and the same five existing unused
variable/field warnings. **All 598 tests passed**, with no failures or skips,
inside the final image without production database mounts or networking.
Source comparison matched reviewed code excluding generated `bin`/`obj`.
The 21 focused creation/packet cases cover both messages, persistence and
rollback, slot/replay checks, appearance fields and family-state encoding.

Tested image
`sha256:6d635509c10b47ba63aab47f0d1fc5b2544454920d0d38f64c0205f78c08235e`
replaced game at **22:30:10 UTC**, reporting ready at **22:30:19 UTC**.
Verification at **22:31:25 UTC** found the expected image running, zero restarts
and no error/unhandled/fatal/OOM lines. Auth's image and start time are unchanged.
No schema migration or saved-character rewrite occurred in this deployment.

All three fresh SQLite backups passed integrity checks. Source/docs, private
deployment configuration, build/test logs and before/after service records:
`/home/blizz/backups/rasa-net/20260912T223008Z-retail-entry/`.
Rollback image: `rasa_net:before-retail-entry-20260912`; retag it as
`rasa_net:latest` and recreate only game with `--no-deps --no-build`.
Code rollback requires no database restoration. Complete final-live creation,
boot camp and new-character-to-endgame progression remain incomplete.


## 2026-09-12 — Original Recruit outfit tint and tutorial entry audit

New characters now persist white RGBA for the fixed Recruit boots, vest and
legs, matching the recovered original creation window. The previous packed
value produced gray with partial alpha. Both first-family and later-character
integration cases verify persistence. Chosen appearance fields, inventory-item
colors and existing character records are unaffected. Exact original source
locations and hash are in [creation evidence](new-character-client-evidence.md).

The [boot-camp audit](bootcamp-client-evidence.md) now records the original
skip-prompt conditions and the current first-login context mismatch. Additional
map audio placements and an archived Google Code emulator were checked; neither
supplied the missing original spawns or working quest definitions. Full initial
loadout, boot camp and subsequent progression remain incomplete.

The .NET 5 image built with zero errors and five existing warnings. All **598
tests passed**, zero failed/skipped, without production database mounts or
network access. Reviewed source matched the final image excluding `bin`/`obj`.
Image `sha256:e031437f52a415d8218001ad694c5603c8f67b08e6800456a3bc8bea51424efa`
started game at **23:26:24 UTC**, ready at **23:26:34 UTC**. Verification at
**23:26:49 UTC** found the expected image running with zero restarts and no
error/unhandled/fatal/OOM lines. Auth's image and start time are unchanged.

All three SQLite backups passed integrity checks. Reviewed source/docs,
configuration, test/build logs and service records are preserved at
`/home/blizz/backups/rasa-net/20260912T232612Z-retail-outfit/`.
Rollback image: `rasa_net:before-retail-outfit-20260912`; retag as `rasa_net:latest`
and recreate only game with `--no-deps --no-build`. No schema migration or saved
character rewrite occurred; code rollback needs no database restoration.


## 2026-09-13 UTC — Character selection failure handling and boot-camp audio evidence

Selecting an empty or unowned slot now leaves saved and session state intact.
Previously the session selected-slot field changed before ownership lookup or
saving succeeded. It now updates after the selected-slot/login save completes.
The original two-field selection message is checked explicitly, and oversized
or negative slot integers cannot wrap into a valid pod. This corrects emulator
entry failures; original error-response behavior and the complete first-login
and boot-camp skip path remain unverified.

Ten new cases cover original skip/no-skip packet decoding, malformed messages,
slot overflow, empty/unowned slots and a real SQLite login-save failure. The
23 focused creation/selection cases pass; the final image passes **all 608
tests**, with zero failures or skips and no production DB mounts or networking.
The .NET 5 build has zero errors and the same five existing warnings. Reviewed
source matches the image excluding `bin`/`obj`.

[Boot-camp evidence](bootcamp-client-evidence.md) now records the original
selection sender, the obsolete Elvers/Burba fansite guide and an unresolved
August 2008 description of the rebuilt tutorial's order. A new
[audio catalog](evidence/bootcamp-audio-catalog.json) preserves four ambient sets
and five named voice sets with original table hashes and bytecode offsets.
The voice filenames identify the four later tutorial missions and McAllister's
bark, but do not establish playback triggers, NPC placements or rewards.
Original audio playback was not acquired or heard. Full boot camp and subsequent
progression are still incomplete.

Image `sha256:7c81e87e6b6b8582c6a6d8b1089c72d152ddcca01ffee2cabee26e031d064f5f`
started game at **00:02:35 UTC**, ready at **00:02:44 UTC**. Verification at
**00:03:10 UTC** found the expected image running with zero restarts and no
error/unhandled/fatal/OOM lines. Auth's image and start time are unchanged.
All three fresh SQLite backups passed integrity checks; no schema migration or
saved-character rewrite was performed.

Source/docs, configuration, logs and service records:
`/home/blizz/backups/rasa-net/20260913T000223Z-retail-selection/`.
Rollback: retag `rasa_net:before-retail-selection-20260913` as `rasa_net:latest`
and recreate game alone using `--no-deps --no-build`. Code rollback requires no
database restoration. Original-client playthrough comparison remains pending.


## 2026-09-13 UTC — Mission-log protocol, NPC objective conversations and boot-camp evidence sweep

A verified six-track sweep (dated wiki history, original client tables and
code, contemporary captures, the map file and a server audit, each with an
independent verification pass) established that the original boot-camp map
contains no gameplay actors: first-login position, NPCs, crates and the exit
are server data that no recovered source supplies. Mission 2005 (the
"Calling for Reinforcements" retry), the client's mission-log limits,
objective indicator names, NPC name ids and Eloh speech are now catalogued.
Details and corrected citations: [boot-camp evidence](bootcamp-client-evidence.md#2026-09-13-verified-sweep-what-the-client-and-the-map-do-and-do-not-establish).
No tutorial content, spawn, reward or skip behavior was added.

The server now implements the recovered client contract that every
conversation-driven mission, including the boot camp's, depends on: `NPCInfo`
package ids, per-player conversation status and topics, objective completion
through `CompleteNPCObjective`, turn-in and abandon, persistent objective
progress restored through `MissionStatusInfo`, list-shaped `PlayerFlags`,
optional-integer reward selection and a dictionary-shaped `CanLootItems`.
Mission definitions are offered only when complete; the unvalidated seeds
321/429 are withheld, so their NPC markers no longer advertise them. Item
rewards, radio and shared missions stay explicit gaps; their requests are
decoded and ignored instead of disconnecting the client. Proven client facts
and emulator storage choices are separated in
[mission research](mission-research.md#mission-log-protocol-and-persistence--2026-09-13).

Two adversarial review rounds (four lenses, then a focused re-review of the
fixes, each finding independently verified) confirmed 31 reports, several of
them duplicates and none critical or high. Every code finding was fixed and
tested before deployment: reward display/payout mismatches, completeability
ordering, saved progress after definition changes, undeliverable item
rewards, radio/share requests and an invented chat message. The documentation
findings were corrected in these records, including the D11.4 source
(public-test notes, repeated in the D11.6 live notes), a reversed D13.4
paraphrase and overstated protocol claims.

Schema: `character_mission.change_time` (default 0) and new
`character_mission_objective`; world tables `npc_mission_objective`,
`npc_mission_objective_conversation` and `npc_mission_objective_transition`,
all empty. Migrations were generated with dotnet-ef 5.0.1 for SQLite and a
disposable MySQL 8.4.11 server; the MySQL chain preserved legacy mission rows
including state 4294967295. The SQLite scripts were dry-run against copies of
the live databases before deployment. Records are in
`/home/blizz/backups/rasa-net/research/20260913-bootcamp/mission-log-migrations/`.

The .NET 5 image built from a clean context with zero errors and the same
five existing warnings. **All 634 tests passed** in the image, with no
failures or skips and no production database mounts or networking. Reviewed
source matched the image excluding `bin`/`obj`. Passing tests verify the
implementation; the conversation flow has not yet been exercised with the
original client.

Image `sha256:d7a8e1163c686d78d8b98a6d9fcecbb876a742e3aa4f3f10285a60241598e511`
started game at **04:18:33 UTC**, ready at **04:18:49 UTC**, and authenticated
with auth. Verification at **04:19:13 UTC** found zero restarts, no
error/unhandled/fatal/OOM lines, both migrations applied, and auth's image and
start time unchanged. After startup the live character and world databases
differed from the fresh backups only in migration history and the new empty
tables; auth was unchanged; all passed integrity checks. The auth container
had independently logged a .NET "Out of memory." and restarted at 02:14 UTC,
before this deployment; this deployment did not touch it.

Backups, configuration, reviewed source/docs, logs, scripts and table
comparison: `/home/blizz/backups/rasa-net/20260913T041812Z-retail-missionlog/`.
Rollback: retag `rasa_net:before-retail-missionlog-20260913` as
`rasa_net:latest` and recreate game alone with `--no-deps --no-build`. The
previous image ignores the added column and tables; a database restore is
needed only to remove them.

## 2026-09-13 UTC — Boot-camp reconstruction foundations (S0) and verified footage

This deployment adds the data layer for reconstructing lost boot-camp server content under the
user's evidence-bounded reconstruction decision (`AGENTS.md`). It changes nothing visible: every
new table is empty, and nothing that could use it is implemented yet.

**What was added**
- **World tables** for mission prerequisites, objective bindings, counters, timers and
  indicators, plus content areas, placements, conditions, rules (with filters, including a
  placement-state filter), rule actions, item sets, locations and per-context map settings.
- **Character storage** for objective timers and counters and per-character content facts.
- **Staged character writes**, committed only through the unit of work.
- **A content validator that fails closed:**
  - It withholds any row with a bad reference, an invented column, an unimplemented mechanic,
    a usable kind with no recovered client state machine, a client-posted tutorial id, a rule
    cycle, or an offer of a mission that cannot be offered.
  - A withheld row withholds everything that references it.
  - A mission with a withheld row is not offered.
  - Startup logs every gap. This build implements none of the mechanics, so any seeded row
    would be withheld.
- **Boot-camp entry switch** (`GameDataConfig.Bootcamp`, default `Disabled`). Character creation
  consults it, but until the boot camp can run end to end it always gives the existing Wilderness
  start.
- **Lookup fixes:** use and loot requests that name an object which no longer exists are now
  ignored instead of throwing.

The plan's `map_info.instancing` column became a separate `content_map_setting` table, because
the world seed migration reflects `map_info`'s columns.

The machine-readable evidence contract is in commit `7ac7639`:
- manifest schema;
- boot-camp manifest with sources, reserved key ranges, gap register and open owner decisions;
- empty positions and footage-event files;
- validator, provenance registry and a SQLite/MySQL seed-parity harness, with one rejecting
  fixture per rule.

**Footage**
- Three original recordings supplied by the owner were transcribed frame by frame, with every key
  event independently re-verified (246 checked, 0 refuted), and matched against the client radar
  maps.
- Player chat dates the main session to about 2009-02-26, so it shows the final live boot camp.
- Findings, tags and remaining gaps are in [boot-camp evidence](bootcamp-client-evidence.md#2026-09-13-verified-footage-what-three-original-recordings-establish).
- Nothing from the footage is seeded yet.

**Review**
- An independent review found no startup or gameplay change with empty tables. It confirmed
  defects in staged writes after a delete, several fail-open validator paths, cycle detection
  that could miss members, and tests that could not catch propagation or entry-gate regressions.
- All were fixed. A focused re-review confirmed the fixes, and its three low-severity findings
  (orphan counters after a delete, untested propagation lines, a stale provenance registry)
  were fixed and tested before deployment.

**Schema and migrations**
- Generated with dotnet-ef 5.0.1:
  - SQLite and MySQL `MissionContentLayer` (world, `20260913180618`/`20260913180640`);
  - `MissionContentRuntimeState` (character, `20260913180522`/`20260913180550`).
- On a disposable MySQL 8.4.11 server:
  - legacy mission rows (including state 4294967295) and the seeded world rows survived upgrade,
    rollback and reapply;
  - no content key auto-increments, and an explicit id 0 is kept;
  - rollback drops the new tables and their rows, as designed.
- The SQLite scripts were dry-run on copies of the live databases: integrity ok, and every
  existing table was byte-identical.
- Records: `/home/blizz/backups/rasa-net/research/20260913-bootcamp/content-migrations/`.

**Image and tests**
- The .NET 5 image built from a clean context with zero errors and the same five existing
  warnings.
- **All 753 tests passed** in the image (634 existing, 91 evidence contract, 28 new), with no
  failures or skips and no network.
- The image's `src` and `docs/evidence` matched the reviewed tree.

**Deployment**
- Image `sha256:5ac800497000a21d739c1ab4dd263be5efb653f2d7e63e89ec9778ebabdeec56` started game at
  **18:43:36 UTC**, authenticated with auth at 18:43:46 and was ready at **18:43:48 UTC**.
- The log shows "Loaded 0 content rules (0 content rows, 0 gaps)" and "Boot camp entry:
  Disabled".
- Mission 321/429 gap lines are unchanged.
- Verification at **18:43:54 UTC** found zero restarts, no error/unhandled/fatal/OOM lines, both
  migrations applied, and auth's image and start time unchanged.
- After startup the live character and world databases differed from the fresh backups only in
  migration history and the new empty tables. Auth was unchanged, and all three passed integrity
  checks.

Backups, configuration, reviewed source and docs, logs and the table comparison are in
`/home/blizz/backups/rasa-net/20260913T184328Z-retail-content-s0/`.

**Rollback:** retag `rasa_net:before-retail-content-s0-20260913` as `rasa_net:latest` and recreate
game alone with `--no-deps --no-build`. The previous image ignores the added columns and tables.
A database restore is needed only to remove them.
## 2026-09-13 UTC — Client objective tables seeded at original tier (schema approved by owner)

The owner approved the two schema changes that [mission research](mission-research.md) had
identified as the blockers for seeding the client's objective tables, and both are now deployed
(migrations `20260913234728_MissionObjectiveClientColumns` and `20260913235900_MissionClientObjectiveSkeleton`,
applied to the live SQLite world database and verified against a disposable MariaDB for the MySQL path):

- `npc_mission_objective_conversation` gained `convo_type` as a fifth primary-key column, making the
  table a lossless 1:1 image of the client's `objectiveconversation` (1,727 rows; 202 of the 1,140
  key groups carry more than one convoType, so the previous 4-column key could not represent 34% of
  the data). The runtime `MissionObjectiveConversation` now carries `ConvoType` for the
  completion/reminder/choice distinction the schema preserves.
- `npc_mission_objective`'s `ordinal`, `is_required` and `revealed_on_accept` are now nullable, and
  `comment` was widened to varchar(100) (223 of the 3,454 client objective names exceed 50 chars,
  max 90). The three flags are server-authoritative with no surviving source, so they stay NULL
  instead of receiving guessed defaults: `Mission.DefinitionGaps` now reports
  "objective N has unknown ordinal/required/revealed flag" per objective, which keeps every such
  mission unoffered (fail-closed) rather than asserting gameplay. The two mission-1990 rows seeded
  by `BootcampS1Initiation` keep their footage-tier values and are excluded from the skeleton.

Seeded at **`original`** tier, verbatim from the retail 1.16.5.0 client's `data/game.zip` members
`generated/client/missionobjective.pyo` (3,454 rows) and `generated/client/objectiveconversation.pyo`
(1,727 rows), decoded through `python/client/clientlanguagemanager.py` in `trpython.zip`:
**3,454** `npc_mission_objective` rows (mission_id, objective_id, name as comment) and **1,727**
`npc_mission_objective_conversation` rows (all five key columns). Every value resolves through the
client's own text-id indirection with zero exceptions (see the 2026-09-13 sweep section and
`mission-research.md` for the full table semantics and extraction recipes). Provenance is recorded
in the seed rows class header (`MissionClientObjectiveSkeletonRows.cs`); no manifest is used because
no field is estimated — the seed is a verbatim import, like the Logos and MapInfo seeds.

Deployment consequence: missions 321 and 429 now load their client objectives (310; 4 and 5) and
report precise per-objective unknown-flag gaps instead of "no objectives"; both remain unoffered,
as before. 3,449 objective rows and 1,721 conversation rows reference missions whose
server-authoritative `npc_mission` columns are unrecovered; `LoadMissions` logs this expected state
as one summary line each instead of one error per row. The previously observed withholding of the
mission-1990 offer rule ("objective has no completion binding" at catalog-build time, because
content bindings attach after the catalog computes gaps) is pre-existing fail-closed behavior, not
changed by this work.

Verification: full test suite 777/777 green; both provider migrations applied forward and the
SQLite pair also reverted (Down preserves the 1990 footage-tier rows); container rebuilt and the
game server starts clean with the new schema.

## 2026-09-14 UTC — Merge regressions resolved; player death and hospital recovery

The EllimistArcade merge (`e06035a`) is resolved in `0678c85`: 128 failures under the .NET 5 CI
runtime were regressions where the merge took the other branch over this branch's tested and
evidence-backed code (disconnect contract, bounded protocol-frame parsing, weapon draw/reload/stow
through `WeaponActionManager` — the merged handlers queued reloads that never resolved — skill and
attribute validation, level-up point deltas). The local SDK 8 runtime hid them behind EF Core 5
startup failures; verification now runs in the `mcr.microsoft.com/dotnet/sdk:5.0` image.

Player death is now playable end to end: lethal hits kill, Hospital Selection offers the hospitals
the character knows, and the chosen hospital revives the player at its client map marker with full
health. The boot camp offers Refugee Base Medic (graveyard 20000001) as in the final-week footage
(A4-36), and the respawn position matches the measured respawn to 1.1 m. Wilderness hospitals are
gained within 100 m with the original "You just gained" message. Details, sources and remaining
gaps (trauma, equipment wear, ally revival, death persistence, control points):
[player-death-implementation.md](player-death-implementation.md) and
`docs/evidence/hospital-catalog.json`. Full suite 801/801.

## 2026-09-14 UTC — Creature kill experience, credits and kill streak

The emulator paid `creature level × 100 ± 10%` experience and 1–10 random corpse credits. Kills now
pay from the 1.16.5.0 client's `shared/gameconstants.py` values (`BASE_KILL_XP 62.5`,
`STREAK_BASE_PER_PARTY_MEMBER 3`, `STREAK_LEVEL_BASIS 10`, `STREAK_MAX_VALUE 5`,
`MAX_KILLING_STREAK_PRESTIGE_POINT_BONUS 1`) and a fit to every clean kill line in the final-week
footage: base experience `62.5 + 4.2·L + 0.2·L²` (L = creature level), truncated only after the
streak multiplier as `shared/xpinfo.py ApplyModifier` does, and `5·L` credits paid at the kill
through `GotLoot`. The fit reproduces all nine recorded observations for levels 1–9, including the
four streak-doubled values (133, 143, 189, 233) that would be one lower if the base were rounded
first. The third kill in a streak sends `SetKillStreak(1)`, one prestige point with PM 10000134, and
doubles experience; a streak ends 15 s after the last kill (bounded to 12.5–16.1 s by A4). The
final-week cave-fight timeline (A3-081 to A4-28) is reproduced exactly by `KillRewardTests`.
Level-difference, squad, partial-credit and crit-kill modifiers and original loot tables remain
gaps (`docs/evidence/kill-rewards.json`). Full suite 811/811.

## 2026-09-14 UTC — Private boot-camp instances, S4 mechanisms, fork navmesh work

- **S3:** context 1985 is a per-character instance; see
  [progression-preservation-plan.md](progression-preservation-plan.md#s3-private-instances-status).
- **S4 mechanisms:** kill bindings with objective counters, staged `grant_rewards`, and owner-conditioned
  placement presence in instances are implemented. No mission 1994 content is seeded yet.
- **Fork work merged (code only):** EllimistArcade's commits after `369a663` bring per-map Detour
  navmeshes built from the client's own terrain heightmaps and collision volumes (creatures path on the
  mesh instead of floating through rock), crafting stations placed at the client's `CRAFTING_STATION`
  markers (recipes still decline), and item repair `ItemStatus`. The wander pacing of `c5634b9` (20 m,
  1.6 m/s strolls, 12–40 s idle) is emulator tuning with no retail source. The 328 MB of built `.nav`
  files are derived client assets and stay outside Git (a copy is at
  `/home/blizz/backups/rasa-net/navmesh-8b65ca7/navmesh`; `GameDataConfig.NavMeshPath`, default `navmesh`).
  Without the files, creatures keep straight-line movement. Full suite 825/825.

## 2026-09-14 UTC — Capture the Flag (S4) content seed

- Mission 1994 and its boot-camp content are seeded (`BootcampS4CaptureTheFlag`); details, labels and
  decisions in [progression-preservation-plan.md](progression-preservation-plan.md#s4-capture-the-flag-status)
  and the boot-camp manifest. Values are evidence-bounded reconstructions, not recovered server data: the
  giver, receiver, transitions 4→2 and 1→3, the cave-in trigger radius, the boss position and presence, and
  Tizzik Gi's level are inferred; creature classes, health, speeds and the Thrax attack (emulator
  `creature_action` 33, whose attack pair matches the client's boot-camp Bane pistol) are labelled analogues.
- Not reproduced: the boss fight itself (never recorded), escorts and allies, 1994 credits and item reward,
  Thrax respawn, Youngblood's appearance and walk-in, and the original attack damage and timing.
  Full suite 839/839.

## 2026-09-14 UTC — Calling for Reinforcements (S5) and exit to Alia Das (S6) content seed

- Missions 1995 and 2005 and the boot-camp exit are seeded (`BootcampS5Reinforcements`, `BootcampS6ExitToAliaDas`);
  details, labels, conflicts and decisions in
  [progression-preservation-plan.md](progression-preservation-plan.md#s5-calling-for-reinforcements-seed-status)
  and the boot-camp manifest. Values are evidence-bounded reconstructions, not recovered server data: the objective
  order except 1 → 4, the corpse and wounded-soldier positions, the reinforcement positions, the wreck and bomb
  classes, the 2005 level, the exit radius and the indicator ids are inferred; the bomb windup and fuse, Van
  Valkenberg's position, the indicator positions and the Alia Das arrival are measured; the 1995 timer (600 s), NPC
  classes, levels and health, and the corpse class are labelled analogues. OD-25..OD-34 were decided by the agent for
  the owner and await owner review.
- Not reproduced: the 1995/2005 rewards, the hidden level-3-to-4 experience before Alia Das, a bomb inventory item,
  detonation damage, the reinforcement dropship, beam-in and walk-off, the unnamed reinforcements and other outpost
  creatures and NPC appearance. The D13.4 abandon quirk is kept. Rogers now stands in the Alia Das command tent
  (`BootcampFixRogersTurnIn`: level observed, position measured, rotation inferred, class and health analogues) and
  takes the 1995/2005 turn-in, which still pays nothing. Full suite 846/846.

## 2026-09-14 UTC — Wilderness arrival: Training Day (segment 3, W1) content seed

- Mission 1526 Training Day, Training Officer Kincaid at Alia Das and the forced Headquarters offer on entering
  Alia Das are seeded (`WildernessArrivalTrainingDay`). Details, labels and decisions are in
  [progression-preservation-plan.md](progression-preservation-plan.md#w1-wilderness-arrival-training-day-status) and the
  boot-camp manifest (slice W1). These are evidence-bounded reconstructions, not recovered server data:
  - observed: the offer, its 120 credits (partly legible) and the reward names, and the tooltip range and alt damage;
  - measured: Kincaid's position;
  - inferred: the offer trigger, Kincaid's level (partly legible glyph), rotation and package, the mission level,
    category and shareable flag, and the reward template ids 116929/116930;
  - labelled analogues: Kincaid's class and health, the reward flags and the unevidenced weapon fields.
- The Training Day reward pistols now exist as item templates (`itemtemplate`, `itemtemplate_weapon`). Their offer
  still reads "Pistol"/"Pulse Pistol" without the Vextronics module line, and they carry no price.
- Not reproduced: Training Day experience, the offer delay after the transfer, an offer for characters who skip the
  boot camp, Kincaid's appearance and observed facing, and missions 2010/2011 (held for a class-chosen trigger). The
  emulator's Major Bonham spawn beside the arrival is unchanged. OD-36..OD-42 were decided by the agent for the owner
  and await owner review. Full suite 849/849.

## 2026-09-15 UTC — Class gear: "Getting It In Gear" (segment 3, W2) content seed

- Missions 2010 "Getting It In Gear: Soldier Class" and 2011 "…: Specialist Class" and their class load-out are seeded
  (`WildernessClassGear`), answering the tier-2 class choice that `65cafcb` added the `class_selected` event for. This
  closes `GAP-W1-GEAR-MISSIONS` and supersedes the OD-42 hold (OD-43). Details and labels are in
  [progression-preservation-plan.md](progression-preservation-plan.md#w2-class-gear-missions-20102011-getting-it-in-gear-status)
  and the boot-camp manifest (slice W2). These are evidence-bounded reconstructions, not recovered server data:
  - original (client): the two mission ids, texts and category names 10000002/10000003, the completion package 133, the
    twelve D11 item templates 122859-122871 and their item classes, the class-owned skills that carry them
    (21/22 Soldier, 30/14 Specialist), and the armor values (client itemclass `max_hp`);
  - inferred: the radio giver 0, receiver creature 132, mission level 5, the one objective, the class_selected rules and
    their two-term conditions, the `forced` dispatch, the per-mission reward split and the neutral 0 prices;
  - labelled analogues (OD-43): the templates' quality 2 and trade/binding flags from the D11 new-player block's uniform
    world-seed rows, and the Rage-O-Matic/Repair-O-Matic `itemtemplate_weapon` columns from the world seed's machine-gun
    and tool family rows.
- Quartermaster Caufield is **not** a new placement: the world seed's own spawnpool 210 spawns creature 132 "AFS
  Quartermaster Caufield" (name 2992, level 10, class 29423) in shared Alia Das at the supply tent, 1.6 m from the pre-D11
  TaRapedia `/loc`. The missions attach the original dialogue package 133 to that creature and complete at him. His class
  is a plain Redshirt body (client entityclass augmentation list [1]) and no appearance rows exist, so whether retail used
  this body and the client renders him interactable is unverified (`GAP-W2-CAUFIELD`).
- The NPC load was fixed: `CreatureInit` aborted startup with a null-reference on the first mission that names a creature
  whose class has no NPC augmentation (Caufield), and it bound an `npc_package` row only when the class carried that
  augmentation, which silently dropped Caufield's package. Both bindings now follow the mission and package data;
  `CreatureNpcBindingTests` covers the regression.
- Not reproduced: the missions' XP and credit rewards, a real item price, the per-template weapon statistics, the offer's
  presentation (whether the client showed it as a broadcast and greyed Decline), and any capture of the gear tooltips.
  `GAP-W2-*` records each. OD-43 was decided by the agent for the owner and awaits owner review, as do OD-25..OD-42.
- Deployed 2026-09-15 against the live world database: `WildernessClassGear` is applied (2 missions, package 132 -> 133,
  4 condition rows, 2 rules, 12 item templates) and the game now reports `Loaded 14 content rules (130 content rows,
  0 gaps)` and `Successfully authenticated with the Auth server!`. The first deployment of this slice aborted startup on
  the NPC-load defect above; the rebuilt image fixes it and `CreatureNpcBindingTests` guards it.
- Upstream merge, 2026-09-15 (**PR #91**, commit `e64d6a1`, "Send players the regions they stand in…"): the PR was
  **closed, not merged**, upstream; the commit is the only one in the PR branch that our `development` did not already
  carry, and it is now merged here (`fcb744b`). It adds the `map_region` table, `RegionManager` (per-second per-map check,
  `UpdateRegions` sent on change and once on map entry), `MapRegionEntry`/`MapRegionRepository`, the `NavMeshFlags.
  Underground` flag with `NavMeshQuery.IsUnderground`, GM commands (`.regions`, `.region`, `.setregion`), `docs/regions.md`,
  and a preload of **373 volumes on 58 maps**. Provenance: emulator implementation (**supporting evidence**, not proof of
  final retail behaviour). The volumes themselves are *not* original: the original server decided regions from volumes that
  are lost, and no `.map` carries a `GBB_RegionTrigger` entity; the rows are derived from client artifacts
  (`generated.client.uimapmarker` REGION_LABEL positions for surface circles, `generated.client.gamecontextuiradarinfo`
  minimap rectangles negated in z for boxes), i.e. `measured`/`inferred` with the method documented in `docs/regions.md`.
  Confidence: region ids, names and the client's `UpdateRegions` behaviour are client-backed; the *shapes* are
  approximations — label circles use half the distance to the nearest label (60–200 m) and 51 boxes are marked
  underground-only. 21 volumes sit on the Wilderness context 1220 (Alia Das, Ranja/Pinhole Falls caverns, the outpost
  villages); regions with neither a label nor a minimap (e.g. Alia Caverns) have no volume yet. **Unverified**: the
  underground flag cannot take effect with the `.nav` files in this repository, which were built before
  `NavMeshFlags.Underground` — cavern regions report the surface until the navmeshes are rebuilt (the commit says old files
  load unchanged). Also note `20260915120000_Add_map_region.Designer.cs` carries upstream's snapshot, which lacks our
  content-layer tables; the standalone `*ContextModelSnapshot.cs` files auto-merged correctly and are what scaffolding
  reads, but the Designer is inconsistent with the convention the other migrations follow.
- **Final live deployment identified (2026-09-15, official patch notes)**: the official patch-notes index
  (`playtr.com/news/patch_notes/index.html`, last pre-shutdown capture) lists every deployment: **D11 = 2008-08-15**
  ("brand new Tutorial" — the rebuilt boot camp our S1–S6/W1 slices reconstruct), D12 = 2008-09-18 ("new version of the
  tier selection process" — the feature our W2 slice implements), D13 = crafting, **D14 = 2008-11-11**, D15 = 2008-12-13
  (Empire Sector), D16/D16.4 = 2009-02-09 (mechs, new drop-package items) and **D16.5 = 2009-02-17, the last deployment
  before shutdown**. That matches the client revision this stack targets (1.16.5.0) and fixes the shorthand: `pre_d11`
  means before the rebuilt tutorial, and **values dated 2008-08-15 … 2009-02-17 are the final-era values** (what the bulk
  research's tarapedia tooling flags as `post_d11`).
- **Final-era mission facts from those notes, needed by the "every mission" goal**: D14 **added** the repeatable
  "War Machine" in Raksha Robotics Factory and **removed "Artificial Iniquity"** (so it must not be seeded as live),
  and added the level-50 "A Mystery Unearthed" (Archaeologist Wynne Topper, Twin Pillars); D15 added "Welcome Home
  Soldiers!" and the repeatable "Rapture" (Captain Pauly Seminario / General Frank E. Murphy, Empire Sector, AFS
  Shocktrooper Suit rewards). The client's own mission table is the authority on which missions exist in the final build;
  the notes explain the history. The same notes publish a **resistance rework** (D14: diminishing returns, 10 → 16.67%
  … 250 → 83.33%; resistance skill 30 s → 60 s; Polarity Field -10/pump; resist modules 5 → 10 per rank) that the
  emulator does not implement yet — recorded as a combat-fidelity target. Research record:
  `research/20260915-aliadas-hub/work/official-deployment-notes.md`.
- Client login path, 2026-09-15: the realm's launcher (`banshee-realm-client`) expects the Tabula Rasa auth
  server on **2116** (`TabulaRasaLaunchPlan.DefaultAuthPort`, `docs/MARVEL_HEROES_TABULA_RASA.md`), while Rasa.NET's own
  default is **2106** (`src/Rasa.Auth/appsettings.json`). The compose file now publishes both to the same listener, and
  the owner's client got through: the launch command is `/NoPatch /AuthServer=tabularasa.bansheerealm.com:2116`, which
  resolves to the server's public address. `Rasa.Auth.Client.HandlePacket` now logs every received client opcode (the
  conversation is short and a stalling client looked identical to a silent one); the observed flow is
  `Login` → `ServerListExt` (`LoggedIn`) → `AboutToPlay` (`ServerList`) → the account is redirected to the queue of
  server 234 → the game accepts the client and creates the character's instance. The `SCCheck`/`SCCheckReq` pair stays
  unimplemented, but the real client does not send it on this path.
- Deployment note: the running game container carried an **ad-hoc navmesh mount** that `docker-compose.yml` never
  declared, so recreating the container from the file dropped it and the server fell back to straight-line creature
  movement. The compose file now declares `./navmesh:/app/navmesh` (the repository copy is byte-identical to the one that
  was mounted) and the log confirms `Loaded navmeshes for 76 of 78 maps`.
- Deployment note: recreating the compose network re-assigns container IPs, and both servers parsed
  `CommunicatorConfig.Address` with `IPAddress.Parse`, so the 2026-09-15 network recreation left the game dialling the
  auth container's old address. The configuration now carries the compose service name `auth`, and
  `Rasa.Networking.NetworkAddress` resolves it (literal IPs still parse first, so old configs stay valid);
  `NetworkAddressTests` covers both paths. The untracked `appsettings.env.json` was repointed from 192.168.16.2 to `auth`
  and the previous copy is in `/home/blizz/backups/rasa-net/20260915T005518Z-retail-class-gear-w2`.

## 2026-09-16 UTC — PvP control points: the client's table decoded, the wire format corrected

- The `controlpointdata` row is now a decoding, not a reading: `client/gameuiutil.pyo` `GetControlPointLabel` /
  `GetShortControlPointLabel` / `SortControlPointList` (lines 2228-2264) unpack it as
  `(typeId, nameId, mapTemplateId, level, sortOrder)`. `typeId` is `controlpointownershiptype`, `nameId` a `uielement`
  id, `mapTemplateId` a `maptemplate` id joined to a context through `gamecontext`. Twelve of the 17 rows are the two
  final-live battlegrounds (`adv_wargame_provinggroundsv002` 2361, `adv_wargame_edmundrange2` 2374: Whiskey, Charlie,
  Echo, Blue Base, Red Base, and Edmund Range's East and West Depots, level 50); five are test-map rows. Full record
  with hashes and line numbers: [pvp-control-point-client-evidence.md](pvp-control-point-client-evidence.md).
- **Defect corrected**: `ControlPointStatusPacket` (814) wrote one bare `ControlPointStatus` struct with no argument
  tuple; the client's `Recv_ControlPointStatus(statusList)` takes one list and iterates it. It now writes
  `(statusList,)`, and `ControlPointStatus` carries `ownerId` as the nullable long `shared/controlpointdefs.py`
  declares, with the four `kCPState_*` ids typed. `RequestControlPointStatus` (817), which had no handler, is
  answered with the channel's points - a faithful pair whose only client reader is the dead challenge-board window;
  the live battleground UI takes its points from `ScoreBoardGameScore`'s `cpData` and from CONTROL_POINT map markers
  `(ownerTypeId, ownerId)`, both part of the unbuilt lifecycle. `SetOwnerId` (884) exists as a packet. `UsePacket` writes the extra arguments
  `Usable.Recv_Use(*args)` accepts (declared before, never written). `ControlPointDataTests` (7) pass under net5.
- **Not reproduced, recorded as gaps**: the points' positions (the client maps of both battlegrounds carry no
  control-point entity), capture rules and war timings, the battleground team/scoreboard/win lifecycle (protocol
  recovered in the evidence doc's section 4, no server side), and the mech server side (`mechpad`, `MORPH_MECH` 457,
  pad states 216-219 recorded). `ControlPointManager` answers with every point unheld and `New`, spawns nothing on the
  battleground maps, and refuses an owner change for a point the map has not. The emulator's one Wilderness PvE
  control point (class 3814 at (197.66, 162.27, -54.08), status id 215) is emulator-authored - neither is in the
  client map or table - and is now labelled as such (`GAP-W3-PVE-CONTROL-POINT-PLACEMENT`) rather than removed.
- Deployed 2026-09-16 23:36 UTC. The candidate image ran the whole suite with no network and no database mounts:
  **905 of 907 pass**, the two failures being `EveryCampPlacementStandsOnTheWalkableSurface` and
  `EveryWorldPositionStandsWhereABodyCanWalk`, which need the `navmesh` folder the Dockerfile does not copy and fail
  identically in the pre-change image (both pass on the host tree with `rasaworld.db` present, where the suite is
  1012/1012). The image's `src` and `docs/evidence` hash identically to the reviewed workspace. Backups with
  `PRAGMA integrity_check` = ok are in `/home/blizz/backups/rasa-net/20260916T233502Z-retail-pvp-control-points/`,
  the previous image is kept as `rasa_net:before-retail-pvp-control-points-20260916`, and only `game` was recreated -
  `docker compose up -d --no-deps --no-build game` worked this time without the network workaround the earlier
  deploys needed, and `auth` was not touched (same container since 22:19 UTC, 0 restarts). The game reports
  `Server ready!`, `Loaded navmeshes for 76 of 78 maps`, `Loaded 16 content rules (239 content rows, 0 gaps)`,
  `Connected to the Auth Server!`, 0 restarts and no error or exception lines.
- One manifest correction found by the deploy's own gate: the wire-format fix was first written into the manifest's
  `changes` array, which the schema reserves for content-row changes with a migration and object citations. A packet
  shape is not a content row, so the entry is removed; the corrected gap entry and this log carry the record.
  `RealManifestsAndCompanionFilesPass` and `RealObservedCitationsResolveAgainstTheRealFootageEvents` pass again.
- Also learned: the challenge-board window (clan bidding on control points) is dead code in the final client, and
  `battlegroundrulestype` names a single ruleset, `EDMUND_RANGE`. A survey of the owner's Alienware found the same
  1.16.5.0 client twice, toolkit map renders of both battlegrounds, and no battleground or mech footage.

## 2026-09-16 UTC — Two live findings: the auth handoff after a redeploy, and mission 1992

- **Players hang at "authenticating" after the game container is recreated.** Auth keeps a stale game-server
  registration for the replaced container - it never logs a disconnect for it, and the new game server's
  registration does not take - so authenticated clients have nowhere to be handed to. The game's own log is
  misleading here: `Connected to the Auth Server!` is only the TCP connect. The fix is to restart auth after the
  game redeploy and confirm both sides: auth must log
  `The Game server (Id: ..., Address: ..., Public Address: ...) has authenticated! Requesting info...` and the game
  `Successfully authenticated with the Auth server!`. The game retries every ~10 s, so restarting auth is enough.
  Observed today: game recreated 23:36, auth's last registration 22:19, clients connecting and dropping in under
  70 ms until auth was restarted at 23:42, after which registration succeeded in 5 s.
- **Mission 1992 "Gearing Up for Battle" cannot progress** (`GAP-S2-GEAR-OBJECTIVES`, reported from live play). The
  placements work: the supply crate dispenses its item set and the two dummies stand correctly. But no content rule
  targets the mission's gear objectives, so a player takes the gear, equips it, and the log never moves. Closing it
  needs three `ContentRuleEvent` kinds the engine does not have - a usable-used/looted event, an item-equipped
  event, and a placement-damaged event (`PlacementDestroyed` is not it: the dummy restores after 930 ms instead of
  dying). This is an unimplemented slice, not a regression from the control-point deploy.
  *Superseded 2026-09-17: this diagnosis was wrong - the bindings and their runtime paths existed; the crate's loot
  window was empty. See the next entry.*
- **The endgame-zone wall is structural, now with numbers.** No client map places a single creature spawner or NPC -
  zero entities of any class carrying augmentation 61, 68 or 52 across all fifteen level-banded adventure zones,
  the Wilderness included - and the world seed's 218 spawn pools are all in context 1220. Creature placement was
  entirely server-side and none of it survives, so the zones above the Wilderness need the reconstruction rules
  (OD-45, OD-48) applied at scale rather than any new mechanic.
- **Mechs: the pads were never shipped content.** Only two entity classes carry the MechPad augmentation (83),
  30421 `TEST_KGS_Mechpad` and 30464 `UsableOwnableMechStation`, and the shipped English strings name **both** of
  them "Testing Mechpad"; both have an all-null `usabledata` row, and no client map places either. What did ship
  for players is the Mech PAU line - 26892 `PAU_Vehicle_AFS_MECH` ("Mech PAU"), `Weapon_PAU_AFS_Mech_MiniGun_Physical`
  and `_Laser`, `Ability_PAU_AFS_Mech_Sprint`, and a complete `Shield_Vehicle_Mech_{Light,Medium,Heavy}_{30..50}`
  ladder - alongside the mech NPC/vehicle classes (`Vehicle_AFS_Mech`, `NPC_Vehicle_AFS_Mech`, the Hominis Machina
  family). So the D16 note that mechs were "usable only on Edmund and only from mech pads" is not reflected in the
  client's pad data, and the mech work should start from the PAU vehicle and its shield ladder rather than from the
  pad augmentation (`GAP-W3-MECH-SERVER-SIDE`).

## 2026-09-17 UTC — Mission 1992 re-diagnosed: the supply crate's window was empty, not ruleless

- **The 2026-09-16 diagnosis of `GAP-S2-GEAR-OBJECTIVES` was wrong.** The bindings for 1992/1 (loot_all on crate
  198651), 1992/2 (equip, any item), 1992/3 and 1992/8 (hit on dummies 198652/198653, the second with action 194)
  are in `npc_mission_objective_binding`, were seeded by `BootcampS2GearingUp`, load with 0 gaps, and their runtime
  paths were already tested. The crate is gated by `content_condition` 198800 (ObjectiveStateIs 1992/1 Incomplete),
  so it cannot be looted before the objective is revealed. No new `ContentRuleEvent` kinds were needed.
- **What the character database says.** Character 5 on 2026-09-17: mission 1992 objective 4 status 2, objective 1
  status 1. Its inventory is items 23-27 only - 145 in the ability drawer, 13126/13186/13156 equipped, 28 x65 -
  all created 2026-09-15 17:01:10, i.e. the creation kit, and nothing from item set 19858 (13066, 13096, 13156,
  13186, 13713). The "gear" the player equipped was the starter armour; the crate handed over nothing.
- **The actual defect, and why the crate window showed nothing.** `OpenContentContainer` built the window's rows
  as `new LootItem(templateId, 0, quantity, owner, 0)`: a fresh entity id with no item behind it, and no
  `ItemInfo` sent. The client's `corpselootwindow` resolves every row with `GetEntity(itemId)` before drawing it and
  skips one that comes back `None` (research `client-code/verify/dis/trpython-client-ui-corpselootwindow.pyo.dis`
  lines 316, 373, 596, 663), so the window listed nothing to take. The corpse path had done this right since
  06b3352 (2026-09-13, `RequestCorpseLooting` sends the items before the window); the content path of 6e5f5b0
  (same day, later) did not. And the window's right-click path - `RequestLootItemFromCorpse(entityId, itemId,
  destSlot)`, one row at a time - was routed only to `LootDispenserManager`, never to content containers.
- **The fix.** A content container now holds real items, created once per owner from its item set and introduced
  to the client with `SendItemDataToClient` before `LootInfo`/`CanLootItems`, as the corpse path does.
  `RequestLootItemFromCorpse` on an entity in `ContentUsables` routes to
  `RequestLootItemFromContentContainer`; a row that is gone or was never there is answered with `TakenInfo`, since
  it is still on the asker's screen; a row that does not fit shows `PmInventoryFull`. Loot All takes every row that
  still fits, like the corpse dispenser, instead of the earlier all-or-nothing transaction. The loot_all binding
  completes when the container is empty by either path. Tests: `ContentContainerRowsAreRealItemsIntroducedBeforeTheWindowOpens`
  (every row is a registered item and its `ItemInfo` precedes `LootInfo`),
  `TakingContainerItemsOneAtATimeCompletesTheObjectiveOnTheLastOne`, `TakingAnUnknownOrAlreadyTakenContainerRowChangesNothing`,
  `LootAllTakesWhatIsLeftAfterSingleTakes`; `LootAllIsRefusedWhenTheInventoryCannotTakeEverything` became
  `LootAllTakesNothingWhenNothingFits`. Under .NET 5 (sdk:5.0 container, `--no-incremental`): 909 of 911, the two
  failures the navmesh/`rasaworld.db` audits that need files the scratch tree does not carry.
- **Not established:** whether the original completed the objective when the crate was emptied one row at a time.
  The footage shows only Loot All (A3-017 to A3-024); the one-at-a-time completion is the emulator's choice, and
  the manifest entry says so.

## 2026-09-18 UTC — Deploy: the crate window (commit 31a5a88)

- Candidate `rasa_net:candidate-crate-window-20260918`; the suite ran **918 of 918, nothing skipped**, with the
  world database and the navmesh mounted. Previous image kept as `rasa_net:before-crate-window-20260918`.
- Integrity-checked backups in `/home/blizz/backups/rasa-net/20260918T060857Z-crate-window/`. No database change:
  this deploy is code only.
- Deploy order as before: recreate game (06:09) → restart auth (06:09:37) → restart game. Both handshake lines
  postdate the auth restart at 06:10:00. Nobody was online. After it: `Loaded 16 content rules (249 content rows,
  0 gaps)`, no error lines, only mission 321 unoffered.
- A caveat for the next crate open: the five items the failed 05:43 open created (item ids 61-65) are orphaned in
  `items` with no `character_inventory` row, and the container is per session, so the next open builds five new
  ones. They are harmless, and `PlayerHolds` means the two templates the player already wears from their creation
  loadout will not need taking again.

## 2026-09-17 UTC — Deploy: twelve NPCs given their dialogue (commit 1a83292)

- Candidate `rasa_net:candidate-dialogue-binding-20260917`; the suite ran **918 of 918, nothing skipped**, with
  the world database and the navmesh mounted. Previous image kept as `rasa_net:before-dialogue-binding-20260917`.
- Integrity-checked backups in `/home/blizz/backups/rasa-net/20260917T231510Z-dialogue-binding/`.
- `WildernessDialogueBinding` applied by `dotnet ef database update` against a copy and swapped in with the game
  stopped; afterwards the live world carries all twelve `npc_package` rows and `PRAGMA integrity_check` is ok.
- Deploy order as before: stop game → swap the world → recreate game → restart auth (23:15:44) → restart game.
  Both handshake lines postdate the auth restart at 23:16:07. Nobody was online. After it: `Loaded 16 content
  rules (249 content rows, 0 gaps)`, no error lines, only mission 321 unoffered.
- **Fifteen objectives over nine missions stop being dead ends**: 421/3, 427/1, 431/1, 431/2, 431/3, 442/1,
  444/1, 451/1, 451/2, 549/1, 682/2, 682/4, 682/5, 682/6 and 698/1. The audit's recorded set is down from 34 to
  19.

## 2026-09-17 UTC — Deploy: the respawn the player never saw, and the floor (commit 88be25f)

- Candidate `rasa_net:candidate-respawn-floor-20260917`; the suite ran **918 of 918, nothing skipped**, with the
  world database and the navmesh mounted. Previous image kept as `rasa_net:before-respawn-floor-20260917`.
- Integrity-checked backups in `/home/blizz/backups/rasa-net/20260917T223912Z-respawn-floor/`.
- The world database was migrated by `dotnet ef database update` against a copy (`WorldPlacementFloorSnap`) and
  swapped in with the game stopped; placement 198674 reads 110.23 and 199603 reads 223.48 afterwards, as the
  migration's own rows say. `PRAGMA integrity_check` ok.
- **Mission progress was cleared at the owner's request** ("can you restart the quests?"): all of
  `character_mission`, `character_mission_objective` and `character_mission_objective_counter`, which held six
  mission rows and ten objective rows for the three live characters and three deleted ones. Initiation is a forced
  radio offer on entering the camp, so the chain starts over by itself.
- Deploy order as before: stop game → swap the databases → recreate game (22:40:04) → restart auth (22:40:16) →
  restart game. Both handshake lines postdate the auth restart — auth *"has authenticated! Requesting info..."* and
  the game *"Successfully authenticated with the Auth server!"* at 22:40:38 — with the expected failed attempt at
  22:40:09 while auth was mid-restart. Nobody was online. After it: `Loaded 16 content rules (249 content rows, 0
  gaps)`, 76 of 78 navmeshes, no error lines, and only mission 321 unoffered.

## 2026-09-17 UTC — Deploy: hospital coverage on 41 maps, and mission 429 offered (commit fca47bb)

- Candidate `rasa_net:candidate-hospital-coverage-20260917` ran the whole suite with the world database and the
  navmesh mounted: **918 of 918, nothing skipped** — the first run in which both `MissionLinkAuditTests` checks
  actually execute (they look for `rasaworld.db` beside the `navmesh` folder, which no test container carried, so
  they had been reported *inconclusive* every time). The previous image is kept as
  `rasa_net:before-hospital-coverage-20260917`.
- Integrity-checked backups (`PRAGMA integrity_check` = ok for all three) in
  `/home/blizz/backups/rasa-net/20260917T190310Z-hospital-coverage/`.
- **The world database needed a repair, not just a migration.** `WildernessPinholeNpc` was deployed on its first
  build, before the objective transition row was added to it, so the live database carried the migration as applied
  and mission 429 still logged *"not offered, definition incomplete: required objective 4 is never revealed"*. The
  migration was reverted and re-applied with `dotnet ef database update` against a copy, and the copy's full
  `.dump` differs from the live one by exactly one line — `INSERT INTO npc_mission_objective_transition
  VALUES(429,5,4)` — which is what was swapped in.
- Deploy order as the 2026-09-16 entry requires: stop game → swap the database → recreate game (19:50:19) →
  restart auth (19:50:52) → restart game (19:51:06). Both handshake lines postdate the auth restart — auth *"has
  authenticated! Requesting info..."* 19:51:14.991 and the game *"Successfully authenticated with the Auth
  server!"* 19:51:15.000 — and the game's first attempt at 19:50:45, while auth was mid-restart, failed exactly as
  that entry predicts. Nobody was online.
- After the deploy: `Loaded 16 content rules (249 content rows, 0 gaps)`, `Loaded navmeshes for 76 of 78 maps`,
  373 region volumes, 141 map links, no error lines. **The "Mission 429 is not offered" line is gone**; only 321
  remains, which has no giver in the client tables at all.

## 2026-09-17 UTC — Deploy: the supply crate fix (commit a117311)

- Candidate `rasa_net:candidate-retail-crate-loot-20260917` (sha256 f134d7e3…) ran the whole suite with no network and no
  database mounts: 909 of 911, the two failures the navmesh/`rasaworld.db` audits that need files the Dockerfile does
  not copy; with the database present those two pass (21 s, in the sdk:5.0 iteration container). The image's `src`,
  `docs/evidence` and solution file are byte-identical to the reviewed workspace.
- Integrity-checked backups (`PRAGMA integrity_check` = ok for all three) in `/home/blizz/backups/rasa-net/20260917T010527Z-retail-crate-loot/`; the previous image is kept as
  `rasa_net:before-retail-crate-loot-20260917` (0132e6f6). Compose `--dry-run` named only `game`, so plain
  `docker compose up -d --no-deps --no-build game` recreated it (01:08:00 UTC); nobody was online (last client left
  00:46:34).
- **The auth hand-off needs one more step than the 2026-09-16 entry says.** After the recreate, auth was restarted at
  01:08:44 as prescribed - but the game did *not* re-register: it had logged `Could not connect to the Auth server!
  Trying again in a few seconds...` at 01:08:43 (auth was mid-restart) and then nothing for four minutes, and auth
  showed no new game-server connection. Restarting `game` at 01:13:21 fixed it in 19 s: auth
  `has authenticated! Requesting info...` and the game `Successfully authenticated with the Auth server!` both at
  01:13:40, `Server ready!` 01:13:42, `Loaded navmeshes for 76 of 78 maps`, `Loaded 16 content rules (239 content
  rows, 0 gaps)`, 0 restarts, no further error lines. So the order is: recreate game → restart auth → restart game →
  confirm both log lines. Checking only the game's first `Successfully authenticated` (which predates the auth
  restart) is not enough.

## 2026-09-25 — supplied footage ledger

Every timestamp below is from `docs/evidence/gameplay-footage-playlists-20260924.json` or `docs/evidence/footage-fanout-20260925/`. Missing evidence is not 100% retail accuracy. Public-test, beta, and E3 2006 rows are boundaries and are not final-live rules. Upload dates are not recording builds.

- cite:A4udsM0rcLo@30 gap: Three squad status panels and several armed characters fighting near a sandbag line. This observation was not implemented as a server change.
- cite:A4udsM0rcLo@90 gap: Player fires during an outdoor fight; fallen enemies and a loot glow are visible. This observation was not implemented as a server change.
- cite:A4udsM0rcLo@180 gap: Friendly nameplates cluster at a structure entrance while the player fights. This observation was not implemented as a server change.
- cite:A4udsM0rcLo@300 gap: Player fights behind two visible allies inside a passage. This observation was not implemented as a server change.
- cite:A4udsM0rcLo@420 gap: Several friendly characters engage a target labelled Prototype Forean Machina in the Production Chamber. This observation was not implemented as a server change.
- cite:-I1WM5ddg-k@600 gap: Player pauses travel at the barracks interface. This observation was not implemented as a server change.
- cite:Of3Uxw9MNec@60 gap: Player advances toward a control point amid combat. This observation was not implemented as a server change.
- cite:Ik3CErtdVHU@30 gap: Characters gather around a boxing event. This observation was not implemented as a server change.
- cite:EiE2oodlP8A@20 boundary: public-test. not a final-live rule. gap: Rows of recruits perform exercise motions before a large door. The uploader claim says this is the D11 rebuilt boot camp on the Public Test Server, and the observation is unverified as PTS only, so the final live count, layout, and schedule stay unknown.
- cite:kmL7t3rnzpA@9 gap: Several players idle and use emotes together. This observation was not implemented as a server change.
- cite:-I1WM5ddg-k@1488 reconciled: seeded mission 1390 Conscientious Objector completes at Warrior Apirka for 2,000 XP. gap: the two selectable item templates are unidentified, and the recording date is unknown.
- cite:-I1WM5ddg-k@1492 reconciled: the on-screen +2,000 XP matches the seeded 1390 reward. gap: the item templates and the recording date remain unknown.
- cite:-I1WM5ddg-k@1495 reconciled: seeded missions 1392/1393 are Conscientious Objector - Part Two from Apirka toward Rogers. gap: the footage does not show which id is on screen, and the recording date is unknown.
- cite:-I1WM5ddg-k@1510 reconciled: accepting that offer is the seeded Apirka handoff. gap: the clip does not show Rogers or a reward, and the recording date is unknown.
- cite:A4udsM0rcLo@30 gap: Wilderness Targets of Opportunity tracker shows Kill 40 Miasmas, Kill 40 Xanx and Kill 30 Shield Drones. This observation was not implemented as a server change.
- cite:A4udsM0rcLo@300 gap: The location label reads Pravus Interior Halls; Thrax Technicians are visible. This observation was not implemented as a server change.
- cite:A4udsM0rcLo@420 gap: The location label reads Pravus Production Chamber; target label reads Prototype Forean Machina. This observation was not implemented as a server change.
- cite:x_ujwmKj8jA@1 gap: Letterboxed cinematic still of a figure at a glowing circular interface facing a machine in a forest. The same still is held at 82s. No HUD, cursor, or camera motion. This observation was not implemented as a server change.
- cite:x_ujwmKj8jA@82 gap: Middle spectrogram is continuous harmonic music with a steady pulse. Silence appears only as about 1.5s at the end. No speech or combat transients in that window. This observation was not implemented as a server change.
- cite:jhw3kEPHZv8@1 gap: Held letterboxed still of a robed figure with a purple-flamed staff beside sandbags, a second armored figure, pines, and water. A small corner mark is present. No HUD. This observation was not implemented as a server change.
- cite:jhw3kEPHZv8@76 gap: Spectrogram is sustained harmonic music. Description says only 'Valverde Plateau track.' This observation was not implemented as a server change.
- cite:BMUI6Akuv9E@1 gap: Static winged Tabula Rasa logo on a plain gray field, held across the sampled stills. No gameplay image. This observation was not implemented as a server change.
- cite:BMUI6Akuv9E@103 gap: Spectrogram is rhythmic harmonic music. Description calls it one Foreas track variation. Audio continues until a silent tail. This observation was not implemented as a server change.
- cite:iNtYRWv_Fs0@1 gap: Held promotional wallpaper: a rune-lit tree, a machine, and a calendar grid. The top-left text reads tabularasavault.ign.com. No game HUD. This observation was not implemented as a server change.
- cite:iNtYRWv_Fs0@98 gap: After the quiet open, the middle spectrogram is sustained harmonic music. Description calls this the Final variation of the Foreas track. This observation was not implemented as a server change.
- cite:2_knJpz5QbU@1 gap: Held product photograph of a Richard Garriott's Tabula Rasa collector-style box, discs, a field guide, and a letter. The login or main-menu UI is not on screen. This observation was not implemented as a server change.
- cite:2_knJpz5QbU@43 gap: Description says 'Track from the TR login screen.' The middle spectrogram is continuous harmonic music, consistent with a music rip rather than UI clicks. This observation was not implemented as a server change.
- cite:wg4qYJpOJ-M@105 gap: Slideshow of held images, not a moving camera. Sampled cards include an Arieki concept painting (1s), a waterfall settlement (45s and 80s), a first-person still with a weapon and t This observation was not implemented as a server change.
- cite:wg4qYJpOJ-M@120 gap: Spectrogram is continuous harmonic music. Description offers megaupload.com/?d=ZTPRWLEV as a 293MB all-soundtracks pack. This observation was not implemented as a server change.
- cite:aOtqXKj69Yc@1 gap: Held promo render of a goggled woman with a pistol and the winged logo. No HUD or environment. This observation was not implemented as a server change.
- cite:aOtqXKj69Yc@69 gap: Spectrogram is sustained harmonic music. Description is 'Tabula Rasa - AFS Outpost - Soundtrack.' No PTS, beta, or E3 wording. This observation was not implemented as a server change.
- cite:RGWKETi_1bw@1 gap: Held winged logo card. A 2007 NCSoft copyright line is visible in the corner. No base geometry and no HUD. This observation was not implemented as a server change.
- cite:RGWKETi_1bw@66 gap: Spectrogram is harmonic music. Description is 'Tabula Rasa - AFS Base - Soundtrack.' This observation was not implemented as a server change.
- cite:H2vuIiIamU8@1 gap: One held promo render of three armed human figures and the winged logo for the whole file. No Forean character, no HUD. This observation was not implemented as a server change.
- cite:H2vuIiIamU8@106 gap: Spectrogram is continuous harmonic music. Description is 'Tabula Rasa - Forean - Soundtrack.' This observation was not implemented as a server change.
- cite:Jvug1Bn2YYE@1 gap: Held promo render of a tall armored non-human figure with a bladed staff, the winged logo, and www.RGTR.com. Corner copyright reads 2007 NCSoft. No camp and no HUD. This observation was not implemented as a server change.
- cite:Jvug1Bn2YYE@70 gap: Spectrogram is sustained harmonic music. Description is 'Tabula Rasa - Forean Encampment - Soundtrack.' This observation was not implemented as a server change.
- cite:N6NMlvij7SQ@1 gap: Held logo over a split landscape painting, green waterfall on one side and lava on the other. No creatures, HUD, or play. This observation was not implemented as a server change.
- cite:N6NMlvij7SQ@83 gap: Spectrogram is dense sustained harmonic music. Description is 'Tabula Rasa - Cormans - Soundtrack.' This observation was not implemented as a server change.
- cite:Z9fn31_ht_I@24 gap: Short card sequence: a Klintonix logo, a logo card reading Do you miss "Tabula Rasa" soundtrack?, a card telling the viewer to use the description link to download all soundtracks  This observation was not implemented as a server change.
- cite:Z9fn31_ht_I@18 gap: A music bed plays under the cards. The description link is https://www.box.com/s/n939ibijs9jeet9h3vns. That archive was not downloaded. This observation was not implemented as a server change.
- cite:NtogPR3B9rI@0 boundary: e3-2006. not a final-live rule. gap: The clip is a photo of a monitor in a room. A third-person humanoid in red-orange armor is on a rocky forest path, seen from behind and slightly above, with a circular minimap at t
- cite:NtogPR3B9rI@12 boundary: e3-2006. not a final-live rule. gap: The same armored figure holds a long rifle across the body while a bright muzzle-side flash is in front of the weapon. A red bar sits over a small figure farther up the slope. The 
- cite:NtogPR3B9rI@20 boundary: e3-2006. not a final-live rule. gap: Between the 8s, 20s, and 32s samples the rifle figure remains in the lower center while the trail, rocks, and trees scroll, which is a following camera during travel rather than a 
- cite:jNxEdYl6fr4@0 boundary: e3-2006. not a final-live rule. gap: Photographed monitor. Third-person view behind an orange-armored humanoid on open orange-brown ground with dead trees. Bottom unit frames and a round minimap are present.
- cite:jNxEdYl6fr4@12 boundary: e3-2006. not a final-live rule. gap: Two large translucent green domes sit in the midground. Smaller humanoid silhouettes stand between the camera character and the domes. A pale arc is in the sky. The camera characte
- cite:jNxEdYl6fr4@16 boundary: e3-2006. not a final-live rule. gap: The camera character's arms are raised in front of the body while one green dome remains to the right and a small figure is nearer the center. Red bars are over at least one distan
- cite:3reasGu9M9c@0 boundary: e3-2006. not a final-live rule. gap: A Dell monitor bezel is in frame. The game view is a dark interior of repeating hexagonal wall pods, several with green lit panels and a few with orange fire. A red-capped figure i
- cite:3reasGu9M9c@16 boundary: e3-2006. not a final-live rule. gap: The view has moved into a darker corridor. The same red-capped figure remains low in frame, so the camera is still following rather than cutting to a fixed shot.
- cite:3reasGu9M9c@28 boundary: e3-2006. not a final-live rule. gap: The character is in a chamber packed with angular green-glowing shapes. Combat is presented as dense colored light in a tight interior, not as an outdoor shootout.
- cite:2mOneHr2pJA@0 boundary: e3-2006. not a final-live rule. gap: Show-floor monitor with a neighboring screen at the right edge. Third-person view in a dark organic forest. A figure is low in the frame and taller pale figures stand ahead on the 
- cite:2mOneHr2pJA@12 boundary: e3-2006. not a final-live rule. gap: The camera is still behind a central armored figure in a brighter alien forest of curved trees. A red bar is over a creature-like shape to the right, so combat targeting is present
- cite:2mOneHr2pJA@40 boundary: e3-2006. not a final-live rule. gap: The followed character is in the lower center amid red ground effects and dark foliage. Across the minute the camera keeps that behind-the-body framing while the terrain changes, w
- cite:b-WhHUWv8WA@0 boundary: e3-2006. not a final-live rule. gap: Dark interior. A huge red wireframe sphere dominates the upper center. At least two humanoids are low in the frame, one nearer the camera and one in bright green to the right. Bott
- cite:b-WhHUWv8WA@8 boundary: e3-2006. not a final-live rule. gap: The red wire sphere is still large in view and a green cloud or beam is on the left. The group is still clustered under the effect rather than spread across a wide field.
- cite:b-WhHUWv8WA@20 boundary: e3-2006. not a final-live rule. gap: The camera has moved down a ribbed organic corridor with fire ahead. A green-clad figure is in front of the camera character. A line of light text is centered on the screen but is 
- cite:b-WhHUWv8WA@32 boundary: e3-2006. not a final-live rule. gap: The followed character is on a sloped floor in the same dark complex, still third person, with the green figure nearby. Locomotion reads as the camera trailing a walking or running
- cite:v1GogJZ9wxQ@0 boundary: e3-2006. not a final-live rule. gap: Interior room with shelves and a large blue text window on the left. Two humanoids stand close together near the center, one slightly behind the other, in third person. The blue wi
- cite:v1GogJZ9wxQ@12 boundary: e3-2006. not a final-live rule. gap: A character in a yellow-brown suit is centered, seen from behind, walking out through a rectangular doorway onto a platform. Another smaller figure is farther ahead on the platform
- cite:v1GogJZ9wxQ@24 boundary: e3-2006. not a final-live rule. gap: The same behind-the-back framing continues outdoors along a walled path with trees. The character is moving away from the camera between the 12s, 16s, 20s, and 24s samples.
- cite:TaxGO-rjLxg@0 boundary: e3-2006. not a final-live rule. gap: Title card in red text on black: E3 2006 and Tabula Rasa. This is the clip labeling itself as E3, separate from the June 2006 upload date.
- cite:TaxGO-rjLxg@4 boundary: e3-2006. not a final-live rule. gap: Third-person view in a dark rocky space. The camera character is low in frame. Another armored body lies or crouches ahead with a green marker. A round minimap is at the lower righ
- cite:TaxGO-rjLxg@16 boundary: e3-2006. not a final-live rule. gap: Two dark armored figures stand on a path in a rocky cut, camera behind them. A light-colored objective line is across the upper screen. At 360p it appears to be an objective-comple
- cite:TaxGO-rjLxg@32 boundary: e3-2006. not a final-live rule. gap: A full-height character window is open on the left over the third-person scene: a humanoid paper doll, rows of attributes, and a second red-armored figure still visible in the worl
- cite:TaxGO-rjLxg@48 boundary: e3-2006. not a final-live rule. gap: The character window is gone. An orange-suited figure runs away from the camera along a road toward a large dark gate with a bright blue opening. The same behind-the-runner framing
- cite:TaxGO-rjLxg@64 boundary: e3-2006. not a final-live rule. gap: End card uses the MMOG-Welten web address, matching the sister files 95ls4AKYAUA and t7aX-sACBTw.
- cite:n1ekRiQB1iY@0 boundary: e3-2006. not a final-live rule. gap: Photographed monitor. Third person behind a white-armored humanoid on a bright forest path. The figure holds a long weapon upright. A round minimap and bottom unit frames are prese
- cite:n1ekRiQB1iY@16 boundary: e3-2006. not a final-live rule. gap: The view is in blue-purple brush against rock. Small red marks are in the vegetation. The white figure is still the camera anchor at the bottom when visible in neighboring samples.
- cite:n1ekRiQB1iY@24 boundary: e3-2006. not a final-live rule. gap: The white figure is center-low with blue particle streaks around the body and a bright flash ahead, in a grove of curved trees. Combat is shown as colored particles around the foll
- cite:n1ekRiQB1iY@32 boundary: e3-2006. not a final-live rule. gap: The white figure is on a dirt path moving away from the camera, long weapon in hand, with two or more smaller figures farther up the path. Red bars are over some of those figures.
- cite:95ls4AKYAUA@0 boundary: e3-2006. not a final-live rule. gap: Black title card with red text reading E3 2006 and Tabula Rasa.
- cite:95ls4AKYAUA@4 boundary: e3-2006. not a final-live rule. gap: Clean gameplay capture, not a photo of a monitor. An orange-and-teal armored humanoid is low in frame in a running pose while a second figure higher on the rocks is wrapped in a bl
- cite:95ls4AKYAUA@28 boundary: e3-2006. not a final-live rule. gap: Two armored humanoids stand close together in the foreground, one aiming a large rifle toward a shape on the right slope. They are grouped as a pair in front of the camera rather t
- cite:95ls4AKYAUA@44 boundary: e3-2006. not a final-live rule. gap: A large dark multi-legged or winged creature fills the center with a white-blue flash at its base. A smaller figure is in front of it. Red bars sit over parts of the creature. Comb
- cite:95ls4AKYAUA@60 boundary: e3-2006. not a final-live rule. gap: Camera returns behind a blue-purple haired or helmeted figure in the forest, still third person, with other bodies and red bars ahead. Locomotion across the clip is repeated behind
- cite:95ls4AKYAUA@76 boundary: e3-2006. not a final-live rule. gap: End card shows www.MMOG-Welten.de.
- cite:t7aX-sACBTw@0 boundary: e3-2006. not a final-live rule. gap: Black title card with red text reading E3 2006 and Tabula Rasa.
- cite:t7aX-sACBTw@4 boundary: e3-2006. not a final-live rule. gap: Forest floor, third person behind a yellow-helmeted figure. Two nameplated bodies are ahead near a large green translucent dome. Red and blue bars are over those bodies. Bottom uni
- cite:t7aX-sACBTw@20 boundary: e3-2006. not a final-live rule. gap: The yellow-helmeted figure is still the camera anchor, now beside a pale crouching or fallen body and a small mechanical or creature shape, with green triangular markers farther ou
- cite:t7aX-sACBTw@36 boundary: e3-2006. not a final-live rule. gap: Wide combat shot: thick blue and red beams cross the frame between several figures, with a bright impact flash on the right. The camera has pulled back from the tight over-the-shou
- cite:t7aX-sACBTw@40 boundary: e3-2006. not a final-live rule. gap: End card shows www.MMOG-Welten.de and a short German community line.
- cite:R4YJs0GOjB0@0 boundary: public-test. not a final-live rule. gap: Opening card reads Control Point Wargame on Public Test Server, with a song credit. The uploader description calls it a control-point wargame on the public test server, red versus 
- cite:R4YJs0GOjB0@12 boundary: public-test. not a final-live rule. gap: Third-person view behind an armored player inside a sandy structure. Other armed humanoids stand nearby. A target portrait and red bar sit at top center. Bottom-left portrait has r
- cite:R4YJs0GOjB0@24 boundary: public-test. not a final-live rule. gap: The player runs across open ground with a blue glow at the feet and the camera held behind the character. Mission Tracker is in the top right. A circular minimap with a zone label 
- cite:R4YJs0GOjB0@36 boundary: public-test. not a final-live rule. gap: A large translucent red dome covers several figures. The player is in the foreground with a weapon raised. Hostile red bars are visible past the dome.
- cite:R4YJs0GOjB0@60 boundary: public-test. not a final-live rule. gap: The player stands aiming at a stationary turret-like machine in fog. The target frame is up. The minimap shows a tight cluster of white and red dots.
- cite:R4YJs0GOjB0@84 boundary: public-test. not a final-live rule. gap: Several humanoids and smaller creatures are in one fight. White and red beams cross the group. Red floating numbers appear over targets. Friendly blue nameplates and hostile red ba
- cite:R4YJs0GOjB0@132 boundary: public-test. not a final-live rule. gap: On a raised platform the player aims into a red beam or dome while other players and a large machine fight nearby. The combat log is repeating short system lines.
- cite:R4YJs0GOjB0@180 boundary: public-test. not a final-live rule. gap: In a red-lit corridor the player fires while another armored player and a large bipedal machine are ahead. The minimap is crowded with red and white contacts. The combat log is sti
- cite:LUAPx89dxTw@0 boundary: beta. not a final-live rule. gap: Title card reads End of Beta Event 2007-10-26. The description says this is part two of a montage from that event and points at a part one.
- cite:LUAPx89dxTw@12 boundary: beta. not a final-live rule. gap: Large overlay text reads Bane Hospital Camping and Alia Das over a blue-lit indoor crowd. The HUD is already up: chat, Mission Tracker, target frame, minimap, and the same third-pe
- cite:LUAPx89dxTw@42 boundary: beta. not a final-live rule. gap: A wide indoor view is filled with blue friendly nameplates and a few red hostile bars. The camera character is at the bottom edge rather than in the middle of the group. Players ar
- cite:LUAPx89dxTw@70 boundary: beta. not a final-live rule. gap: A bright horizontal beam fight with many nameplates. The combat log shows short failure lines, including wording consistent with not having enough power, and chat about getting som
- cite:LUAPx89dxTw@130 boundary: beta. not a final-live rule. gap: Outdoors, the player looks along a fenced path. A few other players walk ahead on their own heading. Creatures are off to the side. Chat shows join and leave lines plus player chat
- cite:LUAPx89dxTw@144 boundary: beta. not a final-live rule. gap: On a metal bridge or walkway, many players and creatures fight at once. Blue and red nameplates overlap. The player stands in the foreground while the group engages ahead.
- cite:LUAPx89dxTw@216 boundary: beta. not a final-live rule. gap: The player runs toward a vertical blue beam on a paved pad. Several other players stand around the beam at different facings. The minimap is a dense knot of dots.
- cite:LUAPx89dxTw@324 boundary: beta. not a final-live rule. gap: A paved gathering with many players in different armor colors. Chat includes lines that OCR fragments match to admin or server messages, plus ordinary player chat. People stand in 
- cite:LUAPx89dxTw@396 boundary: beta. not a final-live rule. gap: A very large outdoor crowd, with chat that includes an event call for more people. Players and large creatures share the frame. The camera character is in the foreground, not leadi
- cite:LUAPx89dxTw@432 boundary: beta. not a final-live rule. gap: The clip ends on a bright outdoor crowd and a large white-blue effect, still with the full HUD and a packed minimap.
- cite:bCQgVDS-OJQ@0 gap: Black card with a glowing mark and the credit 'by Spartan Fidelity'. The description says the clip is set to that band rather than a Mad World cut, and links the retail site. This observation was not implemented as a server change.
- cite:bCQgVDS-OJQ@12 gap: Sepia third-person shot of an armored figure approaching a large multi-legged creature in grass. No chat, bars, minimap, or target frame are visible. This observation was not implemented as a server change.
- cite:bCQgVDS-OJQ@24 gap: The figure swings a weapon at tall plant-like or creature forms beside a carved circular structure. The grade stays monochrome. This observation was not implemented as a server change.
- cite:bCQgVDS-OJQ@60 gap: The figure stands before a tall bright column in a dark ruined space, seen from behind. This observation was not implemented as a server change.
- cite:bCQgVDS-OJQ@96 gap: Wide sepia view of disc-shaped structures on a terraced rock face. A small humanoid is on the terraces. No HUD. This observation was not implemented as a server change.
- cite:bCQgVDS-OJQ@132 gap: The figure walks away from the camera across a dark plain toward raised platforms and distant lights, weapon in hand. This observation was not implemented as a server change.
- cite:bCQgVDS-OJQ@168 gap: A bright explosion fills the frame, with the top of a helmet at the bottom edge. This observation was not implemented as a server change.
- cite:bCQgVDS-OJQ@221 gap: From a rise, the figure fires into a dusty courtyard where several other figures are gathered. Still no HUD. This observation was not implemented as a server change.
- cite:bCQgVDS-OJQ@240 gap: Two armored figures stand side by side at a railing, seen from behind, looking at a rock formation. They are close but not in a stacked follow pose. This observation was not implemented as a server change.
- cite:Iiq8oaJXxVc@0 boundary: beta. not a final-live rule. gap: Title card reads Beta Test Characters, with a Spartan Fidelity song credit. The description says these are the uploader's characters from the beta test.
- cite:Iiq8oaJXxVc@24 boundary: beta. not a final-live rule. gap: Third-person run toward red crystal structures. A hostile bar is targeted. The combat log is filling with short yellow gain lines. Bottom-left bars, a weapon label, Mission Tracker
- cite:Iiq8oaJXxVc@36 boundary: beta. not a final-live rule. gap: The same red-accented character stands on stone while several tall creatures approach across a red patch of ground. Greenish name text sits on the creatures. The player is alone in
- cite:Iiq8oaJXxVc@60 boundary: beta. not a final-live rule. gap: A different armor set, blue with a glow, runs through trees toward a creature. The nameplate has changed from the red-accented character. Foot glow returns while moving. Ability ic
- cite:Iiq8oaJXxVc@96 boundary: beta. not a final-live rule. gap: Another character, red cloak, level badge reading in the mid-teens, with Mission Tracker text on the right that is too small to read. The view is over the character toward an empty
- cite:Iiq8oaJXxVc@132 boundary: beta. not a final-live rule. gap: A grey-armored character advances with a green spherical effect and green ground light. The combat log continues. Minimap shows green terrain with white and red dots.
- cite:Iiq8oaJXxVc@168 boundary: beta. not a final-live rule. gap: Another nameplate, level badge again in the mid-teens, firing uphill at creatures with red bars. Gain lines continue in the log.
- cite:Iiq8oaJXxVc@185 boundary: beta. not a final-live rule. gap: Camera sits on the weapon, yellow lights along the barrel, still with the HUD and gain lines. The character is shooting rather than locked to another player.
- cite:Iiq8oaJXxVc@204 boundary: beta. not a final-live rule. gap: A further armor and nameplate, badge reading around 20, fights a creature at a rock face with a white muzzle effect and green impact. Solo framing continues.
- cite:Iiq8oaJXxVc@240 boundary: beta. not a final-live rule. gap: A full-screen location card reads Memory Tree Hill and Concordia Wilderness over ongoing combat. The HUD remains visible behind the card, including a different nameplate whose badg
- cite:Iiq8oaJXxVc@264 boundary: beta. not a final-live rule. gap: Closing card thanks developers, designers, and support personnel and says thanks for a great game.
- cite:A4udsM0rcLo@8 gap: At AFS Preparation Camp the player Zorlac Ripslayer runs behind two squad nameplates. Squad chat says The Means of Production is being shared with Varko Ulliuvenn, that Radmon Aolo This observation was not implemented as a server change.
- cite:A4udsM0rcLo@8 gap: The mission tracker reads Wilderness Targets of Opportunity, Complete All 10 Targets of Opportunity, Kill 40 Miasmas with 8 of 40, Kill 40 Xanx with 28 of 40, and Kill 30 Shield Dr This observation was not implemented as a server change.
- cite:A4udsM0rcLo@88 gap: The zone label reads Frontlines. The player is in the foreground of a sandbag fight, reloading a Teleract Rifle, while squad nameplates are in the melee on the targeted Hominis Mac This observation was not implemented as a server change.
- cite:A4udsM0rcLo@180 gap: Still in Frontlines, the player is running toward a fight in which the squad nameplates are already engaged. This observation was not implemented as a server change.
- cite:A4udsM0rcLo@244 gap: The zone label context is the interior. The player moves alone through a dark cluttered passage. Both squad frames are still filled. No squad body is in view. Chat mentions a Pravu This observation was not implemented as a server change.
- cite:A4udsM0rcLo@292 gap: The minimap label reads Pravus Interior Halls. The player runs down a ramp past a body. The squad frames for Ulliuvenn and Aolon are still present. This observation was not implemented as a server change.
- cite:A4udsM0rcLo@408 gap: The minimap label reads Pravus Production Chamber. Squad nameplates are in a fight ahead of the player, who is still back on the floor. This observation was not implemented as a server change.
- cite:A4udsM0rcLo@548 gap: Still in the Pravus Production Chamber, the target frame reads Prototype Forean Machina. The player is shooting, and the same squad nameplates are in the melee. This observation was not implemented as a server change.
- cite:-I1WM5ddg-k@45 gap: Draco Unicornn walks alone toward a hostile Thrax with the reticle on it. Only one unit frame is on screen. This observation was not implemented as a server change.
- cite:-I1WM5ddg-k@120 gap: The player runs alone across open ground with a Logos window open. A creature is on the ridge ahead, not beside the player. This observation was not implemented as a server change.
- cite:-I1WM5ddg-k@225 gap: The player is in melee with a pack of Thrax, shotgun out, damage numbers on the targets. No second unit frame. This observation was not implemented as a server change.
- cite:-I1WM5ddg-k@400 gap: The player runs alone through a dark stretch. The mission tracker is open. No second character is beside them. This observation was not implemented as a server change.
- cite:-I1WM5ddg-k@600 gap: The player stands at a trainer pedestal with a character training window open and a press-to-talk prompt. Another figure stands off to the side of the camp, not on the player's uni This observation was not implemented as a server change.
- cite:-I1WM5ddg-k@750 gap: Indoors, the player stands still in front of an NPC with a press-to-talk prompt while a training window covers the left side. This observation was not implemented as a server change.
- cite:-I1WM5ddg-k@1260 gap: The player runs alone along a path with a weapon out. A hostile name is targeted farther along the trail. This observation was not implemented as a server change.
- cite:-I1WM5ddg-k@1360 gap: The player stands in melee range of a single large Thrax and fires. Corpses are on the ground. Only the player's unit frame is shown. This observation was not implemented as a server change.
- cite:-I1WM5ddg-k@1488 reconciled: seeded mission 1390 Conscientious Objector completes at Warrior Apirka for 2,000 XP. gap: the two selectable item templates are unidentified, and the recording date is unknown.
- cite:-I1WM5ddg-k@1510 reconciled: accepting that offer is the seeded Apirka handoff. gap: the clip does not show Rogers or a reward, and the recording date is unknown.
- cite:-I1WM5ddg-k@1850 gap: The player runs alone toward one standing NPC with a press-to-talk prompt. This observation was not implemented as a server change.
- cite:-I1WM5ddg-k@2200 gap: The player runs alone toward a blue gate marked Twin Pillars. A single distant figure is inside the gate, not at the player's side. This observation was not implemented as a server change.
- cite:-I1WM5ddg-k@2500 gap: In a paved interior, several other characters stand around while say-lines scroll in chat. The player has a weapon out. They are not stacked on the player as a pair of followers. This observation was not implemented as a server change.
- cite:-I1WM5ddg-k@2700 gap: The player runs alone on a forest path with a hostile targeted ahead. This observation was not implemented as a server change.
- cite:-I1WM5ddg-k@2880 gap: The player stands among corpses under a dropship near a landing structure. Another character is off to the side. A talk prompt is up. This observation was not implemented as a server change.
- cite:Of3Uxw9MNec@2 gap: An uploader title card sits over the footage before the fight continues. This observation was not implemented as a server change.
- cite:Of3Uxw9MNec@16 gap: The player is already in melee beside a tree. Another humanoid is nearby in the fight. Only one unit frame is shown. This observation was not implemented as a server change.
- cite:Of3Uxw9MNec@52 gap: The player runs forward through the fight. The minimap carries a control-point label consistent with the uploader's Imperial Valley CP. This observation was not implemented as a server change.
- cite:Of3Uxw9MNec@100 gap: The player runs up a ramp into the control-point structures while fire and bodies fill the approach. Other characters are in the fight. No second unit frame trails the player. This observation was not implemented as a server change.
- cite:Of3Uxw9MNec@148 gap: The player shoots from behind fallen cover at a target in the trees, still with a single unit frame. This observation was not implemented as a server change.
- cite:EiE2oodlP8A@0 boundary: public-test. not a final-live rule. gap: The clip opens on the Tabula Rasa logo, then a black title card that reads Deployment 11 Bootcamp Mini-Game (Kind of).
- cite:EiE2oodlP8A@14 boundary: public-test. not a final-live rule. gap: A group of recruits in gray exercise in front of a large paneled wall. Many have their arms up in a jumping-jack pose. Several are already prone. One figure in a cap is in the fore
- cite:EiE2oodlP8A@44 boundary: public-test. not a final-live rule. gap: Most of the group is standing while several are still on the ground and one is partway through a push-up.
- cite:EiE2oodlP8A@62 boundary: public-test. not a final-live rule. gap: Nearly the whole group is down in a push-up or prone pose together, with one or two slightly out of phase.

## 2026-09-25 — creation and first login compared with labelled evidence

`spawn.first_login` in `docs/evidence/bootcamp-d11-positions.json` is map 1985 at (387.2, 136.75, -79.09), rotation 2.879793, tier measured. The shipped creation path stores that row as content location 19851 when boot-camp entry is on, and `CharacterRepository.Get` returns the same map, position, rotation, and name on a second load. With entry off, creation still uses the existing wilderness default map 1220 at (894.9, 307.9, 347.1), rotation 0. Those defaults were not given a new footage spawn.

Family name, character name, the five appearance slots, rank-1 skills 1/8/19/49/165, the Lightning and rifle tray, and the starter items already follow `docs/evidence/character-creation-appearance.json`, `docs/evidence/new-character-loadout.json`, and `docs/evidence/race-unlocks.json`. No reward, rate, spawn, movement value, or companion parameter was added. Still open, and left unimplemented: the exact later level and class bonus timing, and the tutorial or skip rewards, recorded in `docs/new-character-client-evidence.md`.
