# Active world equipment compared with the February client

Audit date: 12 September 2026. Target: 1:1 final-live preservation. The active
database's numeric weapon and armor class values match the recovered original
tables. The main unresolved data gap is the 2,440 weapon template rows, which
all contain the same 22 values. Their replacements require evidence for the
server-supplied weapon and module stats.

## Scope and reproducible evidence

The original package has embedded version **1.16.5.0**, with Python module
timestamps on **10 February 2009 UTC**. [client-artifacts.md](client-artifacts.md)
records acquisition from a community archive and the missing independent
official checksum. These are strong versioned client artifacts; they do not
establish every final server hotfix or prove that every retained item was
available on the final live service.

The active source was verified from the `rasa-net-game-1` bind mount:
`/home/blizz/servers/rasa-net/rasaworld.db` → `/app/rasaworld.db`. A consistent
SQLite backup was read through `mode=ro` with `query_only=ON`, without writing
the live database, at **2026-09-12T19:37:30.790553+00:00**. Repository HEAD was
`6db42a94a8c5814b17cfd44c0c157536b50f2a58`.

Raw material and complete reports are retained outside the repository at:

`/home/blizz/backups/rasa-net/research/20260912-client-artifacts/world-equipment-selected/`

The `world-snapshot.db` SHA-256 is
`a70c12eebea8675955e01c8e5ed2d09a137db3f929c28bae608152e92685514a`.
`snapshot-provenance.json` includes applied migrations and capture metadata.
Reproduce comparisons against that immutable snapshot with:

```sh
cd /home/blizz/backups/rasa-net/research/20260912-client-artifacts/world-equipment-selected
python3 audit-world.py
python3 audit-template-mapping.py
python3 audit-requirements.py
/tmp/rasa-bytecode-tools/bin/python read-crafting-metadata.py
/tmp/rasa-bytecode-tools/bin/python read-equipment-consumers.py
```

`audit-world.py` reuses the snapshot when it exists and verifies its hash.
`active-world-rows.json`, `world-client-mismatches.json`,
`world-client-comparison-summary.json`, `template-mapping-report.json`, and
`requirements-comparison.json` contain exact IDs, fields, original values,
current values, and mappings. The original literal tables are in the adjacent
`combat-damage-selected/` directory. They were decoded from collection-building
bytecode, without executing or importing acquired modules.

| Original archive member | SHA-256 |
| --- | --- |
| `data/game.zip:generated/client/weaponclass.pyo` | `49e514546f8e30ee906b9e63c04e2c541af98bc681b8005e3a9f4f225f943591` |
| `data/game.zip:generated/client/armorclass.pyo` | `c01b8bf0130c349b600ee202f8485ec10005fff553921fda2a28f202b6e6ea33` |
| `data/game.zip:generated/client/itemclass.pyo` | `cd0e4367ff60ede526ef5429e3fdbf73f99873a15bcb4c426fed28e7bbc13ed9` |
| `data/game.zip:generated/client/equipmentdata.pyo` | `75c2caf08e42fea264dbf521e11350dee44faf369bfdeae035fbb3bc3d5afed8` |
| `data/game.zip:generated/shared/crafting.pyo` | `f6984159a4e1037bda8b2664e4c1c67bd8204b3680337160221861bc061e1594` |
| `trpython.zip:client/augmentations/weapon.pyo` | `38357c191fd8214e1b911ed51cb4b03989105f8b6b71eac54f253558a8f02594` |
| `trpython.zip:client/gameui.pyo` | `451bb70f10faced1261c74ad6452057711f230dc0798faf924b25911257d84d0` |
| `trpython.zip:client/gameuiutil.pyo` | `0a6543c60a39f8be64040c598216d606b0c211f9411dff96308b1deaff17df7b` |

`source-manifest.json`, `consumer-sources.json`, `tooltip-server-sources.json`,
and `equipment-consumer-raw-manifest.json` identify additional raw inputs and
their hashes. Selected method files include original source lines, arguments,
and raw bytecode offsets. Decompiled comments can show February 9 in the host
timezone; UTC timestamps above come from the module headers.

## Numeric class data and mapping results

| Active table | Rows | Result |
| --- | ---: | --- |
| `weaponclass` | 2,946 | All IDs and the first 18 named numeric fields match; original `None` values were imported as zero in 4,341 fields. |
| `armorclass` | 3,377 | All IDs and all three fields match. |
| `itemtemplate_itemclass` | 30,225 | All template-to-class mappings match. |
| `equipableclass` | 6,935 | All equipment slot mappings match. |
| `itemtemplate_requirement` | 5,293 | All generic requirement rows match original class-keyed `reqData`. |
| `itemtemplate_requirement_skill` | 19,564 | All template skill requirements match. |
| `itemtemplate_requirement_race` | 70 | All template race requirements match. |
| `itemtemplate_weapon` | 2,440 | Every row maps to an original weapon class; all 22 other fields are uniform. |

The 18 weapon fields are named by the original `Weapon.__init__`, as recorded
in [combat-damage-client-evidence.md](combat-damage-client-evidence.md). This
comparison does not assign meanings to the remaining eight original tuple
positions. `MinDamage`, `MaxDamage`, and `DamageType` have no numeric mismatch;
the evidence does not support swapping or rewriting their database values.

The original-`None` to zero losses occur in `reload_action_id` (422),
`ammo_class_id` (570), `clip_size` (415), `velocity` (1,863), `draw_action_id`
(134), `stow_action_id` (134), `weapon_anim_condition_code` (8), and each of
`windup_override`, `recovery_override`, `reuse_override`, `reload_override`,
and `range_type` (159). Some fields also contain genuine original zeros, so a
blanket zero-to-null migration would corrupt data. Restoring absence requires
nullable model and consumer review against the exact original row. No class
data migration was made in this pass.

Three distinct identifiers must remain separate:

| Item template | Entity class | Original weapon archetype, tuple slot 0 | Damage min/max |
| ---: | ---: | ---: | --- |
| 145 | 6048 | 1 | 55 / 55 |
| 48542 | 28126 | 1 | 88 / 88 |
| 630 | 7614 | 186 | 127 / 127 |

`WeaponClassInfo` incorrectly copied the entity class ID into
`WeaponTemplateid`. It now copies `WeaponClassEntry.WeaponTemplatId`, preserving
the original slot-zero value. This repairs a mapping defect; it does not
claim that an otherwise absent archetype-dependent system is implemented.

The requirements loader had a separate identifier error. Original
`Item.__init__`, source line 61, bytecode offsets 79–100, reads
`_itemclass.reqData.get(self.classId)`. The audited `ItemManager` loader instead
looked up those IDs directly in its item template dictionary. Comparing both
joins changes requirements for **19,576 of 30,225** loaded templates. For
example, class 6048's level-one requirement belongs to template 145 and every
other mapped template of that class. `requirements-comparison.json` records
all affected template IDs. `ItemManager.LoadItemTemplates` now groups templates
by their entity class and applies each generic requirement to all templates
of that class. Race and skill requirements retain their separate template-ID
joins. The two `ItemRequirementLoadingTests` pass, including the original
6048/145/level-one case and numeric-ID collision controls; exact validation and
fixture provenance are in [inventory-session-evidence.md](inventory-session-evidence.md).
The data itself needs no migration.

`Equipable.CanActorEquip`, original line 88, separately calls `CanActorUse`,
reads `itemTemplateSkillRequirement` by **item template ID**, rejects a missing
skill rank only when the requirement is non-null and positive, and rejects
condition zero when the item has a condition. Its raw consumer is retained.
The class-keyed generic join must not replace the correct template-keyed
skill/race joins.

## Uniform weapon templates are still an evidence gap

All 2,440 rows contain these values, also explicitly seeded in
`ItemTemplateWeaponPreloader.GetRows`:

```text
aim_rate=1, reload_time=1500, alt_action_id=1, alt_action_arg_id=133,
ae_type=0, ae_radius=1, recoil_amount=1, reuse_override=0,
cool_rate=1, heat_per_shot=2, tool_type=15, ammo_per_shot=1,
windup=800, recovery=1, refire=800, range=80,
alt_max_damage=25, alt_damage_type=1, alt_range=80,
alt_ae_radius=1, alt_ae_type=1, attack_type=2
```

No original provenance for that uniform assignment was established. The
mapped classes span 15 primary attack action IDs and many reload arguments;
240 weapon templates map to an original class with no reload action argument.
Uniformity alone does not establish each replacement value.

Original `Weapon.Recv_WeaponInfo` receives a 17-position server tuple:

```text
weaponName, clipSize, currentAmmo, aimRate, reloadTime,
altActionId, altActionArg, aeType, aeRadius, recoilAmount,
reuseOverride, coolRate, heatPerShot, toolType, isJammed,
ammoPerShot, cameraProfile
```

`GetReloadTimeMs` returns that server-supplied reload time. A class's reload
action argument is an action variant identifier, not a duration. Generated
primary action timing is independently available and covered in
[weapon-attack-client-evidence.md](weapon-attack-client-evidence.md); it does
not supply every server override, reload duration, or instance modifier.

`WeaponInfoPacket` now snapshots its fields when constructed so later ammo,
jam, class, or template mutations cannot rewrite an earlier queued update.
The existing tuple shape and scalar sources are preserved. The unknown weapon
name and reuse override remain `None`; that existing policy has not been
verified as the full original behavior. Neither this fix nor the class audit
validates the uniform template stats.

## Exact sources for the next reconstruction

`client.gameui.OnRequestTooltipForItemTemplateId` sends a server request.
`Recv_ItemTemplateTooltipInfo` caches the returned `info` through
`gameuiutil.SetItemInfo`. The analogous module request/reply caches module
tooltip data received from the server. `tooltipwindow` requests and reads
these caches; it does not derive all item stats from the generated class rows.

The original weapon tooltip augmentation has 16 fields:

```text
minDamage, maxDamage, ammoClassId, clipSize, ammoPerShot, damageType,
windupTime, recoveryTime, refireTime, reloadTime, range,
aeRadius, aeType, altFire, attackType, toolType
```

Its alternate-fire tuple has maximum damage, damage type, range, AE radius,
and AE type. Original `GetWeaponDamageInfo` actually returns maximum damage,
damage type, and tool type; an old comment claiming both min/max is stale.
The armor tooltip augmentation supplies regeneration rate, which does not
prove an item template's armor pool contribution.

Only five current `itemtemplate_armor` rows exist. Templates 13066, 13096,
13126, 13156, and 13186 map to classes 15542, 15572, 15602, 15632, and 15662.
Their current armor values are 35, 23, 59, 70, and 0; original class
minimum/maximum absorption values are 352, 234, 469, 586, and 703 respectively.
The raw class fields and server-owned item contribution are distinct inputs.
No formula proving that one can replace the other was recovered.

The supplemental generated tables provide bounded next inputs:

| Source | Established content | Missing for weapon stats |
| --- | --- | --- |
| `equipmentdata` | Slots, suits, meshes and accessory mappings | Per-instance damage, reload and modifiers |
| `rangedata` | 17 named range profiles | Selection and final instance overrides |
| `modulevariant` | 54 variant tooltip mappings | Numeric combat effects |
| `tooltip` | 2,503 symbolic tooltip IDs | Server-generated stat values |
| `generated.shared.crafting` | Module identities, strength ranks, class sets, costs and upgrade links | The formulas applying those modules to combat stats |

`read-crafting-metadata.py` reads only explicit literal constructor arguments;
it never invokes the acquired constructors. Its report retains each bytecode
offset: 7 strength rows, 5,893 module class rows, 5,380 module template rows,
54 variant rows, 160 recipe rows, 353 recipe input rows, and 205 class sets.
`ModuleClass` identifies module/template/entity IDs, variant and name tooltip,
class set, mimeogel costs and next upgrade. `ModuleItemTemplate` identifies
description, strength type and icon. Strength values are ranks 1–5, including
two special IDs with value five; they do not establish a damage percentage.

The next decisive evidence is a final-era capture of `WeaponInfo`,
`ItemTemplateTooltipInfo`, module tooltip/effect messages, and the corresponding
item identity/level/modifiers, or original server equipment/module definitions.
The client metadata can identify and validate those records. Availability,
rarity, random rolls, level scaling, module coefficients, and server override
selection remain unverified.

## Validation

The two `WorldEquipmentTests` pass in the isolated Docker workspace. They
verify the original class 6048/archetype 1 distinction and every position of
the 17-field weapon packet after mutating its source objects. The focused log
is `world-equipment-selected/focused-tests.log`; the test filter is
`FullyQualifiedName~WorldEquipmentTests`. No live database writes, migrations,
deployment, or commit were performed by this audit.
