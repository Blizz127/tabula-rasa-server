# Gearing Up equip-step audit — 2026-09-22

The boot-camp mission `1992` skipped its equipment lesson as soon as the crate
objective completed. `CommitObjectiveProgress` reveals objective `2`, then checks
what the recruit already wears. Its seeded `equip_match = 0` accepted any equipped
item, including the starting outfit and pistol. This contradicts the surviving
recording, which keeps that lesson open until the player equips crate gear.

## Original evidence and its limits

The original recording is YouTube `7Lrst9SG3pk`, *Tabula Rasa Creating Character
1of8*, the owner-supplied video whose SHA-256 is
`c3783d8c863f6807e8f55a8b47dd33d6f1117c3556906fda94c62a933edf6632`.
Its player chat dates the recording to the final week before shutdown; this
does not establish the exact executable revision. The evidence catalog uses
decoded video seconds, not approximate YouTube playback timestamps.

- `A3-027`, **308.533 s**: after collecting the crate, the tracker changes to
  “Equip the gear by right-clicking it in your backpack [B]”. The character
  already has the Recruit outfit and pistol equipped.
- `A3-036`, **321.667 s**: the crate boots leave the backpack. The displaced
  Recruit Boots enter that slot at **321.867 s**.
- `A3-037`, **322.267 s**: the equip objective completes while the gloves, legs,
  vest and rifle remain in the backpack. A full outfit is therefore unnecessary.

Exact transcriptions and verification corrections are in
[`bootcamp-d11-footage-events.json`](evidence/bootcamp-d11-footage-events.json).
The local original-frame composites re-inspected for this audit are
`/home/blizz/backups/rasa-net/research/20260913-bootcamp/footage/analysis/A3/work/q308.png`
and `c322.png` in the same directory. They corroborate the open tracker after
looting and the later completion banner.

Confidence is high that existing starter equipment must not automatically
complete the lesson. The exact original server predicate is unrecovered. It
could have listened to a new equip operation or matched a class of equipment;
the footage does not test re-equipping starter gear or equipping every crate
item. The chosen reconstruction matches an equipped member of crate item set
`19858`, retaining the existing reconnect reconciliation. That predicate is
**inferred**, not original or directly observed. Only the boots are directly
shown satisfying it. Existing estimates in the crate's item identities remain
unchanged; the evidence for their armor classes was strengthened below.

`BootcampEquipCrateGear` changes only binding `(1992, 2, 0)` to item-set matching
(`equip_match = 2`, `item_set_id = 19858`) and updates its comment. The SQLite and
MySQL migration operations are identical, and rollback restores all three prior
values. The field-level citations and replacement history are recorded in the
[`reconstruction manifest`](evidence/bootcamp-d11-reconstruction-manifest.json).

## Crate ownership after reconnect

The existing container recovery policy recognizes already held crate items so
rebuilding a dispenser does not require taking duplicate gear. Its ownership
scan checked the backpack and armor slots but omitted the weapon drawer, even
though equip objectives treat the drawer as equipped. A recruit who had moved
the rifle into the drawer could therefore remain short of a crate item under
that recovery policy. The scan now includes the drawer. This repairs an
internal inconsistency; original reconnect/container refilling behavior remains
unverified and is not asserted by this fix.

## Crate armor tooltip correction

The older claim that boots, legs and vest tooltips were absent was incorrect.
Their retained crops are readable, and their timestamps were already recorded
in the original A3 transcript. Reinspection found the following matches, without
changing any seeded item IDs:

| Piece | Footage event and time | Body armor / regen | Selected template → original class |
| --- | --- | --- | --- |
| Boots | A3-035, 321.467 s, `c321_tt.png` | 42 / 1 per second | 12209 → 16692 |
| Gloves | A3-034, 319.6 s | 28 / 1 per second | 15803 → 16742 |
| Legs | A3-040, 324.467 s, `c324_tt.png` | 70 / 2 per second | 26879 → 16592 |
| Vest | A3-046, 332.467 s, `c332_bp.png` | 84 / 2 per second | 12208 → 16642 |

Each tooltip requires Motor Assist Armor 1 and minimum level 1. In the recovered
client's `generated/client/armorclass.pyo`, the corresponding `lookup` tuples
are `(422,422,1)`, `(281,281,1)`, `(703,703,2)` and `(844,844,2)` at assignment
offsets **19604, 19802, 19208, 19406**, respectively. The member is dated
2009-02-09 and has SHA-256
`c01b8bf0130c349b600ee202f8485ec10005fff553921fda2a28f202b6e6ea33`.
`generated/client/itemclass.pyo` supplies the template-to-class and skill-rank
links; exact offsets are now in the manifest. Its SHA-256 is
`cd0e4367ff60ede526ef5429e3fdbf73f99873a15bcb4c426fed28e7bbc13ed9`.

These observations support the selected armor classes, but do **not** uniquely
recover template identities. The original client's mapping includes additional
templates for each same class: boots `30058/50241`, gloves `30211/50242`, legs
`29751/50239/111158/130434`, and vest `29905/50240/111156`. Manufacturer prefixes
and module contents remain unresolved. Boots, legs and vest provenance therefore
moves from an analogue borrowed from gloves to **inferred** matching of their
own observed tooltips. The existing glove label remains inferred, with its
earlier emulator-subset uniqueness claim explicitly qualified.

## Verification

The migrated-seed runtime regression completes `1992/1` with starter equipment
present, checks that `1992/2` stays open, equips crate boots, then verifies the
persisted completion, the reveal of `1992/5`, and no duplicate completion. A
separate crate regression verifies recognition of a drawer item without taking
the remaining duplicate. Migration tests check the single affected binding,
exact rollback values, and provider parity. These are implementation checks;
an original-client playthrough of both paths remains necessary.
