# Boot-camp supply crate gear: template audit

> **Reconstructed 2026-09-26.** `src/Rasa.DBL/Migrations/BootcampData/BootcampCrateUncommonGearRows.cs` has cited
> this document since it was written on the deployed-but-uncommitted 2026-09-22..24 tree (commit `f30d9aa`, "The
> boot camp as the final-week footage plays it..."). That tree's own narrative write-up was never committed and is
> lost; there is no earlier version of this file to recover. What follows is reconstructed from the migration's own
> doc-comment, `docs/evidence/client-gearing-playthrough.json` (2026-09-22, an emulator-integration playthrough, not
> a retail-fidelity certification), and the seeded item-set data. Nothing here goes beyond what those sources state.

## What changed and why

The boot-camp supply crate (item set 19858, mission 1992 "Gearing Up for Battle") originally seeded four armour
pieces from the **common** Motor Assist row. Footage of the crate's own tooltips shows the **uncommon** row instead,
so the four templates were swapped:

| Slot | Was (common) | Now (uncommon) | Footage citation |
| --- | --- | --- | --- |
| Gloves | 13096 (`Body Armor: 23`, requires level 15) | 15803, `Armor_T1_MotorAssist_V06_UNC_Gloves_01_to_02` | 7Lrst9SG3pk A3-034, t=319.6: tooltip reads "Telaract Motor Assist Armor Gloves \| Body Armor: 28 \| Regen Rate: 1 per sec \| Motor Assist Armor 1: Novice \| Min Level: 1" |
| Boots | 13066 (requires level 30) | 12209 (V06) | A3-035 |
| Legs | 13156 | 26879 (V05) | A3-040 |
| Vest | 13186 | 12208 (V05) | A3-046 |

## How the gloves template was pinned

Body armour is the armour class's absorption divided by 10. Of every Motor Assist gloves template on the server,
exactly one absorbs 281 points and carries no level requirement: 15803. The V04, V05 and V07 gloves rows require
levels 22, 15 and 49 respectively, so they do not match a tooltip that reads "Min Level: 1" — 15803 is the only
candidate.

## Boots, legs and vest: inferred from class, not directly identified

The original seed inferred boots 12209 (V06), legs 26879 (V05) and vest 12208 (V05) from the gloves' class pattern.
A 2026-09-22 reinspection of the same footage recovered retained tooltip crops for those three slots (A3-035,
A3-040, A3-046): armour 42/70/84 and regen 1/2/2, which match the classes already selected. Multiple original
templates share those same classes, so **the exact template identities remain inferred**, not directly read from a
unique tooltip the way the gloves were. This reinspection changed no seeded values — it only added the supporting
tooltip crops.

Maker and module names shown in the client are runtime-composed from the equipped item's modules and do not, by
themselves, uniquely select a template; they are not used as an identifying signal here.

## Supporting integration evidence

`docs/evidence/client-gearing-playthrough.json` (playthrough-06, 2026-09-22) is an original-client-against-emulator
integration run, not a retail-fidelity certification. It records the crate handing over the five items on Loot All,
the first equipped boots completing objective 2, and a `rifle_fields` comparison against the original rifle tooltip
(A3-018) that also shows unresolved gaps for the crate's own gear: the emulator's items carry no `Not Tradeable` /
`Not Sellable` restriction lines and no module lines (`[4] Armor Piercing +8%`, `[2] Resist: Laser +12` on the
rifle) at the time of that run. Those gaps are tracked as open evidence, not resolved by this swap.

## What remains unverified

- The exact original template ids for boots, legs and vest (only their classes are pinned by tooltip armour/regen
  values).
- Any module or trade-restriction data on the crate's armour pieces (only the rifle's module lines are recorded in
  the playthrough, and only as an unmatched original-vs-emulator comparison).
