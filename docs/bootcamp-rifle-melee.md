# Boot camp rifle alternate melee: evidence audit

Research date: 2026-09-22. The strongest new evidence identifies the original
**rifle melee action `(174, 5)`**, its animations and its 4 m range. It does
**not recover the rifle's final-live alternate base damage**. The candidate
implements melee-only alternate requests and a labelled reconstruction of the
crate rifle's action and tooltip fields. It was deployed after an isolated
original-client tooltip check and a migration dry run against a copy of the live
database. Hashes,
assignment offsets and selected
original rows are in [the evidence record](evidence/bootcamp-rifle-melee.json).

## Damage discrepancy

Original rebuilt-camp footage `7Lrst9SG3pk`, A3-018 at 304.733 s, displays the
lootable Shinobi Rifle with 106 primary damage and **82 physical melee damage**.
The previous inferred counterpart, template 13713/class 27220, sent **25** in
its alternate tooltip tuple. Original `gameuiutil.GetWeaponAltFireInfo`, source
lines 627–635, reads this tuple; `_AddWeaponMeleeDamage`, lines 1783–1798,
passes its maximum damage directly into the displayed text. That client path
does not multiply the displayed value by skill rank or module strength.

The original class 27220 row contains primary action `(1,134)` and primary
damage 106/106; it supplies no recovered alternate damage or alternate-action
binding. `Weapon.__init__` initializes alternate action fields to `None`, and
`Recv_WeaponInfo` takes them from the server. The recovered weapon-property
enum likewise distinguishes alternate damage minimum/maximum (18/19) and
alternate action/argument (16/20). Knowing those field IDs does not supply their
lost server values. Six original templates map to class 27220, so the name,
primary damage and class do not uniquely identify the filmed server template.

## Original action and animation evidence

The February 9, 2009 generated members were verified byte-for-byte against the
recovered 1.16.5.0 `data/game.zip`. This is the compatibility artifact, not an
independent identification of the exact shutdown revision.

| Field | Original client evidence |
| --- | --- |
| Action | `WEAPON_MELEE = 174`, dispatch to `weapons.meleeattackmovement` at action-module assignment 37877 |
| Rifle argument | `WEAPON_ATTACK_MELEE_RIFLES = 5`, constant store 16155 |
| Timing/range tuple | `(200, 994, 133, 986, 4, 250, 0, 1)`, actionArguments `(174,5)` assignment 67832 |
| Windup | 200 ms, family 994 `WEAPON_MELEE_RIFLES_WINDUP` |
| Recovery | 133 ms, family 986 `WEAPON_MELEE_RIFLES_RESOLVE` |
| Range/reuse | 4 m / 250 ms; preload false, start reuse on perform true |
| Effect families | `(606,607)`, actorActionFXFamily assignment 148613 |

These tables contain timing and presentation, not a base-damage curve. Their
named rifle action is a strong **inferred binding candidate** for this rifle;
the original server's per-template binding remains absent. The existing action
catalog already contains the `(174,5)` timing row. The candidate runtime now
admits matching melee alternate requests.

The previous alternate `(1,133)` is demonstrably a placeholder: original
actionArguments assignment 43886 gives it range 20 m and animation family 490,
`WEAPON_PISTOL_RESOLVE`. The previous tooltip's alternate range 80 m was another
unverified template field. Neither is evidence of original rifle melee.

## Scaling and later live changes

Final compatibility-client Hand-to-Hand descriptions are explicit: rank 1 has
no melee bonus, followed by +10%, +20%, +30% and +40%. Skill 8's `skillLevel`
rows map to UI descriptions 2650–2654, whose English assignments are
88004/88040/88076/88112/88148. Even the maximum listed training bonus cannot
turn 25 into 82. This establishes the training schedule, not the complete
original combat formula or the server's template-tooltip calculation.

Retained community pages offer a plausible explanation of the filmed number:
the September 19, 2008 Rifle page lists level-one melee damage 68, and the
November 19 Rarity page describes modified equipment as +20%, including melee.
`68 × 1.2 = 81.6`, which can display as 82. This is **supporting arithmetic
only**: the same rifle table has a primary value that does not match the later
client, and neither page recovers the final server's exact base, rounding or
template binding. Obsolete boot-camp reward items are not used as analogues.

[Deployment 14 on Live](https://web.archive.org/web/20081115092604id_/http://www.rgtr.com/news/patch_notes/deployment_14_11112008.html)
changes resistance to ratings and updates passive-skill descriptions.
[Deployment 14.8 on Live](https://web.archive.org/web/20081220224823id_/http://www.rgtr.com/news/patch_notes/deployment_148_on_live_1.html)
then adjusts resistance-module strengths. Those changes explain the rifle's
resistance-module interpretation; they do not establish a change to its melee
base damage. A keyword survey of 62 retained later patch files found no
rifle-melee base/scaling replacement; unrelated Shocktrooper armor melee bonuses
are not applied to a recruit. PTS and live records remain distinct, and silence
in the retained notes cannot prove the absence of undocumented changes. The
archived official pages were read locally; this session's web replay attempts
were unavailable.

## Bounded conclusion

Template 13713 now carries the observed 82 Physical tooltip value and the
original client's rifle-melee `(174,5)` action and 4 m action range. The
template identity and server minimum damage are still unverified; runtime uses
82 as a fixed attack amount until that range and its scaling are recovered.
The earlier 25 was an unsupported emulator placeholder. Alternate melee
consumes no ammunition, and melee actions cannot hit a target beyond their
original action range at impact.
Other alternate attack families remain separate evidence gaps. The Armor
Piercing and Laser resistance modules do not establish a multiplier that
resolves the damage discrepancy.

## Original-client check and deployment

An isolated copy of the saved level-2 character logged into the recovered
1.16.5.0 client against the new server build. The equipped rifle tooltip visibly
shows **106 Physical Damage** and **82 Physical Melee Damage** in capture
`playthrough-18/13-rifle-tooltip.png`. The copied world database applied
`BootcampRifleMelee` and stored action `(174,5)`, 82 damage and 4 m range.
An alternate click in the medical tent had no target, and the server trace
recorded no attack request. This first pass only verifies the tooltip.

The same build migrated a separate copy of the live world and character
databases, reached `Server ready!`, and produced the same rifle row. Before
deployment, SQLite backups of the live databases were saved under
`/home/blizz/backups/rasa-net/deployments/20260922-rifle-melee/`. The game
container was recreated with image `rasa-dit-test:20260922b`; it reached
`Server ready!`, and the mounted live database records
`20260922235000_BootcampRifleMelee` with `(174,5)`, 82 and 4 m. The exact
capture, binary, log and backup hashes are in
[the client verification record](evidence/bootcamp-rifle-melee-client-check.json).

## Alternate attack with an empty or jammed rifle

The recovered client bytecode shows that `BaseWeaponAttack.CheckAction`
(source lines 143–155) checks jam and an empty magazine only for a primary
attack. `Actor.PerformAltWeaponAttack` (lines 578–584) reloads an alternate
attack only when its action uses ammunition. `MeleeAttackMovement` (lines 5–9)
sets `useAmmoInAction=0`. The server previously rejected an alternate melee
request when the rifle was empty or jammed, despite charging zero ammunition
for a successful request. `WeaponAttackManager` now applies those two guards
only to primary attacks and keeps the zero-ammo alternate intact through
windup. The original client files and hashes are in
[the rifle evidence record](evidence/bootcamp-rifle-melee.json).

All **33** weapon attack lifecycle tests pass under .NET 5, including an empty
and a jammed alternate rifle. Image `rasa-dit-test:20260922c` carries the fix;
the live game reached `Server ready!` and remains running. This validates the
emulator rule against the recovered client path. The empty and jammed cases
still need original-client playthrough checks. An earlier diagnostic run moved
the practice dummy in an isolated database, but its client process ended before
a targeted request was captured; the seeded and live placements were unchanged.

## Targeted original-client pass

In a later isolated pass, a GM command spawned a level-2 Thrax Infantry
Initiate beside the saved character. The first F press drew the holstered rifle;
three further F presses produced three `RequestWeaponAttack` calls in the game
trace. The client showed the Thrax die and award **71 XP and 10 credits**.
The rifle remained at **15/1000** rounds, and the isolated character database
corroborates XP **3000 → 3071**, credits **300 → 310**, and rifle ammo **15 → 15**.
[The targeted-client record](evidence/bootcamp-rifle-melee-targeted-client.json)
holds the frozen screenshots, log, databases and hashes.

This verifies a targeted melee exchange through the recovered compatibility
client and current emulator. The GM-spawned medical-tent encounter is diagnostic;
it does not establish the original server's damage formula, hit chance, creature
health, reward amounts or exact shutdown-client revision. The game trace names
the request method but does not print its arguments, so individual hit amounts
remain unmeasured.
