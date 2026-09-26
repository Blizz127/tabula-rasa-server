# S2 Lightning lesson hit credit — 2026-09-24

Mission 1992 objective 8 asks the recruit to use Lightning on the Target Dummy.
The retained compatibility client's objective name 21666 is “Use your Lightning
power on the Target Dummy”; Corporal Hartmann's conversation 21665 instructs the
player to target the dummy and fire Lightning. These are original client text,
not recovered server completion rules. Their bindings are in
`docs/evidence/bootcamp-client-catalog.json`: `missionobjective[1992,8]` at
store offset 106856 and `objectiveconversation[1992,9]` with package 2563 at
store offset 40853. The client mission-text member SHA-256 is
`06292bf592ce1ab241771ac3317125e8545c608781822fb345809b70f863bf9d`.
The exact shutdown client revision remains unverified.

The lesson is absent from the surviving final-week video `7Lrst9SG3pk`: the
edit between 362.067 and 362.133 seconds jumps from the firing range to later
mission progress (`A3-065` in `docs/evidence/bootcamp-d11-footage-events.json`).
The prior seed required a *destroying* Lightning hit. That requirement was an
inference from the earlier Practice Dummy lesson; neither original text says
the Target Dummy must be destroyed. The Target Dummy's 100 HP is also an
analogue. In this emulator, normal rank-one Lightning at level one starts at
180–240 damage (`LightningAbilityData.GetBaseDamageRange`), so it destroys that
100-HP target in one hit absent mitigation. The correction primarily matters
when a successful Lightning hit leaves the target alive.

`BootcampLightningHitCredit` changes only binding `(1992,8,0)` from
`destroying_hit_only=true` to `false`. It preserves target placement 198653 and
action 194. Crediting the first **damaging** Lightning hit is an **inferred**
reconstruction from the original instruction, with medium confidence; it is
not a filmed completion threshold. The runtime had a separate implementation
defect: `DamageContentUsable` returned for every surviving hit before notifying
hit bindings. It now notifies on a positive change in HP, passing `destroyed=false`;
zero-damage attempts and other actions do not complete the lesson.

The correction has paired SQLite/MySQL migrations and a rollback to the old
inferred predicate. Focused tests verify the migration's exact key/column and
the runtime's other-action, zero-damage and first-damaging-Lightning cases.
The first original-live Lightning completion packet, if recovered, should
replace this inference and may require a different threshold.

The central reconstruction manifest's `(1992,8,0).destroying_hit_only` field
must change from `true` to `false`, retain tier `inferred`, cite original
mission-text IDs 21665/21666 and this note, and state that the original
completion event is unfilmed. No other S2 manifest field changes.
