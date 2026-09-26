# Recruit clothing admission audit (2026-09-22)

The original client raised three `TypeError: iteration over non-sequence`
exceptions while displaying the newly worn Recruit outfit. The server sent the
Equipable tooltip tuple as `(None, None)` because these templates have no skill
requirement. Original `gameuiutil._LoadElementalIconForItemTemplate`, source line
1397, bytecode offsets 166–188, unconditionally iterates `GetEquipableResistances`.
That getter, source line 531, offsets 47–54, returns the tuple's second member
directly. `HasEquipableInfo` checks whether the augmentation exists, so a tuple
containing `None` does not bypass the iteration.

The first repair sent `(None, [])` for these equipables. This fixed the
resistance-list field but left a separate skill-field shape defect, identified
by the later hover audit below. No resistance or armor value was invented. The
reproduced exceptions are recorded at lines 32–47 of the isolated original-client
archived log under `20260922-client-playthrough/playthrough-03/tabula_rasa.log`.

The three server `UpdateStatsValues: ... non_armor` errors are a separate false
assumption in the stat loop. Original `entityclass` maps Recruit Boots, Legs and
Vest (templates 122854/122855/122856) to classes 10000068/10000069/10000070, with
augmentations `[4,6]`: Equipable and Item. `equipmentdata` assigns their valid slots
2/16/15. They have neither augmentation 51 (Armor) nor any `armorclass` row.
Their item durability values 35/59/70 are not player armor absorption. Original
comparison tooltips at 321.467, 324.467 and 332.467 seconds in `7Lrst9SG3pk` also
show the Recruit pieces without Body Armor or regeneration fields.

The stat loop now silently accepts recognized equipable clothing with no armor
class, retaining a diagnostic when both armor and equipable metadata are absent.
Armor calculation and resistance accumulation are unchanged. Current seed data
contains no resistance rows for these three templates; no missing original
clothing resistance is established by the admission errors.

Three serialization regressions decode the actual Recruit class shapes and
verify absent skill requirements, empty resistance lists, unchanged durability
and no invented Armor augmentation. A stat regression wears all three pieces,
checks zero armor, armor regeneration and resistance, and checks that corrupting
the equipable metadata still produces a diagnostic. All four cases fail against
the preceding production code in 2.6150 seconds: the three tooltip cases reject
`None` where a list is required, and the stat case captures all three false
warnings. Baseline log: `/tmp/rasa-retail-20260922-clothing-before.log`, SHA-256
`4f020dcb95e053a5358c433d617dbeea7fe29673871f4d383b147aace06c61f4`.
The full fixed-code suite passed **1,232/1,232 tests**, with zero skipped, in
5.0007 minutes. Log: `/tmp/rasa-retail-20260922-opening-final.log`, SHA-256
`e457c9b845f0385b6eee9696af069bc86ec2b2ce594d370a4dd3810937066c3a`; the matching TRX is retained alongside it.
The TRX includes 134 data-test parent entries in addition to the 1,232 leaf
outcomes, giving 1,366 passed result entries. This run precedes the subsequent
spawn-heading data correction.

Original-client admission run 04 successfully entered Luna Cavern. At
2026-09-22 19:51:05 UTC, the captured 5,882-byte client log (31 lines) contained
zero elemental-icon iteration errors; the prior run contained three. After the
run stopped, its complete archived `playthrough-04/tabula_rasa.log` was byte-for-byte
identical to this capture. It retains
one unrelated, unresolved error at line 7: `TypeError: argument list must be a
tuple`. The complete archived client-log SHA-256 is
`433a3830bc58b383d110cb3b74050ac0ed09f9cb39705083a4734706aeab673b`.
The 42,810-byte server-log prefix (336 lines, 14:47:52.961–14:50:52.196 America/Chicago)
contains no error lines and no `non_armor` warnings; its SHA-256 is
`31dbe70c420ca0ff1754ff5c00544d9ec03d3471be1682b3dd1fa6cfee8aa919`.
The server-log claim covers the stated prefix; the client claim covers the complete
archived file. `playthrough-04/admission-retest.json` records the stopped-run check.

The frozen prefixes and capture record are retained under
`20260922-client-playthrough/clothing-admission-audit-04/`.
`playthrough-04/02-half-pi-before-input.png` visibly shows the worn Recruit outfit
and Pistol HUD 20/1000 (SHA-256
`57c22ebfe77f0d30a2078fd29328858b7aa31f465412fd57fd99c77abcdb71cc`).
This confirms the bounded client repair, not complete original-server fidelity.

The later run 06 exposed three item-tooltip errors, `TypeError: unpack
non-sequence`. Original `tooltipwindow._FormatIndividualItemTooltip`, source
line 743, offsets 424–451, passes `GetEquipableSkillRequirements(classInfo)` to
`_AddSkillSlot`. The getter returns EQUIPABLE field 0 directly (source line 542,
offsets 47–54). `_AddSkillSlot`, source line 1564, offsets 248–257, unconditionally
unpacks that field into two values. It explicitly accepts `None` for the skill
identifier and level at lines 1566–1569. The separate icon power-level consumer
also unconditionally unpacks the pair at `gameuiutil._GetClassPowerLevel`, source
line 1358, offsets 58–73. The earlier assertion that scalar `None` was valid for
this field was incorrect.

The packet now sends **`((None, None), [])`** when no skill requirement exists.
Existing skill requirements and resistances retain their values. The three
Recruit serialization cases now require this nested pair, and a synthetic
equipable case verifies that present skill and resistance data remain intact.
No requirement, armor or resistance is added to the original clothing. The five
focused cases were run against the same isolated snapshot before and after
copying only the serializer correction. Before the fix, all three Recruit cases
failed when reading the required tuple; the present-value and stat cases passed
(2 passed, 3 failed, 1.5342 seconds). After the fix, all five passed, none skipped,
in 1.6888 seconds. The logs are
`/tmp/rasa-retail-20260922-clothing-skill-before.log`, SHA-256
`66ed2b5054908df2213bdb889affe4c02001bcdfe8e83afaba448a2c82b184df`, and
`/tmp/rasa-retail-20260922-clothing-skill-after.log`, SHA-256
`1dcdf03c31c3cf6ee7fe7b798d170d887f15b869ebedfa18b85052b9ef2317a4`.
The subsequent combined suite passed **1,251/1,251 tests**, none failed or
skipped, in 6.2316 minutes. Log:
`/tmp/rasa-retail-20260922-gearing-verified.log`, SHA-256
`ea2037d8e7cb20beae9a390a8316924ed2050d5f68ffedfaca3a15886d038ac2`.
All 2,056 source entries still match the completed test snapshot, including
1,978 C# and project files. The evidence record retains its exact snapshot hash.
Earlier passing suites asserted the old scalar shape and did not validate this
correction.

The run 06 log prefix was frozen at 20:38:02 UTC in
`20260922-client-playthrough/clothing-skill-tooltip-audit-06/client-prefix.log`:
8,560 bytes, 80 lines, SHA-256
`6faf041a6a9369b7c6cc44fde0062e498377952f49439d399f78d862a72d8d4d`.
Lines 53–79 contain the three item-tooltip tracebacks. Their exact hovered
template IDs and timestamps are absent, so attributing each error to a specific
Recruit piece remains unverified. This error is separate from the startup
callback tuple error and the projection warning.

Original-client run 07 verified all three clothing tooltips after the fix.
The independently viewed captures `playthrough-07/02-recruit-boots-tooltip.png`,
`03-recruit-legs-tooltip.png` and `04-recruit-vest-tooltip.png` show each Recruit
piece beside its equipped Motor Assist Armor comparison, without adding skill,
body armor or regeneration fields to the Recruit item. Exact screenshot hashes
are recorded in the evidence manifest. The frozen 8,468-byte, 63-line
`playthrough-07/tooltip-retest-client-prefix.log` has SHA-256
`cddab068fa7ef4e28685304bf79679b5b077f90ed1ff08b013d1a65eb1fd5677`.
It contains no unpacking or iteration errors. One separate startup tuple error
and 30 projection warnings remain. This verifies the bounded hover correction
in the isolated emulator; it does not certify all original-live behavior.

Source hashes, exact original table assignment offsets and consumer bytecode
locations are in [the evidence record](evidence/recruit-clothing.json). The client
is the repository's 1.16.5.0 compatibility artifact compiled in February 2009;
its exact relationship to the shutdown build remains unverified.
