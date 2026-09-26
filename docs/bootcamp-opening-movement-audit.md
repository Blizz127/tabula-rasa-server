# Boot camp opening: McAllister's walk to the gear

> **Reconstructed 2026-09-26.** `src/Rasa.DBL/Migrations/BootcampData/BootcampMcAllisterWalkRows.cs` has cited this
> document since it was written on the deployed-but-uncommitted 2026-09-22..24 tree (commit `f30d9aa`, "The boot
> camp as the final-week footage plays it..."). That tree's own narrative write-up was never committed and is lost;
> there is no earlier version of this file to recover. What follows is reconstructed from the migration's own
> doc-comment and `docs/evidence/client-mcallister-grounding.json` (2026-09-22, an emulator-integration
> investigation, not a retail-fidelity certification). Nothing here goes beyond what those sources state.

## What changed and why

Footage (7Lrst9SG3pk, A2-056/057/060, 296.2/296.6/297-301 s) shows Corporal McAllister (creature 198500) remaining
beside the recruit until mission 1992 ("Gearing Up for Battle") is accepted, and then walking away toward the gear.
`BootcampMcAllisterWalkRows` retimes content rule 1985014 so it now fires on **1992 accepted** (event 2) instead of
**1990 turned in** (event 6, the previous trigger), keeping the comment "1992 accepted -> McAllister walks to the
gear". Rolling the migration back restores the 1990-turn-in trigger and its original comment.

## The 2.5 m/s walk speed is an analogue, not a measured value

The migration sets creature 198500's `walk_speed` to 2.5. This is explicitly an **analogue**: it is the animation
reference walk rate of McAllister's reconstructed human class, not a speed measured from the lost original spawn.
No footage frame-times McAllister's own walk over a known distance, so his actual original pace is unrecovered.
Walk and run rates for this creature became independent doubles as part of this change (previously a single shared
rate), so the analogue walk speed does not also force a matching run speed.

## Supporting investigation: the arrival grounding question

`docs/evidence/client-mcallister-grounding.json` records a 2026-09-22 investigation into a still image
(playthrough-06, capture 37) that appears to show an officer suspended above the terrain near the firing range. It
explicitly does **not** establish McAllister's exact runtime position, height, or the cause of the visual:

- A path probe from (387.2, 125.57, 53.3) to the walk destination (390, 120.75, 172) completes and lands inside the
  destination polygon, but the navmesh height (120.75) differs from the decoded original terrain height at the same
  X/Z (120, from `H_be16.npy`) by 0.75 m. No nearby decoded static geometry closes that gap.
- Source-level review found a concrete omission (not a proven cause): `BehaviorManager.FollowPath`'s one-shot
  completion ended within 0.8 m of the destination without broadcasting a zero-speed `Movement` update, so a late
  observer's client could keep rendering the last advertised 2.5 m/s heading. This was corrected (a terminal stop
  packet is now sent, and a late-introduced observer's `ActorInfo` yaw matches the last walking heading) — a
  regression fix (`BootcampOpeningTests.WalkArrival.cs`), not a change to McAllister's placement or footage-derived
  destination.
- The original footage itself (7Lrst9SG3pk, 304.667-304.733 s) cuts from McAllister's approach to the crate-loot
  window before showing his arrival or exact footing, so **the source cannot validate the reconstructed arrival
  height** either way.

## What remains unverified

- McAllister's true original walk speed (2.5 m/s is an analogue, not a measurement).
- The exact original arrival position and ground contact at the gear/firing-range destination.
- Whether the visual "suspended officer" impression in playthrough-06 reflects a genuine original-terrain mismatch
  or only the (now-fixed) missing terminal-stop packet.
