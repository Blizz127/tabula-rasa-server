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
