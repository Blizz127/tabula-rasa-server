# Crate rifle range correction — 2026-09-22

The isolated original-client capture `playthrough-06/32-rifle-tooltip.png`
showed template 13713 as Rifle: 106 primary damage, 80 m optimal range, 25 melee
damage. Final-week footage `7Lrst9SG3pk`, A3-018 at 304.733 s, shows the crate's
Shinobi Rifle with 106 primary damage, 60 m optimal range and 82 melee damage.

The 60 m range is independently recoverable for the selected template from the
original client tables, all compiled 2009-02-09:

| Original data | Assignment offset | Value |
| --- | --- | --- |
| `itemclass.itemTemplateItemClass[13713]` | `416576` | Class 27220 |
| `weaponclass.lookup[27220]` | `104576` | Primary action 1/134, damage 106/106, magazine 20 |
| `actiondata.actionArguments[(1,134)]` | `43925` | `(0,None,500,502,60,250,0,1)`; maximum range 60 m |

The emulator already preserves that action row in `WeaponAttackData`. Its
separate `itemtemplate_weapon` row still had the generic preloader's range 80.
`ItemTemplateTooltipInfoPacket` sends that row's range at weapon tuple index 10.
The original `gameuiutil.GetWeaponRange` and tooltip renderer display it
directly; they do not apply a module or skill multiplier to turn 60 into 80.
This is stale template data, not a client calculation error.

`BootcampCrateRifleRange` updates only template 13713's `range` from 80 to 60 in
both world providers; rollback restores 80. Frozen target models match the
preceding migration's schema. The change does not replace every template's
range from its action, because template-specific original overrides have not
been audited. Template 13713 remains an **inferred counterpart** to the filmed
Shinobi Rifle; the range for that selected counterpart is **original** client
data and independently **observed** on the filmed rifle.

Melee damage 82, manufacturer/module binding and trade restrictions remain separate.
The tooltip also displays alternate damage directly, but the selected template's
original alternate action/base damage has not been recovered. The current
server rejects alternate-action requests, so changing 25 to 82 alone would not
implement the missing attack. No alternate fields are changed here.

A further original-client check confirms that `Weapon.__init__`, source lines
79–81, initializes the alternate action ID, argument and cached action info to
`None`. `Recv_WeaponInfo`, lines 281–293, fills them from the server's method
arguments. No alternate action or base damage for class 27220 was recovered
from that client data. Three retained emulator SQL dumps contained equipment
or item metadata matches, but no alternate values for this template. These
secondary dumps do not establish final-live behavior. The observed 82 remains
useful evidence for reconstructing the filmed item once its binding is known.

The server stores primary action `MaxRange=60` but currently does not consult
that field during weapon target selection or hit resolution. Original-client
prediction and the displayed range must not be mistaken for server-side range
enforcement. Range checks, falloff and hit behavior require a separate audit;
this migration does not invent them.

Four regression tests check the original seed class/action link, both providers'
operation descriptions, and the serialized tooltip's 60 m field. A SQLite
`IMigrator` regression runs the preceding yaw migration → range correction → yaw
rollback and compares every weapon row/field, accounting for only the intended
range change. This executes EF-generated SQLite SQL; the MySQL check establishes
operation/model parity only. The tooltip regression uses the original class's
ammo class 3147 and magazine capacity 20 and consumes all 16 tooltip fields;
the separate `WeaponInfo` method has 17 fields. All four passed in the combined
isolated .NET 5 run: **1,251/1,251 tests passed, none skipped**, in 6.2316 minutes.
The actual SQLite migration/rollback regression passed in **29 seconds**, as
reported at the test runner's display precision. The other rifle tests passed
in 3 seconds, 130 milliseconds and 2 seconds respectively. No production
database was changed.

The verified run log is `/tmp/rasa-retail-20260922-gearing-verified.log`
(SHA-256 `ea2037d8e7cb20beae9a390a8316924ed2050d5f68ffedfaca3a15886d038ac2`).
The source snapshot is `/tmp/rasa-retail-20260922-gearing-snapshot.json`
(SHA-256 `8c505bd1afb4f9862e09862f5ffc2d2e69b6d7a26d2deddebbe0a53c1707e819`);
all 2,056 recorded source files still match. This documentation update preserves
that tested source snapshot.

The original-client runtime recheck passed in isolated playthrough 07. Capture
`05-rifle-range-tooltip.png` visibly shows the equipped Rifle with 60 m optimal
range, 106 primary damage, unchanged 25 melee damage and 17/20 loaded rounds.
The frozen `tooltip-retest.json` records template 13713 with range 60 and the
new migration applied. Its saved `tooltip-retest-client-prefix.log` is hashed
as an immutable artifact; it retains the separate startup tuple and projection
warnings, which this range change does not address.

This confirms the corrected emulator data renders in the authentic client. It
does not certify the inferred template identity, modules, alternate attack or
other original-live behavior. Exact capture/record/log hashes, field provenance
and outstanding gaps are in the
[evidence manifest](evidence/bootcamp-rifle-range.json).
