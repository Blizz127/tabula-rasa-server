# First practice-target combat audit (2026-09-22)

The Gearing Up for Battle weapon path omitted the original client's damage feedback
for destroyable objects. `MissileTrigger` listed the Practice Dummy in `hits` but
left `hitdata` empty. Original `BaseWeaponAttack.DoHits` (source line 188,
bytecode offsets 12–141) iterates only `hitdata`, decodes each damage record and
calls the object's `AnnounceDamage`. Original `Usable.AnnounceDamage` (source
1081, offsets 0–57) emits `UI_FLOAT_DAMAGE` when the record contains damage.
An `UpdateHitPoints` packet does not replace that path. The original footage
independently shows red damage numbers over the dummy (7Lrst9SG3pk,
347.133, 350.933, 355.267, 360.333 seconds).

Weapon recoveries now include the existing calculated damage amount/type and
critical flag for a living damageable placement. Destroyed placements no longer
produce a reported hit during their restore interval. No damage formula, armor,
XP, loot or objective requirement changed. Object destruction remains communicated
through the existing usable state; no actor death-blow semantics were invented.

The weapon windup also discarded an eligible object target, sending `None`
because it used the creature-only variable. It now includes the selected object's
ID. Original `TargetedAction.Windup` source 259, offsets 131–193 uses the target
as `fxHits` when there is no resolved hit list; this matters for server-initiated
autofire and other clients watching the lesson.

`DestroyablePlacementTests.CombatFeedback` verifies damage records on surviving,
destroying and overkill hits, correct objective credit, no hit while destroyed,
and recovery at the precise configured deadline. The existing fixture's 100 HP
is test data, not proof of the original dummy's health.
`WeaponAttackLifecycleTests.ContentTargets` verifies the server windup target
and that ammunition is not spent before impact. A focused .NET 5 baseline run
copied only the new tests and partial-class declarations into the previously
verified source snapshot, retaining its old production code: all five new cases
failed at the expected assertions, while all 32 existing cases passed (37 total,
7.0757 seconds). The run used the offline `rasa_net:latest` image, one MSBuild
worker and disabled shared compilation. Log and TRX are retained at
`/tmp/rasa-retail-20260922-combat-before.log` and `.trx`. With the two production
fixes applied, all 54 cases in those two fixtures and the appearance helper
fixture passed in 7.4887 seconds. The log is
`/tmp/rasa-retail-20260922-combat-after.log`. The final integrated suite, including
the crate changes but preceding the health reconstruction below, passed 1,218/1,218 cases with zero skipped in 4.4251 minutes:
`/tmp/rasa-retail-20260922-crate-final.log`.

The original package is the repository's 1.16.5.0 compatibility artifact,
compiled February 2009; its exact relationship to the final shutdown build is
still unverified. Source hashes and exact locations are in
[`evidence/bootcamp-combat-feedback.json`](evidence/bootcamp-combat-feedback.json).

A subsequent native-frame audit found five continuous single-round rifle hits,
each followed by the Practice Dummy disappearing. The third clearly displays
84 damage (355.267–355.467 seconds). Under ordinary damage-driven destruction,
with no concurrent damage or forced server script, that would bound the target's
then-current health at 84. It does not identify its original maximum HP. The
client's fading `HealthBarDamage` overlay also prevents reading exact remaining
HP from the total red bar length.

`BootcampPracticeDummyHealth` changes only placement 198652 from the previous
100 HP placeholder to **1 HP, explicitly inferred**. This minimal positive value
reproduces the observed single-hit behavior through the existing damage system;
it does not claim that the original server used 1 HP. Low HP and a script forcing
destruction on a successful hit remain indistinguishable in the footage. No
custom destruction mechanic was added. The paired SQLite/MySQL data migrations
restore 100 HP on rollback and leave the unfilmed Lightning Target Dummy 198653
at its existing 100 HP estimate.

The exact native-frame disappearance/return measurements are:

| First absent (s) | First present (s) | Visible interval (ms) |
| --- | --- | --- |
| 347.133333 | 348.066667 | 933 |
| 350.933333 | 351.666667 | 733 |
| 355.266667 | 356.266667 | 1,000 |
| 357.733333 | 358.866667 | 1,133 |
| 360.333333 | 361.200000 | 867 |

The retained nominal `restore_ms=930` approximates the 933 ms sample mean.
The visible intervals span 733–1,133 ms, each with ±67 ms frame quantization at
15 fps; about ±270 ms around the retained nominal value covers those observations.
These are visible intervals, affected by presentation and network timing, not a
recovered original server timer. The old four-cycle ±70 ms claim missed the
357.733333 cycle and used the second health bar's fully bright appearance at
351.8 instead of the first visible mesh/nameplate at 351.666667.

The new seeded-data regression cases use 1 and 84 damage, check that zero damage
does not complete the objective, and verify destruction, credit, deadline recovery
and another single-hit destruction after recovery. They load the migrated seed's
combat values through the database, catalog and materializer fixture path. Narrow
operation and SQLite migration tests cover scope, untouched Lightning HP and
rollback. These new cases await the serial integrated test run; the earlier passing
suite above does not yet verify this change. Measurements, source hash, retained
frames and reconstruction limits are recorded in
[`bootcamp-practice-dummy-observations.json`](evidence/bootcamp-practice-dummy-observations.json).

Open fidelity limits:

- Exact original dummy HP, resistance and forced-script behavior remain unknown.
  The 1 HP reconstruction guarantees destruction for positive integer damage;
  attacks weaker than the filmed rifle have not been observed against this target.
- The Lightning/Target Dummy scene is cut out between 362.067 and 362.133 seconds.
  Original text 21665 says to target the dummy and fire Lightning, without explicitly
  requiring destruction. A later inferred correction credits the first damaging
  Lightning hit; its exact original completion threshold, 100 HP and borrowed
  recovery remain unverified. See [the hit-credit audit](bootcamp-lightning-hit-credit.md).
  Both other retained videos begin after this lesson.
- The rifle's crate tooltip directly shows 20/20 before receipt at 304.733 seconds
  (A3-018). Its first equipped HUD at 335.467 shows 20 loaded rounds and 994 reserve.
  The crate audit owns the issue-time magazine correction and provenance.
- Exact damage rolls/modifiers, target-instance recreation versus state restoration,
  and credit under simultaneous attacks still need evidence. Passing packet tests
  does not establish those original server rules.
