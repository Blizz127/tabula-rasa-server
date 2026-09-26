# Shinobi Rifle module semantics and final live revisions

This September 22, 2026 read-only review corrects the resistance effect proposed
in [the rifle identity audit](bootcamp-shinobi-rifle.md). It establishes final
rating and display semantics more firmly; it does not claim to have recovered
the original server's combat implementation. Exact hashes and source locations
are in [the evidence manifest](evidence/bootcamp-shinobi-module-semantics.json).

## Resistance chronology

The [official live Deployment 14 notes](https://web.archive.org/web/20090218143923id_/http://eu.playtr.com/en/news_article/deployment_14_patchnotes_known_issues_november_2008_live)
introduce resistance ratings and diminishing returns. They initially set
weapon/tool resistance modules to 10 per rank. That intermediate value is
superseded by [Deployment 14.8 on Live](https://web.archive.org/web/20081220224823id_/http://www.rgtr.com/news/patch_notes/deployment_148_on_live_1.html),
which sets the five ranks to **6, 12, 18, 24, and 30 resistance**. Its explicit
live heading and introductory statement distinguish this from test-server
notes. The retained capture is dated December 20, 2008; this audit does not
infer the exact hotfix deployment day from that archive timestamp.

Thus the filmed rank-two `+12` is compatible with the later live **12 rating**.
It is not proof of 12% mitigation. The February compatibility client's original
`shared.damageresistance` implements `100 / (100 + 2R)` for nonnegative ratings,
and `(100-R)/100` for negative ratings. At zero other resistance, adding 12
rating yields roughly 19.35% mitigation. The conversion is original; where it
fits among armor piercing, filters, absorption and other modifiers remains a
separate question. No retained later note examined here replaces the D14.8
rank schedule, but surviving notes are not proof that every final hotfix is
available.

## The correct module tooltip candidate

The initial audit found legacy `SERVER_LIGHT_RESIST` effect 188, whose tooltip
489 retains a percent sign. The same original client also contains a more
specific and better-supported candidate:

| Field | Original value |
| --- | --- |
| Effect | `MODULE_RESIST_EFFECT`, 413 |
| Effect class mapping | `gameeffects.loot.resists.ModuleResistEffect` |
| Tooltip | 2127 |
| English text | `Resist: $damageType%(arg1)s %(amount)s` |
| Laser damage type | 6 |

The generated effect-name store is offset 1605; its class and tooltip mappings
are offsets 13133 and 21047. English tooltip 2127 is assigned at offset 14258.
`_AddModuleEffects` supplies `arg1` to the first substitution. Original
`clientlanguagemanager._SetupSecondaryTokens`, source 773–774, builds
`damageType6` from the damage-type language table. The later templated
substitution resolves that to Laser. No percent removal or resistance-specific
flag is needed. The old 188 mapping is therefore a weaker candidate, rather
than evidence that the final module intentionally displays an incorrect unit.

The client archive does not include the mapped `gameeffects/loot/resists.pyo`
implementation: its loot package contains only `__init__`, `powercost`, and
`reload`. The generated name is not a recovered server implementation. Using
413, argument 1 = 6 and base amount 12 is a strong **inferred** reconstruction,
not an original captured tuple.

## Original packet shape and scaling

`_AddModuleEffects`, source line 2248, iterates the packet's module-info
collection and unpacks every row into exactly nine fields:

```text
(effectId, setLevel, flatValue, linearValue, expValue, arg1, arg2, arg3, arg4)
```

At source 2260–2262, bytecode offsets 487–550, it calculates:

```text
amount = int(ceil(flatValue + linearValue * itemLevel
                  + expValue * 2**((itemLevel - 1) / 8.0)))
```

Here `itemLevel` is the item's required level: the caller reads `GetReqLevel`
at source 870–874, offsets 2100–2147. It is neither the wearer level nor the
module rank. Positive amounts receive a leading plus sign. A nonzero set level
uses `(n)` as a prefix; otherwise the normal module prefix is `[n]`.

`gameuiutil.GetModuleLevel`, source 210–214, returns the crafting lookup's
module strength. Although `SetModuleInfo` caches the received module level,
this getter does not use that cache. The documented rank 4 and rank 2 display
comes from original module definitions, not an invented item-instance level.

Flat 8 with zero scaling for armor piercing, and flat 12 with zero scaling for
resistance, reproduce the original base descriptions and final resistance
rank schedule. Those coefficients and unused argument representations remain
inferred because the original server module-effect rows are missing. The
scaling formula alone cannot recover them uniquely from a level-one tooltip.

## Combat scope and remaining boundaries

The original armor-piercing effect is explicitly named
`LOOT_ARMOR_PIERCE_PERCENT`, 10000052. The original crafting description says
it improves the weapon's armor penetration; contemporary client ability text
also describes armor piercing as a fraction of damage going directly to
health. This supports bypass rather than flat added damage. It does not
establish exact rounding, modifier combination, or all damage-type exceptions.

Two original live patches impose useful constraints. The April 8, 2008
1.6.6.1 notes remove equipped TSR/injector armor piercing from ability damage.
Deployment 9.6 states that EMP against biological targets ignores piercing
modifiers and applies damage to armor. An unrestricted actor-wide piercing
bonus applied to every outgoing ability would conflict with these rules.

Weapon modules affecting the user should not be accumulated merely because
weapons are carried. The D14 live fix for regeneration changes while switching
weapons supports activation linked to the selected weapon, but that is
**indirect evidence for resistance**. The exact active-drawer versus drawn
weapon boundary, switching behavior and in-flight attack snapshot remain
unverified. No resistance contribution should silently become permanent body
armor data.

The current server already has the original resistance conversion helper.
Its player hit path applies resistance before armor; the creature path applies
its existing Paint Target piercing separately. These are current implementation
facts, not proof of the original complete modifier order. Adding the rifle's
module effects must account for both paths without presenting the existing
pipeline as recovered retail code.

The research above made no production edits, builds or client launches.
The original table and Python members used here were compared byte-for-byte
with the complete compatibility-client archives. Original-client display and
combat validation of the revised module candidates remains outstanding.

## Generic wire repair after research

A bounded implementation now replaces the placeholder module packet with the
original collection of nine-field rows. `ItemModule` copies its supplied rows;
`ModuleInfo` is immutable and preserves all nine fields, including the formerly
self-assigned fourth argument. Module level likewise retains the supplied value.
Fractional numeric coefficients are supported, and nullable integer arguments
and level remain distinct from zero. Other possible argument types remain an
explicit extension point requiring recovered data.

The original `gameui.OnRequestTooltipForModuleId`, source 1601, offsets 101–116,
sends exactly `(moduleId,)`; the request decoder now requires that shape. The
response consumer at source 1612–1615 caches and republishes the supplied
collection. Its request-side test is `GetModuleInfo(moduleId) is None`, so an
empty collection would be remembered as a known definition with no effects.
The server has no recovered runtime definitions loaded yet. Accordingly its
handler reports the unsupported ID and sends no fabricated empty response.
No placeholder coefficients, module-pair seeds or combat effects are activated.

Seven regression cases exercise multiple rows, all nine fields, fractional and
negative coefficients, null-versus-zero, retained level and fourth argument,
immutable queued data, null-versus-empty definitions, and original request
routing/framing. The parent serial full suite passed **1,300/1,300 tests**, with none skipped,
in 5.9650 minutes, including all seven module-tooltip cases. All six production
and test files in this change still match the tested 2,111-file snapshot.
The exact log and snapshot hashes are recorded in the evidence manifest. This
verifies the wire implementation; no original-client module rendering, seeded
module definition or combat effect is claimed.

## 2026-09-22 Training Day follow-up

The generic handler now answers requests for the two evidenced Training Day
modules only: 900221 and 900256. Their original-client effect IDs are 9 and
115; the observed tooltips provide −15 for 15 seconds. The flat coefficient
and unused packet fields are inferred server values. An isolated recovered
client displayed both filmed effect lines; see the
[Training Day module record](evidence/training-day-reward-modules.json). The
Shinobi rifle module discussed above still has no runtime definition, and
neither reward module has a reconstructed combat proc chance or application.
