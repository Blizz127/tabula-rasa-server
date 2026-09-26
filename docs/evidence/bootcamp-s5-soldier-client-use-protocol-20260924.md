# S5 wounded-soldier client use protocol — 2026-09-24

This is a static audit of the original 1.16.5.0 compatibility client's `trpython.zip` bytecode. It describes the client input path; it does not establish the final-live soldier placement or prove an uninterrupted mission route.

| Original client source | Recovered behavior | Test consequence |
| --- | --- | --- |
| `client/augmentations/npc.pyo`, `_GetUseAction` lines 60–66, bytecode offsets 0–42 | The NPC exposes the `CONVERSE` use action only while `convoStatus != CONVO_STATUS_NONE`; `OnConverse` lines 69–72 calls `player.Converse(self.entityId)`. | A yellow objective-location indicator alone does not prove that the NPC is the selected usable target. Require the client's **Human** target and “Press [Mouse Button 2] to talk” prompt before clicking. |
| `client/inputhandlers.pyo`, `Use` lines 238–265, bytecode offsets 0–225 | In FPS mode `PerformUseAction` runs on right-button press. In MMO mode it runs on release only if the button was down and mouse movement did not turn it into a use drag. | Use a short ordinary right click while the prompt is visible; do not move the mouse between press and release. |
| `client/augmentations/actor.pyo`, `IsInConversationRange` lines 878–884, bytecode offsets 0–45; `shared/gameconstants.pyo` recovered value in `bootcamp-client-catalog.json` `MAX_CONVERSATION_RANGE` | The client tests `body.InRadiusOf(actor.body, MAX_CONVERSATION_RANGE)`, where the recovered range is 5 m. `Converse` lines 1872–1876 creates and performs a converse action for an existing NPC entity. | The earlier ordinary-client stop at `(-95.578,85.668,71.699)` is about 7 m from the **inferred** soldier seed `(-102.4,86.09,70)` and can fail the client range gate. The measured north-side bypass to `(-104.520,86.765,72.079)` is about 3 m from that inferred seed and is a better interaction checkpoint. |
| `client/missionlog.pyo`, `CompleteNPCObjective` lines 400–401, bytecode offsets 0–31 | Continue sends `CompleteNPCObjective(npcId, missionId, objectiveId, playerFlagId)`. | A right click that merely opens dialogue has not completed objective 2. Require the Objective Completion window, click its visible Continue control, and verify `CompleteNPCObjective` plus persisted objective 2 status 2 and objective 3 status 1. |

The server's current `NpcManager.RequestNpcConverse` resolves the requested NPC entity in the same map channel, and `MissionManager.AddMissionConversation` adds objective topics only when the NPC package matches the active objective's conversation row. Its completion handler also checks active mission state, incomplete objective, package/flag binding, and conversation range. These are current emulator checks, not evidence of final-live server internals (`src/Rasa.Game/Managers/NpcManager.cs` lines 98–128 and `MissionManager.cs` lines 775–800, 1542–1593).

## Source identity

All four original bytecode members came from the restored `Tabula Rasa 1.16.5.0/trpython.zip`:

| Member | SHA-256 |
| --- | --- |
| `client/augmentations/npc.pyo` | `d54178e21d838417110afd39b85a40c08b906082965864aed480233e745f3894` |
| `client/inputhandlers.pyo` | `586fc0ea303e60dcc7991bf79d6a787b560e186bd3c0b4d86d4cc42993e5c9be` |
| `client/augmentations/actor.pyo` | `2ecb129d5071ac588e076415857ea4e5b3ebae08a964e2828b885089ac316c4f` |
| `client/missionlog.pyo` | `5e43f82a7bab34c15971a0b74dca9585d5b878abcb332e4eeef1540760aeb375` |

See [soldier and corpse interaction boundary](bootcamp-s5-soldier-corpse-interaction-boundary-20260924.md) for the original footage and placement limits.
