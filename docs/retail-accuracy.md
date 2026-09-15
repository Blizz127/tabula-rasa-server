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
