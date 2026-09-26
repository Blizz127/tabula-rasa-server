# Attribute allocation reply ordering

Original-client playthrough 14 reproduced a stale counter after spending
attribute points. At level two, Alden had three points and Body/Mind/Spirit
12/12/12. The preview allocated one point to each, showing 13/13/13 with zero
remaining. After acceptance, the attributes remained 13/13/13 but the window
showed three available points again. The saved character correctly had spent
values 1/1/1. This was an original client running against the isolated emulator,
not footage of the original live service.

The original consumers establish the cause. `Actor.Recv_AttributeInfo`, source
768, announces each attribute immediately after storing it.
`ActorAttribute._AnnounceChange`, source 72–77, posts `ACTOR_ATTRIBUTE_UPDATE`.
The attributes window handles a Body, Mind or Spirit update by calling
`_LoadBMSValues` at source 226–227. That function copies `avatar.attributePoints`
into the window's local available-points cache at source 1025, offsets 54–63.

`Manifestation.Recv_AvailableAllocationPoints`, source 1187–1192, updates the
avatar cache and posts `UI_UPDATE_CHARACTER_ADVANCEMENT`. The attributes window
does not subscribe to that event. Sending `AttributeInfo` first therefore
reloads the window from the old budget; the later allocation reply updates only
the avatar cache. This explains the observed correct persistence and stale UI.

The allocation handler now sends `AvailableAllocationPoints` before
`AttributeInfo`, after the existing database write. No point arithmetic, stat
formula, persistence rule, or rejected-request behavior changes. The new order
lets the attribute callbacks read the committed remaining budget.

Two regressions invoke the handler against SQLite, decode its actual replies,
and reproduce the relevant original UI-cache behavior. They cover spending all
three points, spending only one point, persisted deltas, updated attributes,
and replay rejection after spending the entire budget. A copy of the old-order
handler is retained outside the repository for the parent's serial baseline
comparison. No agent builds or client launches were performed.

The old-order baseline failed both new cases in 4.5231 seconds. After a forced
clean rebuild, all **127/127 focused cases passed** in 8.2094 seconds, including
creation, allocation and weapon-action lifecycle checks. The first candidate
attempt retained stale old-order build output because copying source preserved
an older modification time; its 125/127 result is archived as a build-cache
artifact. Forcing `-t:Rebuild` resolved that issue without another source change.

All three logs and TRX results, plus the 2,112-file source snapshot, are archived
in the external `verification-attribute-final` directory with SHA-256 hashes.
The final full suite passed **1,302/1,302 tests**, with none skipped, in
5.7901 minutes. All 2,072 source files match the tested snapshot. The complete
full-suite log and TRX are archived alongside the focused results with hashes.

[The machine-readable evidence](evidence/client-attribute-allocation-reply.json)
records screenshot and state hashes, byte-verified original Python members,
exact consumer offsets, and the preserved baseline source.

## Original-client candidate verification

Frozen playthrough 15 verifies the fix using the original client. It restores
the complete, naturally earned level-two unspent checkpoint into a **separate
diagnostic server directory**; the continuing playthrough databases remain
unchanged. The input manifest identifies the checkpoint and its verified hash.
This is a controlled replay of allocation, not additional normal progression.

After spending one Body point, capture `05-partial-accepted.png` shows
13/12/12 and **two points remaining**; its state snapshot records spent values
1/0/0. After spending the remaining points on Body and Mind,
`08-final-points-accepted.png` shows 14/13/12 and **zero points remaining**,
matching persisted values 2/1/0. Normal character logout reaches selection in
capture 10, and the logout state retains 2/1/0. The screenshots were inspected
directly; their hashes and the frozen logs, inputs and state snapshots are in
the evidence manifest.

Capture 04 still shows the previous frame because of rendering delay and is
not used to establish the preview. The verified results are the accepted
states. This resolves the observed UI counter defect; it does not establish
all original stat formulas or claim a new allocation on the continuing
playthrough character.
