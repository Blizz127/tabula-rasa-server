# Player death and hospital recovery — implementation record, 2026-09-14

This closes the playable part of the death track described in
[death-research.md](death-research.md): a lethal hit now kills the player, the
client's Hospital Selection opens with the hospitals the character knows, and
choosing one revives the player there. Every data value and rule is labelled in
[`docs/evidence/hospital-catalog.json`](evidence/hospital-catalog.json), which
`HospitalCatalogTests`-style checks in `PlayerDeathLifecycleTests` compare with
`src/Rasa.Game/Data/HospitalCatalog.cs`.

## What the evidence settled

| Question | Answer | Tier and source |
| --- | --- | --- |
| Does the list come from the server and depend on the character? | Yes. Two final-week Wilderness deaths offered different lists as the player travelled (B3-043: Alia Das, Landing Zone CP, Twin Pillars; B3-058: Alia Das, Imperial Valley CP, Ranja Gorge, Twin Pillars). The boot-camp death offered only "Bootcamp > Refugee Base Medic" (A4-36). | observed |
| Which ids does `PlayerDead` carry? | graveyardlanguage keys: Refugee Base Medic 20000001, Alia Das 3, Ranja Gorge 5, Twin Pillars 6, Daghda's Urn 20, Imperial Valley CP 122 (the label the footage shows; 110 reads differently), Landing Zone CP 136 (136 and 183 render identically). | original / observed / inferred |
| Where does the player respawn? | At the hospital's client map marker. The boot-camp respawn was radar-measured at (358.94, 156.95) ±1.5 m, 1.1 m from the Refugee Base Medic marker (357.90, 156.52). Wilderness hospitals use their markers from `uimapmarker.factionedmarkers[1378]`. | original coordinates, inferred binding, calibrated |
| What is restored? | Full health: tutorial text 1582, "the medical techs will revive and restore you to full health"; A4-39 and B3-045 show a full health bar. Armor is restored with it (inferred); power and adrenaline are unchanged. | original (health) / inferred (armor) |
| How is a hospital gained? | `GraveyardGained(waypointId)` posts "You just gained %(graveyard)s." (manifestation.pyo). The Alia Das line was already in chat 98.9 m from the marker (B2-019), so the server gains hospitals within 100 m. No gain line appears for the boot camp's hospital in the transcribed A2–A4 chat, so it is known from the start. | original message / inferred radius |
| Teleport handshake | `Actor.BeginTeleport` queues the acknowledgement that `Recv_Teleport` sends, so `BeginTeleport` precedes `Teleport` (also corrected in `SelectWaypoint`). The default teleport type's arrival FX 8550 fits the blue shimmer after the A4 respawn. | original client code / inferred |

## Server behaviour

1. `MissileManager.DoDamageToPlayer`: health reaching zero sets control state
   `Dead` and the hit's `deathBlow`. Later shots, weapon actions, attacks,
   abilities and autofire already refuse dead actors and cancel on their next
   tick; movement from a dead client is ignored.
2. `MissileTrigger` sends the killing recovery, then
   `PlayerDeathManager.AnnounceDeath`: `ActorKilled` to the victim's observers,
   then owner-only `PlayerDead(sourceId, offered hospitals, canRevive = 0)`. The
   offer is kept on the manifestation.
3. Offered hospitals: the current map's catalogued hospitals that are known
   (gained, or known without discovery) and, for control points, AFS-held.
4. `ReviveMe(id)` must name an offered hospital; anything else is refused and the
   player stays dead with the offer intact. `ReviveMe(None)` takes the nearest
   offered hospital (the client's own timeout rule). A second request after
   revival does nothing.
5. Revival: `PreTeleport(DEFAULT)` to observers, `BeginTeleport`,
   `Teleport(hospital, current yaw, DEFAULT, 0)`, movement to observers, control
   state `Normal`, full health and armor, `Revived(0)`, `UpdateHealth`,
   `UpdateArmor`, and the position is saved.
6. `DiscoverHospitals` runs each second from the map worker for living in-game
   players; a gain is sent once and stored as a `character_teleporter` row of
   waypoint type 5 (Hospital), so no schema change was needed.
7. `BuryMe` (the Revive button) is refused: `canRevive` is always 0.

## Remaining gaps

Recorded in the evidence file's `gaps` list: trauma/rez sickness (constants and
D10.6 values are known, application rules are not), equipment wear per death,
revival by other players, death persistence across logout, control-point
ownership (control-point hospitals are always offered once gained), maps without
a catalogued hospital (revive in place; only contexts 1985 and 1220 are
catalogued), and respawn facing.

## Verification

`PlayerDeathLifecycleTests` (8 cases) covers the kill and notification order with
source-side and victim-side observers, the per-character Wilderness offer,
respawn position/resources/packet order and replay refusal, unoffered and `None`
requests, state preconditions, discovery radius and dead-player exclusion, the
boot camp's known hospital, and catalog/evidence equality. Full suite 801/801
under the .NET 5 SDK image. An original-client check of the death window,
respawn and "You just gained" line is still required.
