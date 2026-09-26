# Original-client Gearing Up handoff and promotion

Frozen isolated-server run 13 on 2026-09-22 completed mission 1992, **Gearing Up
for Battle**, and the first promotion objective of mission 1994, **Capture the
Flag**. Alden Vanguard reached level 2 with 3000 XP and 300 credits, then logged
out with the cave objective pending. This verifies the implemented handoff in
the recovered original client; it does not establish final-live fidelity for
the reconstructed NPC placements or the intervening route.

This run used the earlier **1281-test server binaries**, not the subsequent
1300-test build containing module and boss-counter recovery changes. The
[evidence JSON](evidence/client-gearing-handoff.json) records the frozen binary
input manifest and its game assembly hashes. No code, content, or tests were
changed for this documentation pass.

Artifacts remain in
`/home/blizz/backups/rasa-net/research/20260922-client-playthrough/playthrough-13/`.
The operator used ordinary movement and NPC interactions, without GM teleporting
in this run. Captures 06–14 preserve the return from Hartmann through the camp
and causeway to DeSimone. Walking this path verifies current accessibility; it
does not establish that DeSimone stood there in the original service.

| Captures | Client observation | Persisted corroboration |
| --- | --- | --- |
| 04–05 | Hartmann's final dialogue sends the recruit to DeSimone; Continue changes the tracker to Gearing Up for Battle (complete). | Before-state has only objective 7 incomplete. At 21:48:11 UTC, `hartmann-objective-state.json` has all nine objectives completed. This is readiness for turn-in, not the mission payout. |
| 15–16 | DeSimone's completion window displays 200 credits. Completing it adds the 1250-XP and mission-completed chat lines. | At 22:02:44 UTC, `gearing-reward-state.json` records mission 1992 completed, level 1, XP 2500 and credits 300. The inherited checkpoint was XP 1250 and credits 100. |
| 17–18 | DeSimone offers Capture the Flag, then presents the promotion conversation and training instructions. | Mission 1994 is accepted; its first objective is to speak to DeSimone for a promotion. |
| 19–20 | The client announces 500 XP, level 2, three attribute points and two skill points. The K panel displays Training Pts: 2; the tracker says Find a way out of the cave. | At 22:05:15 UTC, `promotion-state.json` records level 2, XP 3000, credits 300, objective 4 completed and objective 2 incomplete. Body, Mind and Spirit allocations remain zero. |
| 21–22 | Ordinary logout returns to character selection, which displays Alden at level 2. | At 22:11:22 UTC, `logout-persisted-state.json` retains completed missions 1990/1992, active mission 1994, completed objective 4 and incomplete objective 2. |

No points were spent: all five starter skills remain rank 1, and all three
attribute allocations remain zero. This pass verifies the displayed award and
unspent persistence, not training a skill or spending an attribute point. Rifle
ammunition remains 17 with 1000 reserve cartridges. Logout preserves map 1985,
position `(387.24609375, 127.27734375, 40.13671875)` and yaw
`0.33421608805656433`. Saved tracker option 55 changes from `"1,992"` before the
handoff to `"1,994"` at logout. This record ends at character selection; subsequent
reconnect and cave traversal belong to later runs.

The final process status is terminal. Auth, game and Xvfb exited with code 0;
the client exited with -15 when the harness stopped it after ordinary logout.
The frozen client log contains one `ERROR:` line, the previously unresolved
`TypeError: argument list must be a tuple` at line 7, and 33 warning lines.
There is no skill-tooltip or elemental-icon traceback in this run. The remaining
warnings concern input-state callbacks, texture/effect fallback and missing
embedded light templates. The absence of those traceback signatures is bounded
to these interactions, not proof that every tooltip path is correct.

The surviving original comparison is narrower than this continuous emulator
sequence. Video `7Lrst9SG3pk` shows the 200-credit offer at A2-050, 289.8 s, and
the 1250-XP completion, Capture the Flag acceptance, 500 XP and level 2 at
A3-066, 362.133 s. The edit from **362.067 to 362.133 s** hides the final
Hartmann/DeSimone handoff and promotion interaction. Original client
`objectiveconversation` binds final Hartmann objective `(1992,7,2563,1,1)` to
text 21494 and promotion `(1994,4,2562,1,1)` to text 21694. Thus the dialogue
identity and observed reward totals have original evidence, while exact NPC
placement, route and timing remain reconstructed. Older footage is not itself
proof that every value remained unchanged until shutdown.

The earlier equipment, Practice Dummy and Lightning checks are in
[the Gearing Up playthrough](client-gearing-playthrough.md). The remaining
ordered mission 1994 route and its evidence limits are in
[the Capture the Flag audit](client-capture-the-flag-audit.md). **Mission 1994
is not completed in run 13; cave traversal, boss combat and Youngblood remain
pending.**
