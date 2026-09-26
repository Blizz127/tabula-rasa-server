# S5 content-use windup integrity (2026-09-24)

The 1.16.5.0 compatibility client's bomb use bar in original gameplay
`8VXeKzGUv0c` at 80.600–86.800 seconds supports a measured 1,420 ±150 ms
windup before the bomb arms (see `docs/evidence/bootcamp-d11-reconstruction-manifest.json`,
OD-27). This does not establish how the original server handled duplicate use
requests or interruption; those protocol details remain unverified.

The emulator queued one `ActionData` per use but kept only the most recent
pending placement under the player entity ID. A repeated use request on the
same bomb could make the first queued recovery finish the second request before
its windup elapsed. An interrupted queued action also left its pending marker
behind. This is an emulator consistency defect, not a reconstructed retail rule.

`MissionContentRuntime` now accepts one content-use request per actor until
recovery or interruption, associates the pending request with its exact queued
action, and clears it when that action is interrupted. Recovery from an older
action cannot arm the bomb. This preserves the measured windup for the accepted
request; the original server's duplicate-request response is still unknown.

`BombPlacementTests.DuplicateOrInterruptedUseCannotCompleteAContentWindupEarly`
exercises duplicate, interrupted and stale recovery paths. The focused bomb and
reinforcements suite passed 9/9 in the isolated .NET 5 SDK container on
2026-09-24. No live server or DIT deployment was made for this change.
