# Boot camp Shinobi Rifle: module and instance evidence

Research recorded September 22, 2026. This is a read-only audit of the rifle
shown in the rebuilt boot camp. This research made no source, database, or
runtime changes. A later pass reconciles the final live module balance below. Exact artifact hashes, bytecode assignment offsets, and provenance are
in [the evidence record](evidence/bootcamp-shinobi-rifle.json).

The strongest reconstruction is an ordered pair of original weapon modules:
`900178` (Armor Piercing, strength 4) followed by `900312` (Photonic resistance,
strength 2). The first module supplies the original **Shinobi** name prefix.
This identifies a plausible original item configuration much more closely than
the base template alone. The IDs and ordering remain **inferred**, since the
footage does not expose the original server's item or module IDs.

## What the original footage establishes

[Video 7Lrst9SG3pk at 304.733 seconds](https://www.youtube.com/watch?v=7Lrst9SG3pk&t=304s),
capture A3-018, shows **Shinobi Rifle**, primary damage 106, optimal range 60 m,
alternate physical melee damage 82, a 20/20 magazine, and condition 100%.
It explicitly shows **Not Tradeable**, **Not Sellable**,
**[4] Armor Piercing +8%**, and **[2] Resist: Laser +12**.
The trailing unit on the resistance line is not confidently legible in the
small crop. The compatibility client's corresponding language entry includes
a percent sign; that should not be silently substituted for the observed text
or used to decide the damage-resistance formula.

These are observations of this filmed item. They do not establish whether
every crate opening granted identical modules, whether modules could vary,
or which base template the original server used. Template `13713` remains the
existing inferred counterpart. Six original templates map to its class
`27220`: `13713`, `17384`, `17443`, `17496`, `97340`, and `130284`.
The name does not uniquely identify one of them.

## Original module binding

The February 9, 2009 generated tables were checked byte-for-byte against the
original compatibility client's `data/game.zip`. This establishes what that
artifact contains, not the exact shutdown build revision.

| Property | Armor Piercing module | Resistance module |
| --- | --- | --- |
| Module class | `900178` | `900312` |
| Extracted module item template | `123146` | `123234` |
| Strength | 4 | 2 |
| Variant | 18 | 41 |
| Allowed class set | `1212`, weapons up to level 50 | `1212`, weapons up to level 50 |
| Description's base bonus | 8 | 12 |
| Name prefix | Shinobi | Luminar |
| Naming priority | 0 | 0 |

`generated/shared/crafting.pyo` assigns module `900178` at bytecode offset
`241765` and module `900312` at `245813`. Their extracted-item records are at
`418386` and `420850`. The descriptions are English tooltip `2359`, which
specifies a weapon armor-piercing base bonus of 8, and tooltip `2447`, which
specifies a weapon Photonic resistance base bonus of 12.

Class set `1212` includes rifle class `27220`; its member-list assignment is at
offset `480500`. The apparent alternatives are distinguishable: armor module
`900419` has strength 4 but bonus 4 and excludes the rifle; armor resistance
module `100029` has strength 2 but bonus 6 and excludes it; tool resistance
module `900489` has bonus 12 but its tool-only class set also excludes it.

The original `shared.craftingnew.GetModuleStrengthByModuleId`, source lines
438–442, resolves strength through the extracted module item. Legacy Shinobi
entries without such an item resolve to strength 0, so they do not explain
the displayed `[4]`. Original `client.gameuiutil.GetItemName`, lines 373–402,
selects the first loot module unless a later module has a strictly higher
priority. With two priority-zero modules, `[900178, 900312]` produces Shinobi;
reversing them produces Luminar.

The original resistance naming is internally mixed: crafting describes
**Photonic**, while the damage-type language calls it **Laser**. The initial
proposal used legacy `SERVER_LIGHT_RESIST` (`188`), whose tooltip `489` has a
percent sign. That proposal is superseded: the final client also contains
`MODULE_RESIST_EFFECT` (`413`) with tooltip `2127`, which displays a damage-type
name and a **rating without a percent sign**. Laser is argument 1, value `6`.
Armor piercing percent effect `10000052` uses tooltip `10000022`.

The filmed value alone is not final-live balance evidence. Deployment 14
changed resistance to ratings with diminishing returns and weapon/tool module
strengths to 10 per rank. The subsequent **Deployment 14.8 on Live** changed
those strengths to 6/12/18/24/30. Rank two therefore returns to **12 rating**,
consistent with the February client description. It does not grant 12%
mitigation: with no other resistance, 12 rating mitigates approximately 19.35%.
The intermediate rank-two value 20 is obsolete. The ambiguous filmed suffix is
preserved as an observation; the final client data and official hotfix resolve
the target rule independently.

The original server's module-effect tuples remain unrecovered. Proposed flat
coefficients 8 for armor piercing and 12 for resistance, with zero level
scaling, fit the descriptions and rank schedule but remain labelled
reconstruction. See [the focused semantics audit](bootcamp-shinobi-module-semantics.md)
for exact original tuple consumers, item-level scaling, official patch
chronology, and remaining scope/order gaps.

## Restrictions and alternate damage

The original `Item.Recv_ItemInfo`, source lines 187–206, receives
`hasSellableFlag` and `notTradable` with the instance's module lists.
`tooltipwindow._ShowItemFlags`, lines 1092–1113, displays the two restrictions
directly from these flags. The filmed instance therefore supports
`hasSellableFlag=false` and `notTradable=true`. It does not establish bound,
lockbox, uniqueness, or bind-on-equip flags.

The two identified modules do not explain or establish a multiplier for the
filmed alternate melee damage of 82. Its action and base-damage source remain
unresolved. The earlier [range correction](bootcamp-rifle-range.md) is
independently supported; it must not be taken as verification of alternate
damage or modules.

## Current server and a bounded implementation route

The client can derive the maker name without a new name field. At the initial
research checkpoint, the server could not preserve or send the necessary
instance configuration:

- `Item` and `ItemEntry` have no module persistence or instance flag overrides.
  `ItemInfoPacket` writes empty `classModuleIds` and `lootModuleIds` lists.
- `RequestTooltipForModuleId` is unimplemented. `ModuleTooltipInfoPacket`
  writes placeholder values and a bare nine-field tuple. The original client
  iterates the received module info and unpacks each element as a nine-field
  effect row, so the packet needs a collection of rows.
- `ItemModule` self-assigns `ModuleLevel`, and `ModuleInfo` self-assigns `Arg4`.
  These defects would lose supplied values if this currently unused path were
  activated.

The smallest sound extension would persist an ordered loot-module list and
nullable instance restriction overrides, assign them before persistence only
when crate placement `198651`, item set `19858`, creates its rifle, and reload
them through inventory reconstruction. Other instances of template `13713`
must retain their own configuration. A shared `ItemTemplate` mutation or
reconstructing modules merely from template ownership would lose that boundary.

`ItemInfo` should serialize those effective instance values, and the module
tooltip handler should answer the two original module definitions with the
original collection shape. Sale and trade checks must use the same effective
instance restrictions. Tests should cover packet fields, ordered naming,
crate-only assignment, persistence and reconnect, and unrelated rifles.

That would establish item identity and presentation, but it would not yet
implement the effects. Armor piercing and resistance need their original
combat semantics and scope, including active-weapon changes. They must not
be applied as flat damage, copied into body-armor stats, or presented as fully
working solely because their tooltip appears. No such implementation is
included in this research pass.

The later [generic module-wire repair](bootcamp-shinobi-module-semantics.md#generic-wire-repair-after-research)
fixes response framing and the constructor defects listed above. It introduces
no runtime definitions or effects. Item-instance persistence is a separate
implementation checkpoint; the initial inventory audit above describes the
research baseline, not validation of that later work.
