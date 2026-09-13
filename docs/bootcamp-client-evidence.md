# Rebuilt boot camp: recovered client data

Research date: 2026-09-12. Target: the final live game under `AGENTS.md`.
This is the next content dependency for the ordered new-character-to-endgame
work. The original client supplies the records below, but the boot-camp server
scripts have not been recovered. These findings are not a playable replacement.

## Version boundary

Deployment 11 rebuilt boot camp in August 2008. The archived August 29 Bootcamp
wiki revision explicitly marks its earlier walkthrough as obsolete. Later
client records describe beginning with an Eloh vision, moving into a refugee
cave, obtaining gear and practicing combat, reclaiming the AFS base, and
escaping for reinforcements. Their identities and links differ from the earlier
Basic Training 101 / Elvers / Vance walkthrough.

The dated sources, archive hashes and revision-cutoff policy are in
[new-character initialization](new-character-client-evidence.md). The
[September 1, 2008 interview with lead QA](https://www.mmorpg.com/interviews/pax-a-look-at-tr-2000115981)
independently confirms the deployed front-end/tutorial rewrite.

The still-accessible [fansite walkthrough](https://www.playtabularasaonline.com/index.php?pg=starters-guide_bootcamp1)
starts with Commander Elvers; its [bypass guide](https://www.playtabularasaonline.com/index.php?pg=starters-guide_bootcamp_skip)
uses Captain Burba inside that instance. These identify the older tutorial,
despite the site's home page carrying a March 2009 shutdown update. Page-level
content, rather than the home-page date, determines applicability. The three
entry/walkthrough/bypass pages and acquisition hashes are preserved privately
in `playtabularasaonline-acquisition.json`; their rewards and bypass path are
not adopted for the rewritten tutorial.

An [August 28, 2008 report](https://www.tentonhammer.com/articles/screenshot-of-the-week)
describes the rebuilt cave and an Eloh hologram encountered after training,
whereas mission 1990's later client text presents an introductory vision.
This does not establish whether these are separate encounters or a subsequent
sequence change. Its linked official screenshot could not be retrieved, and
the archive-index request timed out. No screenshot placement or mission-order
claim is inferred from unseen footage; exact transitions remain unresolved.

## Exact mission and objective identities

The [machine-readable catalog](evidence/bootcamp-client-catalog.json) preserves
every mission-text, objective-text and objective-conversation binding for these
four missions, including original bytecode store offsets and source SHA-256.
It deliberately marks the server definitions incomplete. Field meanings and
original consumers are established in the
[mission-table audit](river-recon-client-evidence.md#table-meanings-verified-in-the-original-client).

| Mission ID | Original name | Name-text ID | Objective IDs | Content established by text |
| ---: | --- | ---: | --- | --- |
| 1990 | Initiation | 21132 | 1, 2 | Approach the Eloh hologram; the closing speaker takes the recruit toward the temporary camp. |
| 1992 | Gearing Up for Battle | 21164 | 1–9 | Obtain gear from a crate, equip it, talk with Delessio/Hartmann, practice shooting and Lightning, then report to DeSimone. |
| 1994 | Capture the Flag | 21235 | 1–4 | Receive the first promotion, leave the cave, defeat Tizzik G and report to Youngblood. |
| 1995 | Calling for Reinforcements | 21240 | 1–4 | Find the ambushed soldiers, take Conrad's bomb, destroy the crashed dropship before its timer expires, and arrange evacuation to Alia Das. |

IDs are not progression ordinals. Mission 1992's shooting objective is 3, while
the preceding Delessio/Hartmann conversations use 4, 5 and 6. Mission 1994's
promotion is objective 4 and leaving the cave is 2, preceding boss objective 1.
Mission 1995 uses objectives 2/3 before the bomb objective 1. The narrative
supports those dependencies, but does not supply complete server transitions.

The nine objective-conversation rows bind these packages:

| Mission/objective | NPC package | Player flag | Type | Text ID | Speaker identified by cross-reference |
| --- | ---: | ---: | ---: | ---: | --- |
| 1992 / 4 | 2560 | 1 | 1 | 21484 | Captain Delessio, gear-crate instruction |
| 1992 / 5 | 2560 | 1 | 1 | 21487 | Captain Delessio, onward to firing range |
| 1992 / 6 | 2563 | 1 | 1 | 21491 | Corporal Hartmann, shooting instruction |
| 1992 / 7 | 2563 | 1 | 1 | 21494 | Corporal Hartmann, onward to DeSimone |
| 1992 / 9 | 2563 | 1 | 1 | 21665 | Corporal Hartmann, Lightning instruction |
| 1994 / 3 | 2561 | 1 | 1 | 21617 | Captain Youngblood, after reclaiming base |
| 1994 / 4 | 2562 | 1 | 1 | 21694 | Corporal DeSimone, first promotion |
| 1995 / 2 | 2584 | 1 | 1 | 21558 | Wounded member of the missing team; name not established |
| 1995 / 4 | 2564 | 1 | 1 | 21864 | Corporal Van Valkenberg, transport instruction |

Type 1 is the original completion-conversation selector. These package IDs are
not creature IDs or creature-name IDs. A text binding does not independently
establish its activation trigger, acceptance rules or the resulting server
state. Mission 1994's log text says Corporal Delessio while its objective and
conversation identify DeSimone; preserve the observed inconsistency in original
text rather than silently rewriting it into a guessed NPC assignment.

None of these four missions has a reward-text-type-5 binding. That does **not**
mean they have no rewards: rewards are supplied separately by the server.
The promotion text gives no exact XP/attribute/skill-point quantities. Gear
crate text does not identify every granted template, quantity, placement or
instance property. The existence of boot-camp pistol template 122875/class
29803 is a useful lead, not sufficient proof of the crate's entire contents.
Lightning training and its Power Logos requirement remain separate; the
complete original Logos acquisition event has not yet been reconstructed.

## Original map and appearance data

The recovered package contains
`data/maps/adv_bootcamp/adv_bootcamp.map`, 127,212 bytes, CRC32 `671e7453`,
SHA-256 `2876982ba3473dcb57d5cc4a1b88d75710349a71948d85fafb499b2a8f5106f3`.
It was acquired with an anonymous HTTP range request and checked against the
preserved ZIP directory. The file's offset-4 integer is 783, matching the
current database's map version. The rest of its binary layout is not yet
established; arbitrary floats must not be treated as player/NPC spawn points.

The original `maptemplate.lookup` maps **1985 → adv_bootcamp** at store offset
3872 and also retains an older **1772 → bootcamp** entry at 2459. The existence
of the older entry is not proof that it remained the new-player map.
`gamecontextuimapinfo.lookup[1985]` is `(1024, 103, -58)` at offset 344. These
are UI-map data, not character spawn coordinates.

`uimapmarker` contains a refugee hospital at
`(357.9005432128906, 120.32544708251953, 156.51882934570312)`, dropship marker
`(-233.74066162109375, 101.03974914550781, -80.08139038085938)` and five region/
point labels for context 1985. A hospital/marker position must not be substituted
for the unrecovered first-login position.

The original starter catalog includes Recruit outfit templates
122854/122855/122856, classes 10000068/10000069/10000070 for boots/legs/vest.
The current default appearance uses those classes. The catalog supports the
creation preview; it does not prove item ownership/placement on first login.
All 95 starter template-to-class mappings agree with the current world DB.

## Current server gap and next implementation boundary

### Additional placement sources checked

The same versioned package's `data/maps/adv_bootcamp/audioemitters.xml` was
range-extracted with ZIP size/CRC verification: 8,399 bytes, CRC `512198e5`,
SHA-256 `d8c8d097d7319ae90a4464a943b71e9d6ac6cb66874cd3f2c264b86dea333bcf`.
It contains eight audio emitters referencing sound sets 30, 487, 1160 and 1162.
The recovered `audiosetdata.pyo` identifies these as alien bird chirps, cave
water drips, burning heavy machinery and generic fire. These are audio
placements; they do not establish NPC or player spawns.
Acquisition metadata and decoded positions are retained as
`bootcamp-audio-extraction.json` and `bootcamp-audio-emitters.json` in the
research directory below.

The [audio reference catalog](evidence/bootcamp-audio-catalog.json) also preserves
voice sets 2773–2776, named for missions 1990/1992/1994/1995, and set 2777 named
for Major McAllister. Their sound records reference `boot_camp_Eloh_initiation_1.ogg`,
`boot_camp_gearing_up.ogg`, `boot_camp_capture_the_flag.ogg`,
`boot_camp_long_way_home.ogg` and `boot_camp_major_mcallister_bark.ogg`.
These are original table names and bindings, not a reconstruction of which
server event plays each file. The catalog records exact store offsets and the
source member hash. Audio playback was not acquired or heard during this audit.

The archived Google Code emulator
[ltsochev-dev/tabula-rasa-server-emulator](https://github.com/ltsochev-dev/tabula-rasa-server-emulator)
was inspected at commit `50c7b4ebce644d3b7ab7b9012b3fe5e975a0409b`.
Its `TRE/TRE.GameService/gameData/mapInfo.txt` identifies context 1985 as
`adv_bootcamp` but declares version 792, differing from the recovered map's 783.
`TRE/TRE.GameService/GameMain/MapInstance/Mission/Mission.cs` has empty mission
initialization/acceptance methods and placeholder objectives. It supplies no
working tutorial definition. Neither that map version nor those mission stubs
are adopted as final-live behavior; no acquired emulator code was executed.

### Remaining implementation

The original selection UI also constrains the skip prompt. In
`client/ui/characterselectionwindow.pyo`, `OnPlayBtn` source line 261,
offsets 39–156, it prompts only when the selected pod has zero logins, no last
game context (`None`), and the account can skip boot camp. Otherwise it requests
play with `False`. `_SkipBootcampYes` (line 472) and `_SkipBootcampNo` (line 479)
forward `True` and `False` respectively. `client/clientmethod.pyo`,
`Recv_BeginCharacterSelection` line 803, offsets 77–95, forwards the server's
fifth argument to `SetCanSkipBootcamp`. Static disassembly and original member
hashes are retained in `bootcamp-skip-code-manifest.json` and `*.skip.dis`.

`client/inputstate/characterselection.pyo`, `OnSelectCharacter` source line 80,
offsets 113–131 (source line 99), sends `RequestSwitchToCharacterInSlot` with
exactly `(slotNum, bSkipBootcamp)`. The server decoder now checks that tuple
length and rejects slot integers that cannot fit a byte, preventing values
such as 257 from wrapping to slot 1. The handler resolves the slot against
persisted account ownership before changing state, and publishes the session's
selected slot only after the selected-slot/login save succeeds. Empty or
unowned slots leave the session and saved login state intact. These are emulator
entry-failure fixes, not recovered original error-response behavior; they do
not implement the remaining tutorial skip flow.

The server already sends its persisted account flag, whose schema default is
false. However, `CharacterInfoPacket` publishes the assigned Wilderness context
even for an unplayed character, so the original UI's no-last-context condition
is not met. The selection handler also ignores the returned skip choice.
First-login context, account completion and skip rewards must be reconciled
together; exposing a prompt alone would still lead to the same incomplete entry
path. No completion flag or skip entitlement is fabricated from a character's
mere existence.

The inspected world DB has context 1985 but **zero boot-camp spawn-pool rows**
and no definitions for missions 1990, 1992, 1994 or 1995. Its only mission rows
are 321 and 429. `CharacterRepository.Create` still assigns Wilderness context
1220 and emulator coordinates; the selection handler does not implement the
received boot-camp skip choice. This is a known fidelity gap, not a verified
retail bypass. Moving new players into map 1985 alone would not supply a tutorial.

Next required evidence/implementation is the first-login instance and spawn,
the introductory vision and completion/skip state, NPC and interactive-object
placements, initial crate loadout, original mission prerequisites/rewards and
events, combat objective hooks, bomb timing/retry, and exit/skip rewards.
These must be implemented as a connected progression segment with persistent
state and reconnect/failure verification before prioritizing later leveling.

Public video searches found a D11 mini-game clip and a tutorial clip, but
YouTube playback retrieval returned a sign-in/bot challenge. No downloaded
playback or unobserved frame is claimed as evidence. The old walkthrough and
earlier-beta videos cannot resolve final tutorial rules by themselves.

Original bytes, decoded tables, source manifests, map acquisition metadata,
wiki revisions and reproducible join scripts are retained outside Git in
`/home/blizz/backups/rasa-net/research/20260912-new-character/`.
The package authenticity limitations in [client artifacts](client-artifacts.md)
still apply. All game bytecode was read statically; no acquired module was run.

## 2026-09-13 verified sweep: what the client and the map do and do not establish

A six-track sweep (dated wiki history, original generated tables, original
client code, contemporary web captures, the map file, and a server audit) was
run with an independent verification pass per track. No claim was refuted;
33 were narrowed and are recorded here only in their narrowed form. Raw
outputs, scripts, capture bodies with SHA-256 and the consolidated synthesis
are retained outside Git in
`/home/blizz/backups/rasa-net/research/20260913-bootcamp/`.

### The static map carries no gameplay actors

`adv_bootcamp.map` is format `0x0002002d` (2.45) and parses with the original
`client/gamemap.pyo` loader grammar to exactly 127,212 bytes: 1,435 static
entities, 24 collision volumes, day length 10,800 s with sky start 0, one
camera script (id 1, three keyframes) and seven region settings. All 377
placed classes have empty augmentation lists in both the original
`entityclass` table and the world DB, and the format has no player-start,
spawn, NPC, trigger or region-volume section. The Concordia Divide map
decodes 16 augmented statics with the same parser, so this is not a parser
gap: every boot-camp NPC, crate, dummy, bomb and dropship was server-spawned.
Region settings reference `sky_foreas_bootcamp_d11.sky`, the only explicit
D11 marker recovered from data. The single `ArchElohHologramLarge` stands at
(389, 128.43, 65) on a hologram platform, reached by a ceremonial bridge at
x 337 and by a southern colonnade with two obelisk pairs; the AFS refugee
base props sit at x 356–418, z 95–190 around the client hospital marker; the
damaged Drop Zone landing pad is at (−225.35, 99.60, −70.52) beside the
client dropship marker. These are prop positions, not spawn or objective
positions. Decoded records, format specification and validation against
independent Divide placements are in `map/` of the research directory.

### Mission set, constants and speech

- A fifth definition belongs to the segment: mission **2005**, also named
  "Calling for Reinforcements", with only objectives 1 and 4 and the same
  Van Valkenberg package 2564. Its opening text (21566) offers "another
  bomb" with "ten minutes" after a failed attempt. It is now in
  [the catalog](evidence/bootcamp-client-catalog.json). How it is offered is
  not established; "ten minutes" is dialogue, not a recovered timer value.
- None of the five missions binds reward text type 5; every reminder text is
  the literal `none`; every objective conversation is type 1 with player
  flag 1. Missing reward text is not evidence of missing rewards.
- `shared/gameconstants.pyo` hard-codes `MAX_MISSION_COUNT = 30`,
  `NON_ABANDONABLE_MISSIONS = [1990, 2010, 2011]` and
  `MAX_CONVERSATION_RANGE = 5`; the client disables Abandon for 1990 and
  refuses to open a conversation beyond five metres. 2010/2011 are the
  Alia Das "Getting It In Gear" class-gear missions (package 133).
- Objective indicator names 430–439 (Eloh Approach ×2, Dropship Debris,
  Equipment Crate, Firing Range, Last known location of scout party,
  Conrad's corpse, Base Center, Dropship Pad to Exit, Cave-in Location) are
  in the catalog. The client stores names only; positions and the mapping
  of ids to missions are server data that was not recovered.
- Creature name ids 10574–10579, 10566, 10598, 10599 and 6730 "Tizzik Gi"
  are recorded with offsets. No table binds them to classes, packages or
  positions; the package-to-NPC identities remain inferences from text.
- Greetings 1634–1636 are Eloh speeches; the client posts the
  `tutlightning_*` UI effect only for greeting 1634. Bark 852 is McAllister's
  line with `boot_camp_major_mcallister_bark.ogg`. Audio sets 2772 (drill
  instructor), 2778 (shooting-range hits) and 2782 (jumping grunts) exist
  alongside 2773–2776; naming patterns make 2773–2776 the likely
  mission-offer voice-overs, which is not proof.
- "An Ancient Eloh" is creature name 10598, not a mission title. What
  produced the dialog in the bridge note is not established: an NPC with
  that name, or a dialog without an entity (`Recv_ForceConverse` titles a
  greeting from `creaturenamelanguage`, and `DispenseRadioMission` can force
  a mission dialog). Its link to the mission 1990 hologram and the trigger
  type are inferences. The text `evacuat` occurs in no mission; the client
  says "transport out to Alia Das".

### Entry, skip and protocol facts used by the implementation

- `PreWonkavate` (134) takes one argument; `Wonkavate` (242) carries
  `(gameContextId, instanceId, templateVersion, startPosition, startRotation)`.
  The version should equal 783: on a mismatch the client raises
  `VersionMismatchError`, which `wonkavator` turns into a load-anyway/quit
  dialog. By default the client sends `MapLoaded` (107) without a button
  press; with the user option `Client.UserInterface.LoadingScreen.ShowButton`
  it waits for the Enter Battle button. The client never selects context 1985 itself, and it
  suppresses only its own local `INSTANCE_ENTERED` tip for 1985.
- The intro movie is a client option, not a server trigger.
- The selection prompt condition and message shape are unchanged from the
  earlier audit; the official D11 rule is account-wide bypass after one
  character "has completed Boot Camp". No source defines the completion
  event, the destination beyond "Wilderness", or any skip grant.
- Mission-log handlers live on the player's manifestation entity.
  `NPCInfo` (490) sets `npc.npcPackageId`, which
  `conversationwindow.HandleShowObjectiveCompletion` (original source line 720)
  passes to `BuildObjectiveConversationText`; without it no objective
  dialogue can render. `Recv_PlayerFlags` (manifestation source line 1661)
  stores its argument and `HasPlayerFlag` (line 1665) tests `in`, so the
  value must be a sequence. `Converse` key 6 must hold
  `(missionId, objectiveId, playerFlagId)` triples; the Continue button
  sends `CompleteNPCObjective` (431) and the client changes nothing locally.
  `CompleteNPCMission` (430) carries `selectionIdx` as `None` or an integer.
  Module hashes: `client/augmentations/npc.pyo`
  `d54178e21d838417110afd39b85a40c08b906082965864aed480233e745f3894`,
  `client/augmentations/manifestation.pyo`
  `724f68eb70e5a47338db82977c55abfa09f1b187f86a0b9a560c012a8588886d`,
  `client/missionlog.pyo`
  `5e43f82a7bab34c15971a0b74dca9585d5b878abcb332e4eeef1540760aeb375`,
  `client/ui/conversationwindow.pyo`
  `6e3f6ce004afef767fcbd25cacda453afaff70c6d8d0b5dd7d3fbc0ee3c5c9af`.
- Scriptable client events carry only an event id, so they cannot express
  "use Lightning on the Target Dummy". The `DISPLAY_TIMER` HUD path is
  unproven in 1.16.5.0 (its handlers are unregistered); the per-objective
  `timeRemaining` field is the demonstrably wired countdown.

### Official and contemporary sources added

- Official Deployment 11.6 live notes ("Deployment 11 is on Live!",
  8/15/2008), Wayback capture 2008-08-28 of
  `rgtr.com/news/patch_notes/deployment_116_8152008.html`, SHA-256
  `e36041158a3d4603eb7f87d9855cb49ba96137c17d9c1ce5b2d0dbb69347cb59`
  (2008-12-20 capture `3dfe1626d2e031c2eb88e38459caf0bfa9e6c49e34c698fbb179d557271dd7d8`):
  recruit outfits with the clothing choice removed, abilities preloaded into
  trays, account-wide bypass, pulsing mission objects. The clothing note
  removes the style choice only; the final client still ships colour
  selection.
- Deployment 11.4 **public test server** notes (8/08/2008), capture
  2008-11-14 of `rgtr.com/news/patch_notes_public_test/deployment_114_8082008.html`,
  SHA-256 `fa41f9ae85cfe6da71d5a4d21efbe23ec7e5468f696ffe8e335bdef1cf788413`:
  the bomb-plant logout bug, the five-seconds-left plant and the bridge
  re-trigger of "An Ancient Eloh". The same three fixes are listed in the
  D11.6 live notes above, so they reached live. A clean cancellation is as
  consistent with the logout fix as a resumable plant. (An earlier draft
  cited the D11.7 notes, which contain none of these items.)
- D13.4 live notes, capture 2008-10-17 of `deployment_134_10152008.html`,
  SHA-256 `fd4d3928792bc2aa2d584bb878a6c882e971ad231933948017fffa8af720caf5`,
  known issue: "It is possible to become stuck in Bootcamp by abandoning the
  final mission after clearing the dropship pad. Workaround - get the mission
  again and fail it normally (let the bomb blow up), this will cause the ship
  to respawn." Detonation is the workaround that respawns the ship, not the
  cause. The notes also record the "Getting it in Gear" / Wilderness Targets
  of Opportunity interaction. The official patch-note indexes from D13.6 to
  D16.5 (see the follow-up acquisitions below) mention no boot-camp change
  and no fix for this issue; silence in notes is absence of evidence only.
- Susan Kath's D11 developer journal republished by Hexus, Wayback
  captures 2008-08-17/19 (page 2 SHA-256
  `c897a91c85300058297a7a7640cd59de67041c26ec9f4b40eae8e9b08e4b187a`, page 3
  `d94060055cfe16f681ad6d7e75057322aa1177fba6450d338d1128e71a982ab6`):
  wormhole arrival into a glowing cavern, the alien speaking while the
  recruit crosses a long stone bridge, a refugee camp with calisthenics and
  target ranges behind a barricaded entrance, the destroyed outpost outside,
  a remote dropship pad, control points no longer taught, and class gear at
  level 5. This is design prose from August 2008, not final-live
  observation.
- Pre-shutdown recordings of the rebuilt tutorial are described under the
  follow-up acquisitions below.

### Follow-up acquisitions (verified 2026-09-13)

Five further tracks were run and independently verified; 59 claims were
confirmed, 34 narrowed and none refuted. Outputs are in the research
directory's `footage/`, `pre-d11-map/`, `patch-notes/`, `list-tables/` and
`community/` folders.

**Pre-shutdown footage (thumbnails only).** Anonymous YouTube playback and
storyboards were refused (sign-in challenge), and no archived copy exists, so
only YouTube's automatic thumbnails were available: at most four frames per
video, at unverified positions in recordings that are evidently edited. No
video was decoded, and frame order must not be read as duration. Transcription
`footage/bootcamp-footage-transcription.md` (SHA-256
`11a6a1b2240a8b30f6162c8eef9115aad4bc40e7beb2c93bd3d66299aa60e6c2`) and frame
index `footage/derived/frame-index.json`
(`e1b9f6e26ab6c172860c221203bdcc9563fe042791c4849c7d05b18cf83a6ec4`). The
frames below were also inspected directly for this record.

- MMORPGManager "Creating Character 1/8" (`7Lrst9SG3pk`) and "Advancing 2/8"
  (`8VXeKzGUv0c`), uploaded 2009-03-02 in a series ending "Server Shut Down
  8/8", so recorded before shutdown; the deployment is not readable. A
  level-1 recruit in the white creation clothes stands on a stone causeway in
  Luna Cavern; the tracker shows "Initiation / Approach the Eloh Hologram",
  chat shows "Mission Accepted: Initiation", and the "Radial Menu" tutorial
  (id 10000018, `TUTCTRL`) is open. No client code posts that tutorial, so the
  server sent `DisplayPlayerTutorialNotification`. A weapon labelled "Pistol"
  is in the tray. The frame is not the login moment and does not show what
  granted mission 1990 or where the pistol came from. Later frames show level
  2 on 1994 objective 2 "Find a way out of the cave" in Luna Cavern with a
  Shinobi Rifle, and level 3 on 1995 objective 2 in Denzil's Caldera with an
  AccuMax Shotgun.
- Zorlac's "Tabula Rasa Tutorial gameplay" (`Ycxm8Pa1-v4`, uploaded
  2009-01-18, before D16.5): level 3 on 1995 objective 2 "Locate the missing
  AFS soldiers" in Denzil's Caldera against a level-2 Thrax Infantry Initiate.
  Chat shows boot-camp kills awarding experience ("You gained 71 experience
  points", crit-kill and +100% kill-streak lines), 5 or 10 credits, a prestige
  point for the kill streak, and item drops such as a Thrax Skull and a
  Class I Concussion Grenade schematic. Displayed values include modifiers and
  must not be fitted into creature reward tables; they show only that
  boot-camp kills paid out.
- Across both players the tracker lists a single mission and objective, and
  no countdown appears during 1995 objective 2. No frame shows a named
  friendly NPC, the crate, a Logos message, Tizzik Gi, the bomb, a mission
  reward, the promotion, the exit or the Alia Das arrival. General chat is
  active, which does not settle instancing.
- A pre-live D11 test-server clip (`EiE2oodlP8A`) shows NPC recruits doing
  jumping jacks and push-ups before a hatted instructor, consistent with
  audio sets 2772/2782 and a JeuxOnLine player post on D11 day. The earlier
  statement that a creation-time weapon would contradict the gearing mission
  is withdrawn: the footage neither confirms nor rules out a starting pistol.

**Earlier maps.** Two pre-D11 clients were reached by HTTP range without
downloading their images: the 2007-11 Russian DVD (client 1.2.2.0,
`adv_bootcamp.map` template 631, SHA-256
`c19d6cb08e87443b1e87c90ebcbb4c4a9330b5ce199ccfc0a36b56b9fdacdaa7`) and the
2007-08 retail master (template 598,
`53a9264e1cfeafb51212cd700d92b5a3b9f6a40bd6ecd9b595fd8e56a15be625`); both MD5
values match their installer descriptors. No 2008 build is public, so the
2007-11 → 1.16.5.0 difference bounds everything changed from D1 through
D16.5, not D11 alone. It removed 354 placements, added 573 (all with editor
ids above the 2007 maximum) and moved 323. The west field camp and its
"Hospital: Field Camp" marker were replaced by the damaged landing pad, the
dropship-transport marker and 28 newly placed Forean ruin pieces; the outpost
became destroyed variants with corpses and fires; the 185-entity refugee base
is new; a Logos dispenser base beside the existing hologram platform and long
ceremonial bridge was replaced by the hologram, and the southern colonnade and
its obelisks were moved and built. The default region became Luna Cavern.
Kath's deleted Collector/Dissector and bridge ambush were server spawns and
cannot be tested. Camera script 1 already existed in 2007 but its keyframes
were edited later and end facing the new pad, so it is neither proven dead nor
identified as an arrival cinematic. Entity class ids of 20000000 and above
are original client data (present in 2007). Diff:
`pre-d11-map/pass2/diff/diff2.json`
(`8760cb05dc2c46be4e0135e6197c825fc9328bb152552222c8c9d023586647f9`).

**Patch notes D13.6–D16.5.** All 43 documents listed by the captured US live,
US public-test and EU patch-note category indexes were read (two EU news items
survive only as error pages; their US equivalents were read). None mentions
boot camp, the tutorial missions, recruit start, skipping, the dropship pad or
the D13.4 abandon issue; the two previously unchecked pages are the D16 and
D16.3 public-test notes. Posts outside those categories were not all
retrieved. Coverage matrix `patch-notes/derived/coverage-matrix.tsv`
(`86e6ded161273248734c9f3de259c6d10076c50687c2f7a467163dff16781660`). The
1.16.5.0 package dates `adv_bootcamp.map` and its `lights.xml` 2009-02-11,
with about 25 AFS-base maps, while the other boot-camp files keep 2008 dates;
this fits D16's "All AFS buildings and structures have updated new looks" but
does not prove it. August 2008 descriptions may therefore not match the final
structures, and the 1.16.5.0 map remains the authority. The official
"Creating Your Character" and "Wilderness Walkthrough" guides were never
updated after D11 and describe the obsolete tutorial and bypass mission.

**All generated tables re-decoded.** Two independent decoders agree on every
binding in all 369 `game.zip` members. The only module-level lists hold no
boot-camp ids, so the earlier negatives stand (one earlier "empty" table,
`loadingscreenplayermessage`, holds 19 generic tips). New bindings: class
24883 is the Eloh hologram NPC (`CREATURE_BIRTH_NPC_ELOH_HOLOGRAM_LARGE`,
creature and NPC augmentations, same mesh as the placed prop); the placed
hologram prop 23936 has ambient audio set 2764; item template 122875 is the
boot-camp pistol (class 29803, Firearms 1) and 123278/123279 the Bane
boot-camp pistol and rifle. No table ties any of them to context 1985 or a
mission. Comparing with the 2007-11 client, every rebuilt mission row, name,
indicator 430–439, audio set 2763–2789, bark 852 and greeting 1634–1636 is
new, while the hologram classes, Tizzik Gi's name and Logos 23 already
existed. Bindings: `list-tables/bootcamp-bindings.json`
(`8e30a05b67dcd551846cca334d3829a5999441599fcd69e95507f660d5871a1e`).

**Community sources.** The official sites had no forums in this period.
French JeuxOnLine republished the D11 test-server notes: the boot-camp map was
completely reworked "including its content, missions, loot and monsters", so
pre-D11 spawn and loot data must not be reused. Player posts from August and
October 2008 mention the drill sergeant, blinking mission objects, class gear
at level 5, and Lightning received "right at the start of the tutorial"; they
fix no event, position or amount. Bainbridge's archive.org screenshot sets all
predate D11 and show only the obsolete tutorial.

### Gap ranking after the sweep

Boot camp cannot start without the first-login position and rotation (no
source; the format has no spawn record, and DB teleporter rows 60/99/598 are
emulator-authored), the NPC classes and placements for packages 2560–2564,
2584 and 133 plus the hologram and McAllister, and the event that grants
mission 1990 (footage shows it accepted in Luna Cavern at level 1, not
what granted it). The chain cannot complete without objective order and
triggers, the crate class and contents, the Power Logos grant event and
protocol (`LogosStoneAdded` 475 versus `LogosStoneTabula` 477), the enemy
roster and Tizzik Gi's class, the bomb and dropship objects with their
timer and failure semantics, and the exit trigger with the Alia Das arrival
position. Rewards, the promotion's grant (level 2 by 1994 and level 3 by
1995 in footage, cause unseen), the skip outcome, tip and audio scheduling
(one server-sent tip observed), the enemy spawns and their reward tables (kills
paid experience, credits and loot, values unknown), the persistent Bane
assault and instancing remain open after that. The server-side mission-log mechanism these need is recorded in
[mission research](mission-research.md#mission-log-protocol-and-persistence--2026-09-13).
