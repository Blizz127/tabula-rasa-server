# Initiation handoff: McAllister movement

Audit date: 2026-09-22. Target: final-live rebuilt boot camp, mission 1990 through acceptance of 1992. This correction does not establish complete fidelity of the opening.

## Defect and correction

`BootcampScriptedMoves` queued a one-node path for McAllister at the 1990 turn-in. His creature row 198500 nevertheless had walk_speed 0; `BehaviorManager.FollowPath` uses that rate directly. The existing test checked only the queued path. Consequently he never departed. Making that path move also exposed its premature trigger: he would leave during the following mission offer.

`BootcampMcAllisterWalk` changes only creature 198500.walk_speed to 2.5 and content_rule 1985014 to MissionAccepted 1992. Its rollback restores the former zero speed and MissionTurnedIn 1990 rule. `CreatureFractionalMovementRates` changes both creature speed columns from unsigned integers to doubles, preserving every former uint exactly in storage as well as fractional rates. Loading explicitly converts to the existing float runtime type. It does not retune other creatures. Both providers have matching migrations and model snapshots. The data rollback precedes the schema rollback. Arbitrary fractional edits made after upgrading cannot survive rollback to integer columns; only this scoped correction restores its prior integer value automatically.

## Original evidence and limits

Original gameplay video `7Lrst9SG3pk`, retained analysis A2 under `/home/blizz/backups/rasa-net/research/20260913-bootcamp/footage/analysis/A2/`:

- A2-044 at 285.333 s: turn-in dialogue says “Follow me over to the…”. Dialogue wording alone does not establish the move trigger.
- A2-047/048/050 at 289.533–289.800 s: Initiation completes and the Gearing Up offer opens. McAllister remains beside the recruit while the offer is read.
- A2-056 at 296.200 s: offer closes; A2-057 at 296.600 s: acceptance chat appears. `crops/s295_pf.png` directly shows the offer open through 296.133 s and NPC remaining in place.
- A2-060 at 297–301 s: turn and departure. `crops/walk_297_299.png` shows turning/stationary frames around 297–298 s and walking away around 298.4–298.8 s. Reinspected directly on 2026-09-22. Movement after acceptance is observed; selecting MissionAccepted as the server callback is inferred. No artificial exact delay is added because the original callback and animation/network delay are unknown.

The decoded original `generated/client/entitymovementrate.pyo` has lookup[3846]=(0.0,2.5,6.5), assignment offset 110, SHA256 `a1afe0abd4d3504cda9d2542778b706e1af9adbcab12e3614bbbc67b3d825660`; ZIP member timestamp 2009-02-09. Its consumer `client/playermovementmgr.pyo`, PlayerMovementMgr.__init__, original line 124 / bytecode 172–203 names these stoppedRate, walkAnimRate and runAnimRate. These are animation reference rates: they do **not** recover McAllister's original server pace. Using 2.5 m/s is an explicitly labelled **analogue** of his already reconstructed generic human NPC class 3846 (OD-11), recorded as OD-57 under the user's standing authorization for closest supported estimates. It is not a separately approved exact owner selection and not a measured McAllister speed.

The destination remains the existing inferred approach `(390,120.75,172)`, horizontal uncertainty ±5 m. Its Y is the original-map navmesh surface established by `BootcampEscortDestinationGround`. Exact route, pace and stop position remain unverified. This patch does not claim that endpoint as observed.

## Runtime checks and remaining opening gaps

The opening test now loads the migrated creature row, drives 1990's two approach greetings and completion, advances AI ticks while the 1992 offer is pending, accepts 1992, then advances AI ticks again. It asserts actual movement toward the destination as well as retained fractional speed. This catches both zero-speed and premature-departure regressions. Migration checks assert only the intended two rows change, rollback values, provider parity, and seeded fractional persistence. Serial integrated validation is coordinated by the parent agent; no separate parallel build was started.

The existing entry/radio offer and forced greetings 1634/1635 remain covered by the opening test. The original Power Logos grant timing is still inferred (OD-4): the first-vision footage does not expose a definitive server grant packet. Existing radius/height and location estimates retain their labels. No first-combat data was changed in this bounded correction.

Private-instance reconstruction respawns McAllister at the initial placement on reconnect; the accepted event does not replay solely because a mission is loaded. This does not prevent reaching the crate or continuing 1992, but original reconnect placement/path behavior is unrecovered. Do not invent persistence or replay the full acceptance reward flow to disguise that gap. Live-client observation of the corrected walk, exact animation timing, route and reconnect remains outstanding.

## Provider verification

A disposable MariaDB 10.5.29 check executed equivalent column/data operations
against synthetic creature/rule tables. DOUBLE preserved 4,294,967,295 and
16,777,217 exactly, retained 2.5, and changed only the intended NPC and rule.
Data rollback followed by schema rollback restored all original values and
`INT UNSIGNED NOT NULL` columns. This tested equivalent SQL operations, not
EF-generated SQL. The container had no network or exposed ports and was removed.
The local log and reproducible SQL are `/tmp/rasa-mcallister-mariadb-audit.log`
and `/tmp/rasa-mcallister-mariadb-audit.sql`.

The final integrated .NET 5 run passed 1,202/1,202 tests with none skipped,
including actual AI movement, SQLite migration rollback and evidence validation.
Log: `/tmp/rasa-creation-final-20260922.log`.
