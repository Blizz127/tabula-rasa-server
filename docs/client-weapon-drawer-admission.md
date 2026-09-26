# Weapon drawer admission ordering

The original client raised `AttributeError: 'NoneType' object has no attribute
'GetArmedWeaponDrawerSlot'` during isolated world admission on 2026-09-22.
The traceback is in `run-client/tabula_rasa.log`, lines 26–29, under
`/home/blizz/backups/rasa-net/research/20260922-client-playthrough`.

Two original consumers explain the failure. `Actor.Recv_EquipmentInfo`
(source line 2287, bytecode offsets 380–398) posts `UI_UPDATE_ARMED_WEAPON`
unconditionally. `weapondrawerwindow.HandleUpdateArmedWeapon` (source line
253, offsets 0–21) obtains the controlled manifestation and immediately calls
`GetArmedWeaponDrawerSlot`, without checking for None. Exact artifact hashes,
locations, and confidence are in [the evidence manifest](evidence/client-weapon-drawer-admission.json).

The emulator included EquipmentInfo inside the owner's CreatePhysicalEntity
packet, before SetControlledActorId. It also introduced nearby players before
control assignment, and their EquipmentInfo triggers the same global UI event.
Deferring only the owner's equipment would therefore leave multiplayer arrival
exposed to the same failure.

The correction creates the owner without EquipmentInfo and immediately sends
the existing CharacterOptions, ActorInfo, SetControlledActorId, and
WeaponDrawerSlot, followed by the deferred EquipmentInfo. CellManager completes
this control phase before introducing nearby entities. Other players' normal
entity bundles retain their equipment. Both normal admission and dropship map
transfer share this path. DeadOnArrival remains in the entity bundle; ActorInfo
still precedes controller creation, preserving the verified orientation fix.
The remaining AssignPlayer work and map-specific callbacks keep their existing
call sites.

Four packet-order cases exercise full CellManager admission with a nearby
player, crossing living/dead with normal/transfer session state. They check that
the owner bundle has no early equipment event, the corpse announcement survives,
and both the deferred owner equipment and nearby-player bundle follow control
assignment. Three existing yaw/state cases exercise the control and remaining
assignment phases. The combined full suite passed **1,232/1,232 tests**, none
skipped, in 5.0007 minutes (`/tmp/rasa-retail-20260922-opening-final.log` and
matching `.trx`).

The subsequent combined suite passed **1,246/1,246 tests**, none failed or
skipped, in 6.0290 minutes. The log
`/tmp/rasa-retail-20260922-selection-verified.log` has SHA256
`535787464acf67366aa8391c0ec80e5b06c920eb84bf2c788e074bc6cbabd377`;
all 2,050 source snapshot entries still match the tested files.

Original-client playthrough 04 also passed: the weapon drawer traceback is
absent in the retained `playthrough-04/tabula_rasa.log` and
`playthrough-04/admission-retest.json`. The log SHA256 is
`433a3830bc58b383d110cb3b74050ac0ed09f9cb39705083a4734706aeab673b`.
The independent tooltip and recruit-clothing fixes also removed their recorded
errors. The separate startup `TypeError: argument list must be a tuple` remains
at line 7; this admission fix does not claim to resolve it.

The later [mission-tracker reconnect correction](client-mission-tracker-reconnect.md)
changes how owner control is initialized while preserving equipment ordering.
The current admission sends CharacterOptions and selects the absent owner ID,
then introduces the owner with its existing state, ActorInfo and a read-only
mission snapshot before the final WorldLocationDescriptor. World entry releases
the original client's deferred controller callback. Only after that owner
bundle do WeaponDrawerSlot, owner EquipmentInfo and nearby actor introductions
arrive. Nearby bundles retain their equipment; the owner's corpse announcement
remains in its initial state. Normal and dropship admission share this path.

The updated yaw, corpse, transfer and nearby-equipment order checks are included
in the 174/174 passing targeted tests, none skipped, in 40.2117 seconds
(`/tmp/rasa-retail-20260922-tracking-targeted.log`, SHA256
`0d49880372e9e8010e8121ff70b9517d45a4db56944bbf267372e3a7fa54e5eb`).
Original-client run 09 also completed admission and restored the saved mission
tracker before UI input, visible in `playthrough-09/02-plain-option-loaded.png`.
That screenshot confirms successful admission and tracker restoration; the
earlier run 04 log remains the specific evidence for removal of its drawer
traceback. The revised full suite and grouped-option runtime check were pending
at this checkpoint. Exact screenshot and state-snapshot hashes are recorded in
the evidence manifest.
