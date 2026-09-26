# S5 wounded soldier: source boundary (2026-09-23)

This record separates the original-client conversation binding from the estimated actor placement. The client artifact is the emulator's required 1.16.5.0 compatibility build; its exact relationship to the shutdown build is unverified. This record does not revise the deployed seed.

| Claim | Tier | Exact source and location | Limit |
| --- | --- | --- | --- |
| Mission 1995 objective 2 has a conversation on NPC package 2584, player flag 1, conversation type 1, text 21558. | original | `generated/client/objectiveconversation.pyo` decoded assignment at bytecode offset 41798; [client catalog](../evidence/bootcamp-client-catalog.json) `missions[1995].objective_dialogues` | Package ID identifies the dialogue binding, not a creature class, name, model or position. |
| Text 21558 starts “Ambushed. Can't...” and directs the player to get the bomb off Conrad's body. | original | `generated/client/language/english/missiontextlanguage.pyo` bytecode offset 565184, text ID 21558 | Establishes a speaking survivor and objective sequence, not their visual identity. |
| Original footage has a yellow 3D handset over the trench bunker doorway and a matching radar marker while objective 2 is active. | observed | `Ycxm8Pa1-v4` at 163.233–165.7 s; [footage events](bootcamp-d11-footage-events.json) C2-20, C2-50, C2-51 | No actor nameplate, usable prompt, conversation or objective completion is shown. The marker is not an actor coordinate. |
| The objective marker is approximately (-104.6, 86.1, 70.5), with ±1.5 m horizontal and vertical uncertainty. | measured | [reconstruction manifest](bootcamp-d11-reconstruction-manifest.json) `npc_mission_objective_indicator` (1995, 2, 0), `indicator.1995.2.435`; original footage above | Indicator ID 435 is itself inferred from client text “Last known location of scout party” (`missionobjectiveindicatorlanguage.pyo` offset 3872); the coordinate describes the marker only. |
| The other original footage cuts from objective 2 to the timed wreck objective. | observed | `8VXeKzGUv0c` last pre-cut frame 80.533 s, first post-cut frame 80.600 s; footage event B1-044 | The wounded-soldier and Conrad interactions are hidden by the edit; their exact behavior cannot be read from this video. |
| The original map contains a trench bunker around (-112, 86, 70), Forean corpses around (-97.57, 85.83, 70.44) and (-99.85, 85.85, 71.33), and sandbags around (-98.58, 84.91, 68.85) and (-102.27, 85.8, 68.64). | original | `adv_bootcamp-entities-joined.csv` indices 1165–1166, 1207–1208, 1262, 746, 18; [approach record](bootcamp-s5-soldier-approach-20260924.json) | These are scenery and corpse placements, not the wounded actor's server placement. |
| The current actor placement (-102.4, 86.09, 70) uses a nearby walkable doorway point; class 3846 and `name_id=0` are reconstruction choices. | inferred / analogue | [reconstruction manifest](bootcamp-d11-reconstruction-manifest.json) `content_placement` 198675 and `creature` 198509 | No original server coordinate, actor name, class, appearance, posture, health, or exact interaction range is recovered. The client text table cannot prove the NPC was unnamed. |

The original-client replay in [the approach record](bootcamp-s5-soldier-approach-20260924.json) displayed a Human target and talk prompt near the seeded coordinate. A later checkpointed continuation opened the objective-completion dialogue, and Continue completed objective 2 and revealed objective 3 in the copied character database. That replay used a deployed-derived world copy. It verifies the reconstructed interaction, but is not independent evidence for the final-live NPC placement or identity.

## Verified source hashes

| Local source | SHA-256 |
| --- | --- |
| `/home/blizz/backups/rasa-net/research/20260913-bootcamp/list-tables/decoded/generated_client_objectiveconversation.pyo.json` | `6c05f83cd054255c48f5302144d07c454e33776a43f72a9b4c938b887a76f5e1` |
| `/home/blizz/backups/rasa-net/research/20260913-bootcamp/list-tables/decoded/generated_client_language_english_missiontextlanguage.pyo.json.gz` | `3834be6271a085b1440a3bc08dc60020bcc5341aa85f95080780788a2bb5f7aa` |
| `/home/blizz/backups/rasa-net/research/20260913-bootcamp/list-tables/decoded/generated_client_language_english_missionobjectiveindicatorlanguage.pyo.json` | `3a3fb7cd095a2cdea36695c22d62dad20c340695347211b2f0c5580ab2b33c2f` |
| `/home/blizz/backups/rasa-net/research/20260913-bootcamp/map/decoded/adv_bootcamp-entities-joined.csv` | `062f293ea7c64df55c922885eb98c741b726139493dfb014815518237baea9a5` |
| `/home/blizz/backups/rasa-net/research/20260913-bootcamp/footage/videos/Ycxm8Pa1-v4/Tabula Rasa Tutorial gameplay.mp4` | `0176e9184c5b8bc71533712ca7da0e08044381e10bb90a35948f80b24cc0b62d` |
| `/home/blizz/backups/rasa-net/research/20260913-bootcamp/footage/videos/8VXeKzGUv0c/Tabula Rasa Advancing 2of8.mp4` | `43187d064abf0282f150513a83f5abd07023a3b9db5baab01a6431f1536f63e0` |

The package 2584 binding is high confidence. The marker position is measured with stated uncertainty. The actor position and identity remain low confidence; the reconstructed interaction works in a checkpointed original-client run, while its final-live behavior and a continuous new-character route remain unverified.
