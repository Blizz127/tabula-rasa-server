# World admission orientation

The original 1.16.5.0 client, connected to the isolated emulator playthrough on
2026-09-22, initially faced upstairs at the reconstructed Luna Cavern arrival.
Changing the disposable character's saved yaw from 6.02139 to pi produced the
same scene and forward movement along negative Z. This identifies an admission
problem independently of the estimated spawn heading. After moving ActorInfo
before controller assignment, the same saved pi yaw faced down toward the cavern
pillars, and a brief W input moved Z from -79.09 to -77.5 with X unchanged.
`playthrough-03/02-world-pi-before-input.png` and `game.log` at 14:38:11.669
record this successful original-client probe. The exact original arrival heading
remains a separate evidence question.

The exact original consumers and artifact hashes are recorded in
[the evidence manifest](evidence/client-world-admission-orientation.json).
`Actor.Recv_ActorInfo` (line 2382, offsets 0–28) sets yaw and synchronizes facing.
`PhysicalEntity.Recv_WorldLocationDescriptor` instead sets the location and
quaternion. `SetControlledActorId` leads to `_ControlledActorAvailable`
(line 110, offsets 116–128), which creates the native user controller. The
emulator previously sent ActorInfo after this controller assignment. The narrow
fix moves the existing ActorInfo immediately before assignment. It
does not duplicate that packet or alter player/creature corpse introductions.

Wonkavate already carries the saved yaw. Original `BeginMapLoading` passes it
unchanged to `gameclient.ForceCameraSettings`. The native implementation at
VA `0x4d9580` calls `D3DXQuaternionRotationYawPitchRoll` at `0x4d95d4`, with
zero pitch and roll. There is no evidence here for a degrees/radians conversion
or XYZW ordering error.

The native controller trace establishes the facing snapshot: mode-0 activation
at `0x54dc60` binds the actor, then calls camera virtual slot `+0x2c` at
`0x54dc97`. Its implementation, `0x56d880`, reads the actor's facing yaw at
`actor+0x40` and supplies it to the active camera yaw setter. The original
`SyncFacingToYaw` implementation at `0x4ff2c0` writes that same field at
`0x4ff34d–0x4ff366` when no movement controller exists. Thus the early camera
snapshot used default facing even though the entity already had its location
quaternion. The packet-order fix initializes the field before that snapshot.

Original `radarwindow._UpdateManifestation` (line 1019, offsets 63–81 and
112–161) drives its arrow with
`int(((pi - world.GetCameraDir().yaw) / (2*pi)) * 1000)`. Native GetCameraDir
at `0x8ba250` derives its angles from the rendered world-camera quaternion,
not directly from the saved character yaw. The perpendicular-axis probe in
`playthrough-04/02-half-pi-before-input.png` used native yaw pi/2. W moved X
from 387.2 to 383.9 while Z stayed -79.1 (`game.log`, 14:49:35.806).
Together with pi facing positive Z, this establishes the compass conversion:
`heading = wrap(nativeYaw + pi)` and `nativeYaw = wrap(heading - pi)`.
The corresponding horizontal forward vector is `(-sin(yaw), -cos(yaw))`.

The three focused `NewCharacterTests.WorldAdmission` cases exercise the full
assignment and require one ActorInfo before one controller assignment, with the
stored yaw and living/dead state preserved. The combined suite passed 1,232 of
1,232 tests with none skipped in 5.0007 minutes, including these cases and the
weapon admission cases. Results are `/tmp/rasa-retail-20260922-opening-final.log`
and the matching `.trx`. This run precedes the separate yaw data migration.

The subsequent [weapon drawer admission fix](client-weapon-drawer-admission.md)
moves this control phase into `InitializePlayerControl`, immediately after the
owner entity is introduced and before nearby actors. The verified ActorInfo
before SetControlledActorId ordering is retained.

The measurement defines compass zero as +Z, clockwise toward +X; its
`arrow2.py` calibration uses northward motion at `7Lrst9SG3pk` 240.467 seconds.
The seed previously stored that compass angle directly as native yaw. The
correct conversion of 345 degrees is 165 degrees, or **2.879793 radians**.
Paired `20260922220000_BootcampFirstLoginYaw` data migrations now change only
`content_location` row 19851's rotation and restore 6.02139 on rollback. Both
provider Designers freeze the immediately preceding target model. The original
S1 migration, all coordinates, other location rows, and existing character
rotations remain untouched. A focused test compares every location field before
and after Up/Down and checks each provider's exact operation and target model;
serial validation passed 97/97 targeted tests, none skipped, in 2.8133 minutes
(`/tmp/rasa-retail-20260922-heading-final.log`).

The subsequent combined suite passed **1,246/1,246 tests**, none failed or
skipped, in 6.0290 minutes, including the yaw migration and admission cases.
The log `/tmp/rasa-retail-20260922-selection-verified.log` has SHA256
`535787464acf67366aa8391c0ec80e5b06c920eb84bf2c788e074bc6cbabd377`;
all 2,050 source snapshot entries still match the tested files.

The source frame for the measured 345-degree heading is
`7Lrst9SG3pk` at 238.133 seconds, after approximately 1.8 metres of movement,
not an original server packet or certain first arrival orientation. Positions
and reconstruction provenance retain its measured tier and ±15-degree
uncertainty. Only the coordinate conversion has been corrected. Other measured
location rotations and shared server facing calculations require a separate
audit as progression reaches their consumers; no bulk conversion was made.

A subsequent original-client check created **Borin Vanguard**, slot 2, through
the existing-family creation UI after the migration. The persisted new row has
rotation 2.879793, and `playthrough-05/07-fresh-first-world.png` shows the first
Initiation offer while facing down the causeway. No diagnostic position/yaw
edit was made to this character. The prior Alden character kept its normally
played position. This verifies the seed-to-creation-to-render path; the original
footage's exact arrival angle remains estimated as described above.

The later [mission-tracker reconnect correction](client-mission-tracker-reconnect.md)
uses the original client's deferred controller path. The current sequence is
CharacterOptions, SetControlledActorId while the owner entity is absent, then
the owner's CreatePhysicalEntity bundle. Its existing state, ActorInfo and a
read-only saved mission snapshot precede WorldLocationDescriptor, now the last
field. That location adds the actor to the world and releases the registered
controller callback. Thus **yaw still precedes actual controller creation**,
although the SetControlledActorId packet itself now comes first. The spawn
rotation and the earlier measured heading conversion are unchanged.

The revised sequence passed 174/174 targeted tests, none skipped, in 40.2117
seconds (`/tmp/rasa-retail-20260922-tracking-targeted.log`, SHA256
`0d49880372e9e8010e8121ff70b9517d45a4db56944bbf267372e3a7fa54e5eb`).
Original-client run 09 then entered Luna Cavern and restored the saved mission
tracker before UI input: `playthrough-09/02-plain-option-loaded.png` shows
Gearing Up for Battle and Speak to Corporal Hartmann. This is a successful
emulator admission check, not a repeat of the earlier controlled yaw-axis
measurements. The revised full suite and grouped-option runtime check were
still pending at this checkpoint. Exact capture hashes are in this document's
evidence record; the earlier results above remain historical evidence.
