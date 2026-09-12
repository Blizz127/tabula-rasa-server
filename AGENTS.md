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
`docs/skill-research.md` and `docs/mission-research.md`, plus subsequent focused
research documents. Existing deployed patches remain subject to fidelity review.
