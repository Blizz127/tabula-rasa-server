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
