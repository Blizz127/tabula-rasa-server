# Skill evidence from the recovered 1.16.5.0 client

Research date: 2026-09-12. The preservation target remains the final live game
immediately before shutdown. [Client artifact provenance](client-artifacts.md)
records the archive acquisition, package caveats, embedded executable version,
and outer-file hashes. These findings come from static inspection of that
package's Python 2.4 bytecode, magic `6df20d0a`; no acquired game module was
executed or imported.

Source and generated findings are under
`/home/blizz/backups/rasa-net/research/20260912-client-artifacts/skill-selected/`.
`uncompyle6` 3.9.3 supplied readable reconstructions; `xdis` 6.1.7 disassembly
checked the instructions supporting the changes. Literal tables were read with
`ast.literal_eval`, without running reconstructed modules. Offsets below are
decimal offsets within the named code object's bytecode, not offsets in the
compressed archive. Original source line numbers come from code metadata.

| Original ZIP member | SHA-256 of uncompressed `.pyo` |
| --- | --- |
| `data/game.zip:generated/client/skilldata.pyo` | `decd69b01ef07189edb8a6f9f1bcab4facfdd576238dacb400c935ae6ef8b05c` |
| `data/game.zip:generated/client/characterclass.pyo` | `e7d4148bc03ed4cdded8ab4a222ec9f3615814515b91791cf59171134de5f325` |
| `data/game.zip:generated/client/abilitydata.pyo` | `3270c0347c8fb234d71b28312a3b618aa7d2b77fd0b952688b2c2dcc9d71fbef` |
| `data/game.zip:generated/client/logosstone.pyo` | `08a6d29c5dcd96972ebd5e47b277707a780d28f99c48b62ceca32c48466c5235` |
| `trpython.zip:client/gameuiutil.pyo` | `0a6543c60a39f8be64040c598216d606b0c211f9411dff96308b1deaff17df7b` |
| `trpython.zip:client/ui/skillsabilitieswindow.pyo` | `43f363de590df3b662fc2f6215d08513f0763f3bc7174aca851b9314658b42c4` |

## Catalog, ancestry, and maximum ranks

`skilldata.skillData`, `skillCharacter`, and the ID/name dictionaries each have
**73 active catalog entries**. All 73 IDs and owning classes match the existing
`SkillTraining.SkillIds` and `ClassSkills` tables. The class IDs in
`characterclass.pyo` and ancestry in `gameuiutil.g_ClassTree` also match the
existing Recruit to tier-two to tier-three to tier-four branches. Firearms is
directly confirmed as **ID 1**. The older C++ catalog is no longer the only
source for these mappings.

The module's `skillData` table is stored at bytecode offset **3519** and
`skillCharacter` at **4620**. `gameuiutil.GetSkillMaxPumpLevel`, original line
**907**, reads `skillData[skillId][4]`. That field is **1** for the eight signature
skills and **5** for every other active catalog skill:

| Class | Signature ID | Internal signature name | Maximum rank |
| --- | ---: | --- | ---: |
| Demolitionist | 20 | Explosive Wave | 1 |
| Grenadier | 47 | Concussive Wave | 1 |
| Guardian | 92 | Shield Wave | 1 |
| Spy | 110 | Cloak Wave | 1 |
| Sniper | 149 | Crit Wave | 1 |
| Medic | 154 | Regeneration Wave | 1 |
| Exobiologist | 156 | Reanimation Wave | 1 |
| Engineer | 157 | Base Wave | 1 |

`gameuiutil.IsSignatureAbilitySkill`, original line **725**, independently
enumerates these eight IDs; offsets **3–60** build the list and test membership.
`skillLevel` has 359 descriptive rows, including leftover IDs absent from the
active catalog. Those leftovers must not be imported as extra trainable skills.

## Signatures are outside ordinary skill purchases

The skill window's `_LoadSkills`, original line **654**, uses the
`SignatureAbilities` widget for signatures. At offsets **1179–1185** it branches
around registration of the Plus and Minus handlers for those widgets.
`_UpdatePlusMinusBtns`, original line **943**, explicitly skips signature rows
at offsets **98–111**. This is stronger evidence than a skill name or an empty
fansite rank table.

For ordinary skills, `OnPlus`, original line **321**, previews one additional
rank only if it is within the maximum and affordable. It adds the new rank to
the pending dictionary and charges **the number of the rank purchased**; its
rank comparisons occur at offsets **129** and **145**, and the pending rank is
stored at **221**. This directly corroborates cumulative costs
`0, 1, 3, 6, 10, 15`.

`OnAcceptBtn`, original line **234**, passes only that pending dictionary's
items to `client.gameui.OnSkillsLeveled` (call preparation starts at offset
**77**). `OnSkillsLeveled`, original line **1510**, calls
`SendCallActorMethod('LevelSkills', (skillList,))` at offsets **0–18**. Its source
member has SHA-256
`451bb70f10faced1261c74ad6452057711f230dc0798faf924b25911257d84d0`.
Thus the ordinary purchase path provides no signature purchase control.

The server now rejects signature increases through ordinary `LevelSkills`
planning, rejects requested or existing signature ranks above one, and keeps
valid unchanged rank-one records intact. It does not invent a signature grant,
cost, class-promotion reward, or respec behavior. The original signature grant
mechanism and point accounting remain separate evidence gaps.

## Tactical Evasion is an active ability

The complete comparison of `abilitydata.skillRequirements` against the existing
skill-to-ability map found one mismatch: **skill 54, Tactical Evasion, maps to
ability 10000005**, whereas the server marked it as having no ability (`-1`).
`gameuiutil.GetAbilityFromSkill`, original line **786**, reads this exact table.

This is not only descriptive metadata. In `generated/client/actiondata.pyo`
(SHA-256 `9520744380868987a093be20bdbca7de26cd7899e07c50c23f00ae3d764c1ecb`),
module offset **1686** declares `AA_RANGER_TACTICAL_EVASION = 10000005` and
offset **42307** starts its action-module entry, pointing to
`abilities.tacticalevasion`. The corresponding client implementation,
`client/actions/abilities/tacticalevasion.pyo` (SHA-256
`14433c4eaf1fa760e143bd5af44bed79aa192ca70bdfe0db99c6dad838a2661e`),
defines a self-targeted `TacticalEvasionAction` and its game effects.

`client/augmentations/actor.pyo` (SHA-256
`2ecb129d5071ac588e076415857ea4e5b3ebae08a964e2828b885089ac316c4f`)
stores server-advertised `(abilityId, pumpLevel)` pairs in `Recv_Abilities`,
original line **1773**, offsets **0–40**. `GetAbilityPumpLevel`, original line
**1809**, reads that dictionary. The skill window's `_CanUseAbility` checks this
value before allowing an action. Advertising the correct ability therefore
matters to use of the skill, not just its tooltip.

The mapping is corrected for future training, with a test that decodes the
resulting `AbilitiesPacket` and checks ID 10000005 and its rank. Existing saved
skill-54 rows may still contain the previous `abilityId = -1`; those require a
read-only inventory and explicit reconciliation before any migration. This
change does not implement Tactical Evasion's server-side effects or prove their
combat behavior.

The read-only inventory immediately before this deployment found zero rows in
the live `character_skills` table. No saved mapping repair was needed here.
All eleven added signature/mapping cases passed in the combined 105-test run;
deployment and backup details are in [the work log](retail-accuracy.md).

## Level and Logos findings, with limits

`skillCharacter` stores class ownership with levels **1, 5, 15, or 30** according
to class tier. It has no per-rank level table. The inspected skill-window Plus
path checks class membership, available points and maximum rank; it does not
check character level or Logos. This does not prove that the original server
had no additional validation, and it does not justify inventing per-rank level
thresholds.

`logosstone.logosSequences` supplies **54 ability-to-Logos sequences** with exact
numeric IDs. For example, Lightning ability **194** requires Logos **23
(POWER)**; Tactical Evasion **10000005** requires **1 (AREA), 7 (DEFEND), and
4 (CHAOS)** in that displayed sequence. `Manifestation.HasLogosForAbility`,
original line **1553**, checks every listed ID against the player's Logos set
(membership test at offset **49**). Its member
`client/augmentations/manifestation.pyo` has SHA-256
`724f68eb70e5a47338db82977c55abfa09f1b187f86a0b9a560c012a8588886d`.
The skill window uses this result to disable ability icons and color tabs,
while the ordinary purchase buttons remain independent of Logos. Do not turn
this client ability-use gate into a guessed skill-purchase prerequisite.

The complete factual join of all 73 skills, owners, class-level fields,
maximum ranks, mapped abilities and Logos sequences is preserved in
`skill-selected/client-skill-catalog.json`, SHA-256
`c83c2cefcffad479decb29dc6c85e8920d01f0176117428119a4af6b3da9230c`.
`skill-catalog-comparison.json` records the pre-change comparison;
`skilldata.dis`, `gameuiutil-selected.dis`, `skills-window-selected.dis`,
`skill-send.dis`, `skill-logos-check.dis`, and `actor-abilities.dis` preserve
the instruction-level checks. These demonstrate the recovered client's rules;
official whole-client authenticity, server-side grant mechanics, effects,
point awards, and in-client end-to-end behavior remain under verification.
