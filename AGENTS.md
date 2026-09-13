# Preservation target

The user's explicit requirement is a **1:1 preservation of Tabula Rasa as it
existed immediately before the original live service shut down**. This applies
to all continuing work in this repository. It supersedes a generic "retail-like"
or best-effort emulator target.

- Preserve the final live content, mechanics, progression, economy, classes,
  combat, missions, NPC behavior, travel, social systems, presentation, and
  documented final events. Do not introduce custom content, balance, rates,
  convenience features, or invented substitutes for missing original systems.
- `docs/setup.md` requires client 1.16.5.0. Treat this as the emulator's current
  compatibility requirement until an original final build/manifest establishes
  the shutdown client's exact revision. Do not equate those claims automatically.
- Prefer original versioned client data, authentic captures, official final live
  patch notes, and contemporary direct gameplay evidence. Older emulator code
  and secondary references are supporting evidence, not proof of final retail
  behavior. Distinguish live patches from test-server patches and obsolete rules.
- For each reconstructed rule or dataset, record provenance, version/date,
  exact source location, confidence, and what remains unverified. Reconcile
  conflicting sources before treating a behavior as accurate.
- Fix emulator defects that obstruct faithful behavior. Preserve documented
  original gameplay quirks; do not "improve" them into a different game.
  Unsupported values or mechanics must remain explicit evidence gaps rather
  than silently becoming guessed gameplay. Continue other evidence-backed work.
- Audit already-implemented behavior against the same standard. A working
  feature, agreement between emulators, or passing automated tests does not by
  itself demonstrate 1:1 preservation. Tests should verify the implementation;
  fidelity needs comparison with the original artifacts/behavior.
- Keep the full preservation goal active while any original system, content,
  final-state requirement, or necessary verification remains incomplete.

Current progress and limitations: `docs/retail-accuracy.md`. Research records:
`docs/final-retail-target.md`, `docs/skill-research.md`, `docs/mission-research.md`,
and `docs/death-research.md`, plus subsequent focused research documents.
Existing deployed patches remain subject to fidelity review.

# Evidence-bounded reconstruction (user decision, 2026-09-13)

Some final-live server data is lost for good: no public capture, client table
or map file records it (for example the rebuilt boot camp's spawn positions,
NPC placements and reward amounts). For such content the user chose on
2026-09-13 to **build it from the strongest surviving evidence — original
gameplay footage, the original map and client data — with every estimated
value labelled**, instead of leaving the content unplayable.

- Every reconstructed row and field records its provenance tier:
  `original` (client/map data or official notes), `observed` (read directly
  from original footage or screenshots), `measured` (derived from observation
  with a stated uncertainty, e.g. a position from radar/landmark matching),
  `inferred` (logical reconstruction from text or sequence), or `analogue`
  (no direct evidence; the closest original counterpart, used only when a
  value is required for the content to function).
- Each label cites its exact source (video id and timestamp, file and offset,
  document and capture) and, for `measured`, its uncertainty. Labels live in a
  machine-readable manifest in `docs/evidence/`, and the seeded data must be
  verifiable against that manifest.
- Prefer leaving an optional value out over an `analogue`. Never present an
  estimate as original, and never use pre-rebuild (obsolete) content as an
  analogue for rebuilt content.
- Replace estimates when better evidence appears, and record the change.

# Implementation order

The user explicitly requested progression from **new character to endgame in
that order** on 2026-09-12. Follow `docs/progression-preservation-plan.md`.
Start with creation, initial gear/skills and the final live boot camp, then
advance through early leveling, class tiers and their content to level 50,
endgame and final live events. Complete and verify each playable progression
segment before prioritizing later content. Implement shared systems when the
current segment needs them. Use original-client reverse engineering,
contemporary websites and gameplay videos, and other emulator projects as
evidence, with their respective provenance and limitations recorded.
