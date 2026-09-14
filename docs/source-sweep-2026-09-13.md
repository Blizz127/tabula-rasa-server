# Evidence-source sweep — 2026-09-13

Seven parallel research tracks searched for anything that helps preserve Tabula
Rasa as it existed immediately before the February 2009 shutdown: other emulator
projects, protocol artifacts, packet captures, official documentation, archived
community sites, footage, and non-English sources. This document is the
consolidated, prioritized record.

Raw evidence, manifests and per-track reports are outside the repository at
`/home/blizz/backups/rasa-net/research/20260913-source-sweep/` (~531 MB, 9,033
files at hand-off, still growing — four harvest jobs were live). Per-track
reports, which this document summarizes rather than replaces:

| Track | Report | Lines |
| --- | --- | ---: |
| GitHub / code hosts | `github/findings.md` | 1,480 |
| archive.org / Wayback | `archive/findings.md` | 681 |
| Footage | `footage/findings.md` | 963 |
| Local inventory | `local/findings.md` | 957 |
| Forums / RE communities | `forums/findings.md` | 1,042 |
| Guides / databases | `guides/findings.md` | 683 |
| International / localized | `international/findings.md` | 304 |

## How to read this

Provenance tiers are `AGENTS.md`'s: `original` (client/map data or official
notes), `observed`, `measured`, `inferred`, `analogue`. Two distinctions are
enforced throughout and must not be collapsed:

- **Live vs public test server.** The official site separated these explicitly
  (`/news/patch_notes/` vs `/news/patch_notes_public_test/`, EU nav item "Test
  Server Access", German `_auf_dem_oets`).
- **Pre-revamp vs post-revamp.** Boot camp was rebuilt by **Deployment 11, live
  2008-08-15**. `AGENTS.md` forbids using pre-rebuild content as an analogue for
  rebuilt content, so pre-D11 boot-camp evidence is a separate category, not
  weaker evidence for the same thing.

Language does not change tier. Original-language text is preserved verbatim
beside its English translation; the original is the citable artifact and the
translation is a reading of it.

## 1. Corrections register

Several claims circulated during the sweep and were disproved before reaching
this record. They are listed first because each would otherwise have entered the
evidence base as a citation.

| Claim | Status | Correct position |
| --- | --- | --- |
| `itemtemplate_armor`'s original values are `352/234/469/586/703` | **False.** Those numbers appear nowhere in the decoded client tables. | See §5. Verified figures are given there. |
| The five hardcoded armor ids "have no name in the original client" | **False for two of five.** `13066` = "Bio Armor Helmet", `13096` = "Bio Armor Vest". | Three ids are genuinely absent; two are present. |
| `ePEbTUrfQ0o` is CommanderGrog's shutdown video | **False.** It is *"'UDAAN' – Installation X Tabula Rasa"*, a 2023 art piece. | Real shutdown footage: `j_4B22Y8z28`, `P40g1AEuLlY`, `CUnkvStC93o`. |
| `C2rGwo6fLw0` is the Russian "Ландыши – Мать (13.02.2009)" video | **False.** It is Raisuly episode #17. | The Russian video's id is **unresolved**. |
| EU has 12 classes vs US 15 (a roster divergence) | **False.** Derived from a truncated 12-row sample. | 38 `afs_class` slugs across de/en/fr are one roster translated. US final-live roster is **15**, confirmed from `/game_intel/abilities/` captured 2009-01-07. |
| DE `mikrobiologe` vs `xenobiologe` is a stale duplicate | **False.** Two distinct tier-4 classes, both level 30, same 2009-01-06 crawl. | DE `mikrobiologe` = EN **`medic`**; DE `xenobiologe` = EN **`exobiologist`**. Explains why US has `exobiologist.html` and no `microbiologist.html`. |
| `/game_intel/armor/` can populate `itemtemplate_armor` | **Withdrawn.** Would compound a defect. | Website armor pages are corroboration and naming only; mitigation must derive from the client's `armorclass`. |
| "Operation Immortality" had an in-game event component | **Not supported.** The official feed is entirely real-world PR. | See §7. The in-game tie-in remains an open question, not a documented event. |
| Early CDX results showing `4players.de` / `pcgames.de` as `cdx=0` | **Invalid.** Throttling scored as emptiness. | Discarded and quarantined; direct queries return captures from 2000 and 1997. |
| The official strategy guide is by Prima | **False.** | It is **BradyGames**, ISBN 0-7440-0943-X / 978-0-7440-0943-9, 272 pp., 2007-10-23. No public full text or scan exists (§8). |
| `rgtr.com/game_intel/images/strat_guide/TRguide*.jpg` are strategy-guide page scans | **False.** Visually inspected: in-game screenshots. | No page imagery of the BradyGames guide survives anywhere located. |

A methodological error worth recording because it produced two of the above: an
HTTP 200 with substantial bytes proves a page exists, not that it concerns this
game. A guessed `4gamer.net` game id returned 131 KB of real content for *Gary
Grigsby's World at War*. Every cited URL in the sweep was identity-checked
against its `<title>` or an in-page marker; the archive track audited all 416
recovered files this way and found 415 with an in-page `Tabula Rasa|NCsoft|rgtr|playtr`
marker, the exception being `rgtr.com/jsfiles/pvpRank.js`, where no prose marker
is expected.

## 2. The reframing result

The sweep's most consequential finding is not a new external source. It is that
**the project already holds `original`-tier answers to most of its content gaps
and does not reference them.**

`docs/client-artifacts.md` names decoding the client tables "the highest-value
next step". That step is **already complete**:

- All **369** `data/game.zip` generated tables are decoded and verified at
  `research/20260913-bootcamp/list-tables/decoded/`, with
  `verify/decode-completeness-check.json` reporting **369 members, 0 mismatches**
  across sha, crc, timestamp, subscript count, name count and attribute count.
  Cross-checked by an independent raw-marshal decoder and by uncompyle6.
- **996** xdis disassembly files (44 MB) and **47** uncompyle6-decompiled `.py`
  sources for `trpython.zip`, at `research/20260913-bootcamp/client-code/verify/`.
- Bytecode confirmed **Python 2.4** (magic 62061) from both archives.
- `research/20260913-bootcamp/list-tables/module-index.json` indexes **23,467
  named bindings** across the 369 members.

**No file in `docs/` references any of this.** Recommended fix: record the
decoded corpus and its verification in `docs/client-artifacts.md`, replace the
"highest-value next step" framing with the actual next step (using the decoded
tables), and note the uncompyle6 3.9.3 const-list defect already documented at
`list-tables/verify/uncompyle6-constlist-defect-scan.json`, which is why the
literal decoder is authoritative.

Two corollaries that remove work from the gap list:

1. **`GameOpcode.cs` is a verified 1:1 copy of the original client's
   `generated/client/methodid.pyo`** — 978/978 names, 0 missing, 0 extra, 0 value
   mismatches, 0 duplicate values. No external opcode list is needed or would
   help. `methodid.pyo` sha256 `cc75aa76…412d8`, zip mtime 2009-02-09 21:56:38.
2. **Client→server argument shapes are recoverable locally for all 170 methods
   the original client can send**, parsed from the disassembly
   (`client_send_sites.json`). The client calls
   `gameclient.SendCallUserMethod('<Name>', (<args>))` and resolves the name
   through `methodid.pyo`, so each method's exact arity and argument names are
   readable. That is what a packet dump would supply, obtained from the
   authoritative artifact — **which is why the absence of any capture (§7) is
   less damaging than it first appears.**

For numeric content the decoded client tables are strictly stronger than any
website: `original` tier versus `observed`/`inferred` for a publisher marketing
page. Official web pages remain valuable for prose, mechanics rationale, dating,
and cross-checking.

## 3. The official site estate — resolved

Previously the project knew only `playtr.com`. The full estate is now
established, and the domain map was read off the **German D16.5 page's own region
picker** (capture `20090227152809`, two days after D16.5 went live) rather than
guessed:

| Host | Role | Captures | Best-captured era |
| --- | --- | ---: | --- |
| `www.rgtr.com` / `rgtr.com` | US official (launch era) | 6,043 | 2007–2009 |
| `www.playtr.com` / `playtr.com` | US official (final era) | 20,414 rows; `news` 1,709, `community` 1,184, `game_intel` 519 | through 2009-02 |
| `boards.playtr.com` | **US official forum** (UBB Threads) | 1,721 distinct URLs | 2007–2009 |
| `eu.rgtr.com` `/en` `/de` `/fr` | EU official, 3 locales | 1,936 | 2007-10 … 2008-12 |
| `eu.playtr.com` `/en` `/de` `/fr` `/eu` | EU official, same CMS, second host | en 552, de 732, fr 914, eu 276 | 2008-11 … 2009-01 |
| `webdev.ncaustin.com` | NCsoft Austin **staging copy** of the official site | 1 | — |
| `ftp.playtr.com` | media/FTP host | 2, both 404 | — |

`rgtr.com` = *Richard Garriott's Tabula Rasa*, the retail title. Official status
is established by: `operationImmortality.xml` carrying
`<copyright>Copyright (c) 2008, NCsoft</copyright>` and author "NCsoft Content
Writer"; EU footers reading `© 2008 NCsoft Europe Ltd.`; the page title
`Richard Garriott's Tabula Rasa® - Classes`; and the fact that it served
`/news/patch_notes/deployment_116_8152008.html`, which this repo's docs already
cite.

The region picker lists exactly four regions — UK → `eu.rgtr.com/en`, FR →
`eu.rgtr.com/fr`, DE → `eu.rgtr.com/de`, US → `www.rgtr.com/index.html` — plus
`eu.plaync.com/de`, `secure.plaync.com?language=de`, `de.support.plaync.com`.

**Locale inventory, verified:** `afs_class` and `field_training` sections exist
in **de, en, fr only**. `es` and `it` have **zero** locale rows on either EU host
from a full-domain HTTP-200 query returning 1,548 unique URLs — a genuine
negative, distinct from throttle failures. ES and IT received localized retail
**manuals** without a localized website. Legacy `/eu/` is the 2006 pre-launch
English site and must not be used for final-live content.

**`field_training` exists only on EU hosts.** All four US hosts return 0 rows for
it, so the English copies at `eu.playtr.com/en/…` and `eu.rgtr.com/en/…` are the
closest English-language official text for control points and cloning.

The EU site's own navigation gives the definitive map of what NCsoft documented:
AFS Pathfinder (Abilities, Allies, Armour, Engineering, Know your Enemy,
Planetary Atlas, Understanding Logos, Weapons Range); Field Training (Guides,
Field Duties, Entry Requirements, **Test Server Access**, Soldier Commands, Using
your HUD, Soldier Registration, Your Challenge Coin); Community (Clans, Contests
& Promotions, Events, Frontline Artists Network, Feedback Form, Forums, Fansites,
Role Play, AFS Freedom Corps, Veteran Rewards, **PvP Rankings**); News (General,
Community News, Server News, Server Status, Patch Notes, Awards); Support.

German localization vocabulary, useful for reading DE captures: *Deployment* →
**Offensive**, *Patch Notes* → **Patchnotes**, *Field Training* →
**Kampfausbildung**, *Character Cloning System* → **Das Replikationssystem**,
*public test server* → **OETS**, *PvP Rankings* → **PvP-Ranglisten**, *AFS class
ladder* → **AFS-Karriereleiter**.

## 4. Protocol position

### 4.1 TNL is Torque Network Library — and it is **not** TR's transport

The sweep found a complete TR reverse-engineering toolkit by GitHub user
`Blumster` (`TNL.NET`, `TNLPacketAnalyzer`, `DataLoader`, `GLMExtractor`,
`ChunkReader`, `XmlToSql`, `TRRM`). `Blumster_TNL.NET/README.md` states outright:
*"Torque Network Library rewritten in C# … It is still incomplete.
SymmetricCipher isn't implemented."* So the hypothesis that TNL meant Torque
Network Library **held** — but the further hypothesis that TR's wire protocol is
TNL-based **did not**:

| Evidence | Source |
| --- | --- |
| TNL is UDP (`MaxPacketDataSize = 1490`, `PacketStream.SendTo(TNLSocket, IPEndPoint)`) | `Blumster_TNL.NET/TNL.NET/Network/TNLSocket.cs:16`; `Utils/PacketStream.cs:22-25` |
| This repo has **no UDP socket anywhere** — zero hits for `ProtocolType.Udp` / `SocketType.Dgram` in `src/**` | grep |
| This repo has **no TNL layer** — zero hits for `BitStream`, `NetInterface`, `NetConnection` | grep |
| Infinite Rasa's C++ server opens `socket(AF_INET, SOCK_STREAM, IPPROTO_TCP)` | `upstream-game-server/src/main.cpp:18` |
| The only protocol document found describes TCP framing, not TNL bitstreams | `upstream-game-server/gameData/TabulaRasaPacketFormat.txt` |

Unresolved and left as an explicit gap: `TNLPacketAnalyzer`'s ghost classes *are*
TR-specific (`GhostCharacter` carries `HeadId/BodyId/MouthId/EyesId/HelmetId/HairId`,
`PrimaryColor/SecondaryColor/SkinColor/HairColor`, `ClanName`, `PetCBID`,
`GMLevel`), so **some** TR build used TNL. Which one — a beta build, an unused
code path, or shared NCsoft engine code — is unknown. The repos carry no TR
version string and no capture. Do not resolve this by guesswork.

Those ghost classes remain useful as an `inferred`-tier **field inventory** with
explicit bit widths (a cross-check on appearance payloads), but they are invalid
as a wire spec. Note `HairColor:u3` and `Coid:u64`/`CBID:u20`/`MaxHP:u18`/`Faction:u16`.

### 4.2 Framing — three independent sources agree

`upstream-game-server/gameData/TabulaRasaPacketFormat.txt` (172 lines, author
"J.H.") specifies: a 5-byte aligned header + `u16 subSize`; a second header with
`u8 channel` (`0x00` game, `0xFF` ping, any other id creates an "ephemeral"
channel) and `u8 ukn`; a channel-0 header with `flagMask` and a groupVar `opcode`;
an optional XOR checksum; four `methodInfoFormat` variants; a `formatPrefix` of
`'M'`, `'F'`(?) or `'O'`(?) with `'M'` meaning CPython-marshal-serialized tuple;
and groupVar/dynVal encodings with prefix bytes `0x09`/`0x08`/`0x07` (gv64/gv32)
and `0xCB` (dv32).

`GameServerLoginSequence.txt` supplies a **raw byte capture** of the login
handshake:

```
00 02 02 00 03 03 29 03 01 07 | FC 56 01 00 | 07 | 78 9C 75 7A | 0D | CB |
08 "1.11.6.0" | 2A | 1A
```

`src/Rasa.Game/Packets/PythonCallPacket.cs:61-109` reads exactly that sequence,
constant for constant, including the `0x07` gv32 prefixes, the `0x0D`/`0xCB`
pair, the `0x29` tuple marker and the `0x2A` terminator. The only difference is
the version string: the capture says `1.11.6.0`, our check at
`PythonCallPacket.cs:106` requires `1.16.5.0`. **This is the strongest protocol
evidence found anywhere in the sweep** — an original-era byte capture our
implementation reproduces exactly.

Full chain, as implemented: 2-byte little-endian length prefix including itself
(`LengthedSocket.cs:212-224,373-377`) → Blowfish-CBC with an MD5-of-session-key
IV, `0xCC` padding to 8, leading plaintext byte = pad count (`GameCryptManager.cs`,
`Client.cs:418-452`) → frame `u16 size, u8 channel, u8 pad`, plus `seq`/`0xDEADBEEF`/
`skip` when channel ≠ 0 (`ProtocolPacket.cs:36-73`) → an envelope of only 9
`ClientMessageOpcode`s, 4 accepted inbound, optional raw Deflate with declared
length (`:78-124`) → RPC payload delimited `0x4F`…`0x66` carrying groupVar
entity/method ids in a TR-proprietary tagged-tuple format (high nibble = type,
low nibble = inline value when ≤ 0xC, else `0xD`/`0xE`/`0xF` length prefix; UTF-8
strings) per `PythonReader.cs` / `PythonCallPacket.cs`.

The `0x66` terminator is **our knowledge only** — it is not in
`TabulaRasaPacketFormat.txt`. Two independent implementations agree on the custom
6-entry Blowfish P-array `{0x243F6A88, 0x85A308D3, 0x13198A2E, 0x03707344,
0x9216D5D9, 0x8979FB1B}` (`src/Rasa.Utils/Cryptography/Game/Blowfish.cs:10` and
`upstream-game-server/src/TabulaCrypt2.cpp:9-11`); P[0..3] are standard π digits
and **P[4], P[5] are custom**. The **auth** cipher is a different variant with a
standard 18-entry P-box of entirely non-π constants
(`src/Rasa.Utils/Cryptography/Auth/Blowfish.cs:125-128`) and remains
single-sourced to our own code.

### 4.3 Coverage counts

| Quantity | Value |
| --- | ---: |
| Named opcodes in original client (`methodid.pyo`) | **978** |
| Named opcodes in `GameOpcode.cs` | **978** (0 differences) |
| Opcodes with an emulator packet class | 369 (37.7%) |
| Opcodes with no packet class | 609 (62.3%) |
| Live `[PacketHandler]` registrations | 154 |
| Client→server packet classes | 167 (154 handled, **13 unhandled**) |
| Server→client packet classes | 233 (179 instantiated, **54 never sent** = dead code) |
| Methods the original client can send | **170** (from 192 disassembled call sites) |
| — handled | 102 |
| — **unhandled** | **68** |
| Server→client receivers the client implements | **443**; we send 175, so **268 never exercised** |
| Registered handlers that are log-only stubs | **22** (`Shout`, `Emote`, `GuildChat`, `ToggleAfk`, all clan-feud and name-change messages) |

The 68 unhandled methods are whole subsystems: **crafting (10)** —
`RequestCraftItem`(151), `RequestCraftItemNew`(844), `RequestDisassembleItem`(676),
`RequestExtractModule`(845), `RequestIntegrateItem`(848), `RequestModifyItem`(677),
`RequestRetrieveAllFinishedItems`(750), `RequestRetrieveFinishedCraftItem`(521),
`RequestSalvageItem`(846), `RequestUpgradeItem`(849); **minions (9)**; **player
trading (8)**; **instance selection / travel (4)** — `ReturnToWormhole`(686),
`ScriptableClientEvent`(643), `SelectInstance`(687), `SelectInstanceCancel`(688);
plus `SelectNewCharacterClass`(177) — the class-advancement gate for progression
segments 3–6 — `RequestUseCloneCredit`(706), `BuryMe`(35), `ReviveMe`(170),
`RequestRevive`(763), `RefuseRevive`(762), `RequestLootItemFromCorpse`(652),
`CancelCorpseLooting`(647), `PurchaseClanLockboxTab`(868), `OverflowTransfer`(308).

Also: 6 radio/shared-mission opcodes are decoded purely to avoid disconnects;
`PrivilegedCommand` is dropped at `ChatCommandsManager.cs:752`; silent-drop paths
are `PacketRouter.cs:41,53` and `CallServerMethodMessage.cs:106`; and **two live
`NotImplementedException`s at `PythonReader.cs:192,227`** would kill a connection
on string type tags `0x41`, `0x42`, `0x52`.

### 4.4 The `0x41`/`0x42`/`0x52` tags — an open gap, with a reasoned hypothesis

Our tag scheme (`PythonReader.cs:9-20`) is a nibble-family encoding, not CPython
marshal: `Structs 0x0_ | Int 0x1_ | Long 0x2_ | Double 0x3_ | String 0x4_ |
UnicodeString 0x5_ | Dictionary 0x6_ | List 0x7_ | Tuple 0x8_`, where the low
nibble is a size/immediate selector. That is proven for `Int` (`≤0x1C` immediate,
`0x1D` sbyte, `0x1E` int16, `0x1F` int32) and `Double` (`0x30`→0.0, `0x31`→1.0,
`0x3E` 8-byte, `0x3F` 4-byte). The natural reading would make `0x41`/`0x42`
inline length-1/length-2 strings and `0x52` an inline length-2 unicode string.

**That reading is not asserted here.** No source in the sweep supports it: both
Blumster TNL repos have zero hits for these tags; `TabulaRasaPacketFormat.txt`
never enumerates marshal tags; Infinite Rasa implements only `'M'` (CPython) with
tags `0x28/0x5B/0x7B/0x69/0x74/0x66/0x4E/'T'/'F'`; and CPython 2.4 marshal has no
`'A'`(0x41) or `'R'`(0x52) codes at all — those are Python 3.4+. One asymmetry
argues against the uniform rule: `ReadString` stubs **both** `0x41` and `0x42`
while `ReadUnicodeString` stubs **only** `0x52`, leaving `0x51` to fall through to
`default: throw "WTF? String type"`. That suggests the three tags were each
observed individually in traffic and stubbed ad hoc.

Authority for the `'O'` format is the client itself (`tabula_rasa.exe` / its
marshal module). The cheaper empirical route is to log the offending bytes at
`PythonReader.cs:192,227` from a live session. Until then these remain two live
connection-killers and an explicit evidence gap.

### 4.5 Auth cipher independently documented

The Google Code project `tabula-rasa-server-emulator` had **exactly one issue
ever filed** — `issues/1`, "Encryption Help", posted 2013-06-18 by "Happy
Elephant", sign-off *"Auto Assault Revival!"*:

> "The blowfish key is good, but watch out, I had problems, b/c i forgot the
> **trailing 0 byte** from the key. Also the unknown encryption you had problems
> with is a **simple DES**. I recommend using `DESCryptoServiceProvider`
> (**keysize: 64** key: **T E S T 0 0 0 0** **ciphermode.ECB** **paddingmode.none**)
> **24 bytes encrypted (14 byte username, 10 byte password)** **6 bytes
> unencrypted (last 6 bytes of password)**"

`src/Rasa.Auth/Packets/Auth/Client/LoginPacket.cs` matches element by element:
BouncyCastle `DesEngine`; key `{0x54,0x45,0x53,0x54,0x00,0x00,0x00,0x00}` = ASCII
`TEST\0\0\0\0`; independent 8-byte blocks (ECB); fixed 30-byte buffer with no
padding; loop bound `i < 24`; username from bytes 0–13; password field spanning
bytes 14–29 (10 encrypted + 6 plaintext). So the 30-byte credential block is
`[0..13]` username DES-encrypted, `[14..23]` password first 10 bytes
DES-encrypted, `[24..29]` password last 6 bytes plaintext, followed by `GameId`
(uint32) and `CDKey` (uint16).

Provenance is class (b): community reverse-engineering by an identified third
party, stronger than inheritance but **not** NCsoft documentation, and `TEST0000`
looks like a default/test value deserving its own scrutiny. The reporter also
names a specific pitfall — the trailing NUL byte of the Blowfish key — which is an
audit point for `Rasa.Utils/Cryptography/*/Blowfish.cs`.

**Observed asymmetry, unresolved:** in `LoginPacket.cs` the `Encrypter` is
commented out, so `Write()` emits the 30-byte block in **plaintext**. If the
retail client expects the DES-encrypted form server→client this is a live defect;
if the direction is client→server only it is correct. The issue text does not
resolve it. **Evidence gap.**

### 4.6 A cheap, high-confidence verification is available and untested

`upstream-game-server/src/crypt_test_arrays.h` (138 lines) contains `InputK[0x40]`
(a 64-byte key) and `CompareD[0x1018 + 0x10]` (4,136 bytes of expected output) — a
complete key-expansion known-answer vector. Our `src/**` has zero hits for
`CompareD|InputK|crypt_test`. **Adding a test that expands `InputK` and compares
against `CompareD` would verify our cipher against original-era derived data at
essentially zero cost.** It validates our implementation, not retail fidelity.

Separately, `downloads/ltsochev/JHLIB/AccountCrypter.cpp:552-573` is a 2011
reverse-engineering of the client's own `Tabula_CryptInit` / `Tabula_Encrypt` /
`Tabula_Decrypt`, citing client executable addresses (`OutputData_D22D48`,
`InputData_0CEA0B8`, `sub_A7E190_1`), the same hardcoded 8-byte key
`{'T','E','S','T',0,0,0,0}`, and 8-byte block alignment (`Len &= ~7`), with
comments *"Seems to work fully!"*. **We have no counterpart to `Tabula_Crypt`**
(zero hits for `TabulaCrypt|Tabula_Crypt|CryptInit|Keyintegrate` in `src/**`); our
`ClientCryptData` holds only `Key`, `MD5[16]`, `K[64]`. Whether `Tabula_Crypt` is
a distinct layer or the same thing under another name is unverified, though the
`K[64]` ↔ `InputK[0x40]` size match is suggestive.

### 4.7 What no external source provides

Infinite Rasa's C++ server handles 50 client→server methods; of those, exactly
one is not referenced by our handlers: **`ReviveMe` (170)**. Its semantics
(`MapChannel.cpp:649-651`, `manifestation.cpp:1418-1458`) are *"dead player wish
to go to the hospital"*; payload is a tuple with one int `graveyardId`, which IR
parses and ignores (`// todo: use this`); response is `Recv_Revived(sourceId)`
broadcast over the cell domain with `sourceId = 0`. IR then hardcodes a teleport
to `(786.92, 294.83, 362.38)` and refills health.

**Those coordinates are an IR author's guess** — a single hardcoded graveyard for
all maps with `graveyardId` discarded. That is post-shutdown private-server
invention and must not be seeded as `original` or `observed`. The reusable parts
are the **arity** (one int) and the **response name** (`Recv_Revived(sourceId)`).
Relevant to `docs/death-research.md` and `docs/hospital-recovery-evidence.md`.

For the other 67 unhandled methods and the 268 never-exercised receivers, **no
public artifact helps**: IR implements none of crafting, tinkering, minions,
trading, instance selection, `SelectNewCharacterClass`, `RequestUseCloneCredit` or
`BuryMe` (each appears only in its own `methodIDs.h` enum definition), and IR
emits only **57** distinct server→client method ids against our 175. Those gaps
must be closed from the client, not from other emulators.

## 5. `itemtemplate_armor` is a defect, verified

`src/Rasa.DBL/Services/Preloader/ItemTemplateArmorPrelaoder.cs:18-22` (note the
filename typo) yields exactly five hardcoded rows — a hand-written stub, not a
load failure:

```csharp
yield return new object[] { 13066, 35 };
yield return new object[] { 13096, 23 };
yield return new object[] { 13126, 59 };
yield return new object[] { 13156, 70 };
yield return new object[] { 13186, 0 };
```

Checked directly against the decoded original client tables:

| id | Our value | Original `armorclass` | Original client name |
| --- | ---: | --- | --- |
| 13066 | 35 | `[10000, 10000, 50]` | **Bio Armor Helmet** |
| 13096 | 23 | `[4089, 4089, 20]` | **Bio Armor Vest** |
| 13126 | 59 | **absent** | **absent** |
| 13156 | 70 | **absent** | **absent** |
| 13186 | 0 | **absent** | **absent** |

`armorclass` has 3,377 int-keyed entries, each a **3-integer tuple**;
`physicalentityclassnamelanguage` has 15,967. So three of the five rows reference
ids that do not exist in the original armor data at all, the two that do exist are
3-tuples bearing no relation to the stored scalars, and **the schema shape itself
differs**. The five ids are the Motor Assist Armor set (Boots, Gloves, Helmet,
Legs, Vest) per Infinite Rasa's `itemtemplate` name column.

Runtime consequence: `ManifestationManager.cs:1094` sums this invented scalar for
mitigation while the verified `armorclass` `Min`/`MaxDamageAbsorbed` are **loaded
and never used** — `:1098` literally carries the comment *"what about damage
absorbed?"*. **2,500 of 4,985** item templates sit in armour slots, so **armour
currently mitigates nothing.**

A second, independent inconsistency: `src/Rasa.Test/NewCharacterTests.cs:82-91`
expects **47 / 70 / 59** for ids 13126 / 13186 / 13156 — matching Infinite Rasa
and contradicting our seeder's 59 / 70 / 0. The seeder's values look positionally
shifted (row 3 holds IR's row-4 value, row 4 holds IR's row-5 value, row 5 holds
nothing, IR's row-3 value 47 is dropped). The test does not catch this because it
builds its own fixture from the `Items` array (`:120`) and asserts against it
(`:182`) rather than reading the seeded database. So **our test fixture encodes
the correct values while the shipped seed does not.**

Provenance of the alternative numbers: Infinite Rasa's `itemtemplate_armor` has
13,893 rows, but a join through our own `itemtemplate_itemclass` (30,225 rows)
shows **13,893/13,893 exact matches** on `damageAbsorbed == armorclass.min_damage_absorbed`
and `regenRate == armorclass.regen_rate`, with zero unmapped ids. IR's table is
therefore the client's `armorclass` fanned out to `itemTemplateId` — **no new
retail data**. Its one extra column, `armorValue`, matches `round_half_up(damageAbsorbed/10)`
in only 13,462/13,893 cases (**96.90%**), with failures inconsistent in direction
(`7295→729` truncates, `469→47` rounds up). Its source is unknown and it has **no
counterpart in the client**, whose `armorclass` is definitively a 3-int tuple with
no fourth field (`rows: 3377`, `value_shapes: {tuple3: 3377}`,
`elem_shapes: {int: 3}`, `zip_mtime [2009, 2, 9, 21, 56, 28]`).

**Recommendation: do not import IR's values.** Retire the invented table and
derive mitigation from `armorclass`, or at minimum reconcile the seeder against
the test fixture and record the decision. Per `AGENTS.md`, an unsupported value
must remain an explicit evidence gap rather than silently becoming guessed
gameplay. Note our data model is arguably the more faithful one: ours is
`(id, armor_value)` with `regenRate`/`damageAbsorbed` equivalents living in the
separate verified `armorclass` table, while IR flattened all four into one table.

This is not a regression: `downloads/rasa-net-release-v0.0.2/rasaworld.db`
(Feb 2023) has identical row counts to today's database for every pre-existing
table, so `itemtemplate_armor` was already 5 rows then.

The official site corroborates the armour *model* but must not be used to
populate the table. `eu.playtr.com/en/afs_intel/armour` (captured 2008-08-28)
states: *"Currently, AFS troops employ **seven major types of personal armour** …
Bio · Graviton · Hazmat · Mech · Motor Assist · Reflective · Stealth."* The German
detail pages give the structured triple — `armour_type_gravitonpanzerung`
(2008-11-21): `Klassen-einschränkungen` = Kommandosoldat, Grenadier, Gardist;
`Bonus` = *"Resistenz gegen Betäubung und Rückstoß"* (stun + knockback
resistance); prose names the three official stats **Absorptionsrate /
Durchlässigkeit / Regenerationswerte**. `Durchlässigkeit` is the official German
name for what US pages call **Bleedthrough** and a French fansite called
«Perméabilité aux dégâts» — so the three-stat model (Absorption / Bleedthrough /
Regeneration) is confirmed by official pages in two languages, consistent with
`armorclass.Min/MaxDamageAbsorbed` being the intended mitigation source. A French
fansite's later reduction to two characteristics (2008-04-07) is an editorial
simplification, not a game change.

**Spelling question, recorded not resolved:** the official career-planner page says
Commando wears "**Gravitron**" Body Armor, while the armour URL slug is
`graviton_armor.html`, the EU armour overview says "**Graviton**", and the German
page says "**Graviton**". Weight of evidence favours **Graviton** (five sources
including the client's own FR/DE language tables and the official slug) over
**Gravitron** (one internally inconsistent official page). The client tables are
the tiebreaker for what actually shipped.

One further official change worth noting as a genuine conflict: Regeneration for
Motor Assist and Reflective is **"Low"** in captures of 2007-10-12 and
**"Average"** in re-slugged copies captured 2008-08-31. Both are official and
pre-shutdown. Whether that is a game change or an editorial re-wording is
**undetermined**; record both.

## 6. Content gaps, quantified against the original client

Seven tables are verified **byte-exact** against the original client — a result
`docs/retail-accuracy.md` does not currently claim:

| Emulator table | Rows | Original client member | Verdict |
| --- | ---: | --- | --- |
| `armorclass` | 3,377 | `armorclass.pyo::lookup` | **exact** — all 3,377 `(min,max,regen)` triples value-identical |
| `weaponclass` | 2,946 | `weaponclass.pyo::lookup` | **exact** |
| `itemclass` | 9,115 | `itemclass.pyo::lookup` | **exact** |
| `equipableclass` | 6,935 | `equipmentdata.pyo::equipableClass` | **exact** |
| `itemtemplate_itemclass` | 30,225 | `itemclass.pyo::itemTemplateItemClass` | **exact** |
| `itemtemplate_requirement` | 5,293 | `itemclass.pyo::reqData` | **exact** |
| `itemtemplate_requirement_skill` | 19,564 | `itemclass.pyo::itemTemplateSkillRequirement` | **exact** |
| `entityclass` | 15,838 | `entityclass.pyo::lookup` = 15,823 | **+15 extra rows in the emulator** — provenance review needed |
| `logos` | **166** | `logosstone.pyo::lookup` = **390** | **−224 missing (57%)** |

Gaps where the original client holds the definitions:

| Gap | Ours | Original | Client source |
| --- | ---: | ---: | --- |
| Logos definitions | 166 | **390** | `logosstone.pyo` (`lookup`/`byvalue`/`bystring` 390, `classlookup` 361, `logosSequences` 54) + `language/english/logosstonelanguage.pyo` (390) |
| Mission objectives | **0** | **3,454** | `missionobjective.pyo` |
| Mission objective conversations | **0** | **5,821** | `missionconversation.pyo` |
| Mission text strings | — | **32,988** | `language/english/missiontextlanguage.pyo` |
| Creatures | 140 | **4,099** names | client name tables |
| Maps | 78 | **784** game contexts | client map contexts |
| Vendors | 20 | **118** | client vendor data |
| Item templates | 4,985 | **30,225** built by the loader | `itemclass.pyo::itemTemplateItemClass` |
| Boot-camp tutorial events | recorded as unrecovered | **56** | `tutorialdata.pyo` — `TUTORIAL1, TUTMOVEMENT, TUTRUN, TUTCROUCH, TUTFIRE, TUTTARGET, TUTHEALTH, TUTHEALTHPOWER, TUTMISSIONS, TUTMISSIONGIVER, TUTMINIMAP, TUTMINIMAP2, TUTWAYPOINTS, TUTFOOTLOCKER, TUTHOSPITAL, TUTFALLINGDMG, TUTBUNK, TUTSPKWCPT, TUTSWITCHWEAPONS, TUTINVENTORYABILITY, TUTINVENTORYTOTRAY, TUTLEVELUP, TUTEXPLORE, TUTCTRL, TUTPATH3, TUTPATHBAD, TUTFORCEFIELD, TUTUSESHIFTER, TUTABILITIES, …` |

**`logos` needs care.** Our table's columns are
`id, class_id, map_context_id, pos_x, pos_y, pos_z, name` — it is a
world-**placement** table, not a definition table, so 166 vs 390 conflates "stones
that exist" with "stones whose placement we know". The definition side (390
names, 54 sequences) is fully recoverable from the decoded client tables; the
placements are the part that still needs evidence, and
`docs/progression-preservation-plan.md` records that the original static map holds
no gameplay actors. Report the two halves separately rather than as one number.

Names are corroborated on all 166 overlapping ids by Infinite Rasa's
`gameData/logos.txt` (390 entries), with four differences that are pure
formatting (`Self`/`SELF_I_ME`, `Was Not`/`WAS_NOT`, `Will Be`/`WILL_BE`,
`Will Not`/`WILL_NOT`). The **count** is therefore verified `original`; the
**names** are corroborated but reach `original` only after re-running the existing
decoder over `logosstonelanguage.pyo` and diffing all 390.

Also unblocked now, from already-decoded tables rather than any external source:
crafting (`generated/shared/crafting.pyo`: `moduleItemTemplateTable` 5,380,
`moduleClassTable` 867, `entityClassSetTable` 205, `recipeItemTemplateTable` 160);
class advancement (`characterclass.pyo` 15 classes, `classset.pyo`
`tooltipsByClassSetId` 205, `skilldata.pyo` `skillData` 73 / `skillLevel` 359 /
`skillCharacter` 73, `abilitydata.pyo` `abilities` 1,084 / `skillRequirements` 53);
control points (`usabledata.pyo` `USE_CPOINT_STATE_*`, `USE_CCP_STATE_*`,
`USE_OCP_STATE_*`; `generated/shared/controlpointtype.pyo` 5;
`battlegroundrulestype.pyo` 3); map markers (`uimapmarker.pyo`: factioned 84,
static 80, maplink 73). Chat `/clan`, `/squad`, `/invite`, `/kick`, `/leave`,
`/tell`, `/say` need no external data at all — `CommunicatorManager.cs:346` routes
chat text to `ChatCommandsManager`, which registers only 31 `.`-prefixed GM/debug
commands (`ChatCommandsManager.cs:74-104`); **no retail `/`-command exists**.

The one gap that genuinely cannot be closed from the client is **loot**: no client
loot table exists, and Infinite Rasa's `creature_type_loot` has 7 rows.

**Provenance warning on inherited content.** `vendor`, `spawnpool` and `creature`
rows largely descend from KDRefugee's merged 2023 pull requests #84/#85/#86, whose
author flagged the armour numbers as wrong. Treat as unverified reconstruction.

`player_exp_for_level` has all 50 levels and matches the fork database exactly
(`L1=0, L2=3000, L3=10500 … L50=103410000`). **That is shared lineage, not
independent evidence** — both descend from one emulator ancestor, so the XP gap is
about *provenance*, not missing rows. Note also that the client's
`tExperienceLevel` schema carries `IDLevel, intExperience, iSkillPoints,
iAttributePoints, iResearchPoints` while our table is `(level, experience)` only —
a real schema gap.

## 7. Confirmed facts and confirmed losses

### 7.1 D16.5 corroborated by two official locales

**US** — `playtr.com/news/patch_notes/d165_021709.html`, capture `20090221145102`,
4,117 B, sha256 `047f56afb644563c209bc939a3f97cbfabe7106da74d9d3eb32339684bcc3b93`:

> Tabula Rasa® - D16.5: 02/17/09 — Patch Notes — D16.5: 02/17/09
> This pacth contains the following fix:
> Fixed a bug that caused Vulcan and Angel Mechs to be unavailable to players below level 50.

(The misspelling "pacth" is in the original and is a useful authenticity marker.)

**DE** — `international/eu_playtr_raw/eu_playtr_de_d165_now_on_live_servers.html`,
title "Offensive 16.5 auf dem Live-Server", **18. Februar 2009, 14:08, Verfasst
von Avatea**, footer `© 2008 NCsoft Europe Ltd.`:

> „Offensive 16.5 wurde auf die Live-Servers aufgespielt: Ein Fehler wurde behoben,
> der dazu geführt hatte, dass Spieler unter Level 50 nicht auf die "Angel"- und
> "Vulcan"-Mechs zugreifen konnten.."
>
> "Offensive 16.5 was deployed to the live servers: a bug was fixed that had
> prevented players under level 50 from accessing the 'Angel' and 'Vulcan' mechs."

Semantically identical, one day apart. This **closes the final-patch mech-access
item** in `docs/final-retail-target.md`: the Angel and Vulcan mechs were
level-50-gated and D16.5 fixed under-50 access. Record both citations. Note D16.5
is a single-fix patch pointing onward to a separate **D16** page, which should be
confirmed as captured.

### 7.2 Deployment timeline — 40 deployments, live and PTS separated

`archive/deployment-timeline.tsv` holds 257 rows; `patchnote-capture-inventory.tsv`
holds 354 capture rows (289 status-200) with a `recovered` flag. Series split:
**LIVE 140** (playtr 62, rgtr 65, eu.rgtr 13), **PTS 114** (playtr 45, rgtr 51,
eu.rgtr 18), unlabelled 4. Distinct deployments recovered: `7.7, 8, 8.3, 8.4, 9,
9.4, 9.5, 9.6, 10, 10.2, 10.3, 10.5, 10.6, 11, 11.4, 11.5, 11.6, 11.7, 11.8, 12,
12.3, 12.4, 12.5, 13, 13.2, 13.3, 13.4, 13.6, 13.8, 14, 14.5, 14.8, 15, 15.5,
15.6, 15.7, 16, 16.3, 16.4, 16.5`.

**Numbering resolved, with evidence from page titles rather than slugs:** 3-digit
slugs concatenate major+minor (`164`→D16.4, `165`→D16.5, `157`→D15.7, `118`→D11.8,
`96`→D9.6), and **US and EU use one scheme**. EU articles often announce the major
(`deployment_11_…_14_august_2008_live` = D11) while US articles announce the
sub-patch (`deployment_116_8152008` = D11.6 on 15 Aug). Cross-check: EU
`deployment_12_patchnotes_known_issues_18_september_2008` (D12, 18 Sep) vs US
`deployment_123_9042008`, `deployment_124_9122008`, `deployment_125_9182008`.

Classification uses only the official site's own signals, recorded per row:
`url_section:patch_notes_public_test` vs `url_section:patch_notes`, and slug
wording (`_on_pts`, `_is_now_on_pts`, `_now_on_pts`, `_test_server`,
`only_on_pts`, German `_auf_dem_oets` vs `_on_live`, `_is_now_live`, `_ist_live`,
`live_today`, `on_the_live_servers`).

**12 deployments have both a PTS and a LIVE record**, which is what dates when a
change reached live players. Measured PTS→live gaps: D8 14 d, D9.6 7 d, D10 20 d,
D11.6 3 d, D11.7 1 d, D12.5 1 d, D13.4 6 d, D14 11 d, D15 17 d. PTS-only:
`8.3, 8.4, 9.4, 9.5, 10.2, 10.3, 10.5, 10.6, 11.4, 11.5, 12.3, 12.4, 13.2, 13.3,
14.5, 15.5, 15.6, 16.3`. Live-only: `7.7, 16.4, 16.5`.

Parser residue flagged, not to be cited: D13.6 shows `date_page=2060-11-03`, D16
shows `2009-12-08`, D15.7 shows a −3-day gap; 10 of 257 rows carry an
out-of-window date, all flagged, none load-bearing for the D16.4/D16.5 conclusion.

Regional cadence diverged even though numbering did not: EU announced majors, the
US issued frequent sub-patches. A live US-only page announces the v16 series
(`patch_v16_series_coming_to_a_live_server_near_you_soon.html`) — the line both
regions converge on at the end. Every row carries its own `host` and `locale`;
nothing is collapsed into a single global date.

### 7.3 Revamp boundary pinned: D11 live 2008-08-15

Boot camp was rebuilt by **Deployment 11, live 2008-08-15** — D11.6 live notes
"Deployment 11 is on Live!" 8/15/2008 (rgtr.com capture 2008-08-28); D11.4
public-test notes 8/08/2008. This is the hard boundary between usable and
forbidden boot-camp evidence under `AGENTS.md`'s rule against using pre-rebuild
content as an analogue for rebuilt content.

Cross-check against existing repo evidence came back clean: `EiE2oodlP8A` "Tabula
Rasa - D11 Bootcamp Mini-Game" (Joe Kaczmarek, uploaded 2008-08-07) falls
**before** D11 live, matching `docs`' characterisation of it as public-test-server
footage. It is **not** cited in
`docs/evidence/bootcamp-d11-reconstruction-manifest.json`, which cites only
`7Lrst9SG3pk`, `8VXeKzGUv0c` and `Ycxm8Pa1-v4` — **agreement, not new evidence.**

### 7.4 Official event inventory

Recovered from `image_gallery/categories.js` — the gallery's own category list,
which is `original`-tier evidence of what official events existed: **13 Logos
Language · 14 Event Gallery · 15 Launch Event · 19 Tabula Rasa End of Beta Event ·
20 Halloween 2007 · 22 DE Day, Resurgence Day · 23 Tabula Rasa War College ·
24 Operation Immortality**. Images for categories **13, 20, 22 and 24 were never
archived** (only `12_*`, `18_*`, `19_8`, a `22_3` stub and `testserver1-4`
survive).

Halloween 2007 is independently corroborated by two original-era non-English
sources: Nico `sm1470947` "Tabula Rasaのハロウィン" (2007-11-07, five days
post-launch) and Dailymotion `x89hk6x` "Tabula Rasa : Halloween" (2007-11-06).

### 7.5 Operation Immortality — hypothesis corrected

The hypothesis that Operation Immortality included an in-game event component is
**not supported**. `www.rgtr.com/news/operationImmortality.xml` (recovered,
55,595 B, sha256 `7b6acee6…`, `<modified>2008-10-03T00:12:38Z</modified>`, Atom
0.3, 15 entries spanning 2008-08-14 → 2008-10-01) is entirely a real-world
PR/marketing operation: *"Operation Immortality™, the project to create a digital
time capsule of the human race"* — digitised DNA samples on the "Immortality
Drive" carried to the ISS aboard Garriott's Soyuz flight, launched Sunday
12 October 2008. Named participants include Tracy Hickman, Joe Ely, Stephen
Colbert, Stephen Hawking, Kevin Rose, Robert Scoble, Matt Morgan, Jo Garcia, nine
screenwriters and an Olympic gold medallist, plus an eBay charity auction.

**Zero in-game mechanics, items, missions or events appear in the feed.** The only
in-game hint anywhere in the recovered set is one link-out blurb in
`www.rgtr.com/news/operation_immortality/2.html` @ `20080905221931` paraphrasing a
third-party interview: *"…about his upcoming flight, **how it will be tied into
certain events in the game**, the state of Tabula Rasa, and more."* That is a
**lead, not evidence**; the interview is off-site and unrecovered. Record OI as an
out-of-game promotional operation, with the in-game tie-in an **open question**.

Corroborated from two independent source types: the official XML feed and YouTube
`DXmhQoopFBg` "Richard Garriott - Operation Immortality - Interview (2008)", plus
Dailymotion `x89hjlm` "Tabula Rasa : Opération Immortalité" (2008-08-01).

### 7.6 Control points — official producer statement

`eu.rgtr.com/en/field_training/guide/control_points`, page dated **1 Feb 2008
12:35** by **Starr Long, Producer**, © 2008 NCsoft Europe; recovered at capture
`20080307033544` (sha256 `46a78c0b…`) with a **later 2009-02-01 capture** at
`international/eu_playtr_raw/` — prefer the 2009-02-01 capture for final-live
claims and keep the 2008-03-07 one to diff for changes. Tier `original`.

Implemented behaviour stated in his own words: control points are "a keystone" of
the dynamic battlefield and NPCs fight over them; when ownership changes AFS↔Bane
*"that changes a bunch of variables in the environment, including what NPCs are
spawning and where they are spawning"*; *"this affects what missions are available
to the players, so control point ownership even directly influences a player's
ability to complete missions"*; *"When the enemy takes and holds a control point,
one of the things players can no longer access is the waypoint system within the
base"*; *"if they need to use a vendor or hospital in the area and the base is not
under AFS control, they will not be able to do so"*; *"We have also introduced
collectable medals that players receive for successfully defending or taking back
a control point. These medals can then be turned in for other rewards"*; some
control points need a large group or clan to hold. Named location: **Wilderness
LZ**.

**Do not implement the article's ninth point.** *"Eventually, I feel that the
control point system will evolve in such a way that the players who hold a control
point will receive some sort of incentive or bonus"* is explicitly **aspirational
future design, not shipped behaviour**. Items 1–7 are statements of implemented
behaviour; the holder bonus must stay an evidence gap.

Corroborated by the official Wilderness walkthrough (`rgtr.com/game_intel/official_guides/the_wilderness_walkthrough.html`,
captured 2007-12-13): *"If it falls into Bane hands, you will have to help reclaim
it before you can accept any new missions there."* Named control-point token
missions exist in the fan mission DB: `Landing Zone (CP): Assault Tokens` /
`Defense Tokens` (ids 38, 39) and `Imperial Valley (CP): Assault Tokens` /
`Defense Tokens` (ids 60, 61).

This maps onto `spawnpool`, `teleporter`, `vendor` and `npc_mission` gating — all
documented gaps.

### 7.7 Cloning — official lead-designer statement

`eu.rgtr.com/en/field_training/guide/character_cloning_system`, page dated
**1 Feb 2008 12:33** by **Paul Sage, Lead Designer**, © 2008 NCsoft Europe;
recovered at `20080307033539` (18,217 B, sha256 `4ba7a41b…`) with a later
**2009-02-01** capture. Tier `original`.

Cloning saves a character's progress so a player can try new classes or builds
without starting over. A **cloning credit is awarded at each tier choice**; extra
credits come from completing certain missions. Cloning was **originally unlimited**
and was restricted after an exploit — *"initially we had unlimited cloning …
unfortunately there were some exploits with the cloning system that we had to
address"* — where players cloned before a valuable mission to re-run it. **Respec
was added alongside the limit.** The **Medic** class separately creates a
temporary combat clone, which is a **different system**.

Fan corroboration (`ign.com/wikis/tabula-rasa/Player_Compiled_Cloning_Tips`,
player "Sitherain", `observed`, written before build 1.6): free cloning tokens at
**levels 5, 15 and 30** — matching the official `Erforderlicher Level` values
exactly; extra credits from 'Target of Opportunity' and Hybrid racial missions; a
clone has all skill and attribute points unspent except the mandatory basics
(Firearms 1, Motor Assist 1); no money or equipment beyond a basic pistol and
starter gear; cloning wipes the quest log, friends and ignore lists; clones share
the same bank space (footlocker); the name is not released even after deleting the
original (a bug). A fansite update archive adds that cloning reset **attribute
points as well as skill points** as of the 2008-01-30 patch, previously only skill
points.

### 7.8 Trainer gates 5 / 15 / 30 — now official

Previously known only from partial TaRapedia excerpts. Now attested by **37
official publisher pages in three languages**, captured 2008-11-20 → 2009-01-08
(the EN set 2009-01-05/06/07, seven to eight weeks pre-shutdown), with **no locale
disagreeing with any other**. Field labels: EN `Level Requirement` / `Tier Path`,
DE `Erforderlicher Level` / `Hierarchie`, FR `Niveau requis` / `Hiérarchie`. Tier
`original`.

**Recruit 01 · Soldier 05 · Specialist 05 · Ranger / Commando / Biotechnician /
Sapper 15 · Guardian / Grenadier / Spy / Demolitionist / Engineer / Medic /
Exobiologist 30.** The German Soldier page restates it in prose: *„Soldaten
beginnen ihre Ausbildung auf Level 5"*.

Corroborated independently by the official Wilderness walkthrough: *"you should be
very close to reaching level 5, where you will be receiving your first cloning
point and career choice. **You can't dip into level 5 until your character speaks
with a trainer.** Return to Alia Das and speak with either the Soldier or
Specialist Trainer."* Plus *"Upgrading in this fashion **every five levels** is a
good strategy!"* The same page states that **Logos persist through cloning**:
*"Once you learn a Logos symbol, it is part of your character forever. **Even
future clones of your character have full use of the symbol.**"*

Two official-page defects are recorded verbatim rather than silently corrected:
the EN `Tier Path` typo **`Specilaist`** on `demolitionist` and `medic` (DE/FR
spell it correctly), and EN `guardian`'s `Tier Path` **truncated** to
`Recruit, Soldier, Commando` (missing `, Guardian`) where DE and FR both carry the
correct 4-element path.

**Unverified:** the German `afs_class_scharfschuetze` (Sniper) page is not in the
cache. That is a **missing capture, not an absent page**; its required level is
expected 30 but unconfirmed.

### 7.9 The US final-live class roster is 15

From `rgtr.com/game_intel/career_planner/tr_class_tree_p_1.html` (23,204 B), whose
document title is `Richard Garriott's Tabula Rasa® - Classes`, and from
`/game_intel/abilities/` captured **2009-01-07**. Per class the page gives tier,
armour, weapons, abilities and sometimes tools:

| Class | Tier | Armor | Weapons | Abilities / Tools |
| --- | --- | --- | --- | --- |
| Recruit | 1 | Motor Assist Body Armor | Firearms (Pistol, Shotgun, Rifle) | Hand-to-hand Combat / Lightning / Sprint |
| Soldier | 2 | Reflective Body Armor | Machine Gun | Shrapnel / Rage |
| Specialist | 2 | Hazmat Body Armor | Leech Gun | Decay; Tools: Repair Disc, Armor Augmentation, Cipher, Healing Discs |
| Commando | 3 | Gravitron Body Armor | Grenade & Rocket Launcher | Force Blast / Scourge / Rushing Blow |

Full roster in document order: Recruit, Soldier, Specialist, Commando, Ranger,
Sapper, Biotechnician, Grenadier, Guardian, Spy, Sniper, Demolitionist, Engineer,
Medic, Exobiologist. Prose states ancestry explicitly — the Soldier *"is a
prerequisite for higher level Tier selection in this career path"*; the Specialist
*"retains all the skills and abilities learned as…"* — which is direct evidence
for the skill-prerequisite and class-ancestry gaps. The complete 15-class
extraction with all five fields is at `guides/rgtr-career-planner-classes.md`.

The roster is independently confirmed by `upstream-game-server/gameData/class.txt`
(`1 RECRUIT, 2 SOLDIER, 3 SPECIALIST, 4 COMMANDO, 5 RANGER, 6 SAPPER,
7 BIOTECHNICIAN, 8 GRENADIER, 9 GUARDIAN, 10 SNIPER, 11 SPY, 12 DEMOLITIONIST,
13 ENGINEER, 14 MEDIC, 15 EXOBIOLOGIST`) and by the decoded client
`characterclass.pyo` (15 classes).

**Locale↔locale class-name mapping** (launch era, `eu.rgtr.com`; full table at
`guides/eu-class-pages-locale-mapping.md`): rekrut/recruit/recrue ·
soldat/soldier/combattant · spezialist/specialist/technicien ·
kommandosoldat/commando/commando · ingenieur/engineer/ingenieur ·
**mikrobiologe/medic/medecin** · spion/spy/espion · aufklaerer/ranger/eclaireur ·
pionier/sapper/sapeur · **saboteur/demolitionist/artificier** ·
gardist/guardian/sentinelle · scharfschuetze/sniper/tireur_delite · grenadier ×3 ·
biotechniker/biotechnician/bio_technicien · **xenobiologe/exobiologist/exobiologiste**.

**One genuinely open item, recorded as open:** final-era EU crawls show
**en=12 / de=14 / fr=15** `afs_class` pages, while launch-era crawls show a
consistent **15 in all three locales**. Since the US `/game_intel/abilities/`
index lists all 15 and the cached EU pages total 15 distinct classes in union, the
shortfalls are most plausibly **crawl incompleteness** — but that is not proven.
Do not merge the crawls into one roster table.

### 7.10 Confirmed losses — evidence gaps, not to be filled by analogy

| Lost | Evidence it is lost | Recovery route |
| --- | --- | --- |
| **The official video library** | 27 FLV names recovered by static `.swf` inspection (zlib-decompress + string scan, **not executed**) of 45 preserved players: `tr_d11_342.flv`, `tr_logos_academy_720.flv`, `tr_creature_changes_342.flv`, `tr_intro_720.flv`, `tr_hd_720p_final.flv`, seven `tier4_*_flyround.flv`. `playtr.com` has **5,330 captures and zero** `.flv/.mp4/.wmv/.mov`; no `/movies/` path exists; the seven `rgtr.com/*.flv` captures are 1.5 KB 404 stubs; `ftp.playtr.com` has 2 captures, both 404; the host resolves to 64.25.35.120 but refuses TCP 21/80/443. Full list: `footage/raw/rgtr-media/official-flv-references.txt` | **YouTube re-encodes.** FREEMMOcom mirrors the official videos one-for-one: `rKkKyzfMRA4` "Deployment 11 HD" = `tr_d11_342.flv`, `8H5dN6PPX2o` "Logos Academy HD", `FvrFIqSxcCw` "Creature Changes HD", `s-A4zLGC3zQ` "Patches to Ashes HD", `649lZqn-nzY` "Halloween Invasion 2007 HD", `dZE-XJPMNHc` "Friday Night Fights HD", `2lVaQ6yrBcc` "TNA Operation Immortality HD", `UYu9rWT37ek` "Hybrids HD", `IvAKyA4sOeg` "Earth Defended HD", `JHu25BaWWaY` "The AFS and You HD", `KGwtWf2AZIw` "The Fight HD", `oyVhqbq1ihM` "Intro Cinematic HD", `wav9kRmBjkY` "E3 2006 Trailer HD". IGN preserves the 2007 set incl. `Idasd3KUbbw` "Mechs (HD Off-Screen)" and `ro6l3aGxFlA` "Gameplay - Death from". These are lossy re-encodes: **`observed`-at-best copies of `original` material.** For `tr_d11` they are the only surviving moving-image record. |
| **Any Tabula Rasa packet capture** | No `.pcap`, `.log`, `.bin`, `.dat` or hex fixture anywhere in 60 MB of downloaded repos; zero matches for `capture\|sniff\|wireshark\|pcap\|hex dump\|wire\|packet log\|proxy\|intercept` across all 63 issue comments on disk; every indexed-web query for a TR capture returned nothing. All 31 pcap-shaped hits on this machine are other projects. | Not needed for argument shapes — see §2. The only real captured bytes anywhere are the annotated login handshake in `GameServerLoginSequence.txt`. `TNLPacketAnalyzer` *consumes* a `RECV`/`SEND` text log format but ships none, so somebody had such a log; it is not in the repo. |
| **The J.H.Work emulator source** | `tabularasa_src.zip` ("Full source code for the J.H work Emulator as of April 18th 2011") and `tr_release_2.zip` both returned **404 from the origin** when Wayback crawled them, in **2013-07-23** and again in **2023-01-14**. High-confidence negative, not throttling: Wayback successfully archived six other binaries from the same host in the same era. | **Lost.** The devlog prose survives (all 10 posts archived 200 on 2011-03-04); the code does not. |
| **Google Code source, wiki and downloads for the origin project** | `source/*` has **zero** captures; `downloads/list` returned genuine **404** from all 17 captures across `can=1..8` variants (the project published no downloads); the wiki list **was read** — Wayback `20160427153703id_` of `/w/list?can=1` (15,122 B, HTTP 200) renders the full wiki UI with column headers `PageName / Summary + Labels / Changed / ChangedBy` and then **zero data rows and zero `/w/<PageName>` hrefs**; `storage.googleapis.com/google-code-archive/v2/…` returns **403 AccessDenied for every project including the control `protobuf`**, i.e. an ACL not throttling; only 2 pre-2015 captures exist, so the live 2011–2015 project was essentially never crawled. | **Confirmed negative: the project had no wiki pages, no downloads and no archived source.** It contributed only its description blurb, its member list, and **issue #1** — which is therefore the *entire* textual technical output of its public web presence. Any SVN revision history, branch or tag that did not survive Google's "Export to GitHub" is unrecoverable. Note the wiki lives at `/w/`, **not** `/wiki/`. |
| **`infiniterasa.com`'s 2011 development forum** | The board index is archived and shows **Development → "InfiniteRasa Dev", 44 topics / 371 posts** (last post 2011-09-25 by `Hellbone`), but a domain CDX returns only 27 rows, every `viewforum.php?f=5\|6\|11\|12` capture is **404**, and there are zero `viewtopic.php` captures with content. | **The 371 development posts were never crawled and cannot be recovered from Wayback.** `infiniterasa.com` is dead. Note `infiniterasa.**org**` is better covered than first thought — 200-status forum index captures for f=3,5,6,8–21 dated **2014-12-05**, plus 2016 captures, all queued. |
| **The official image gallery as UI evidence** | Inspected samples (`12_41.jpg`, `18_38.jpg`) are curated scenic/action shots with **no HUD visible**. | Cannot evidence UI panels with numbers. Can evidence visual appearance: weapon and armour models, the AFS beret uniform, Eloh hologram structures. |
| **The TR character planner** | `trcpt.crymore.de:8080` times out; the fan mission DB explicitly says "**Not available anymore!**"; a second builder `zeus.jrq.ch/trcb/index.php` returns HTTP 500. | The lost skill-tree planner was potentially the only structured prerequisite/cost source. Wayback is the only route. |
| **Original server-side data generally** | Three independent contemporaneous statements agree that retail TR kept content definitions **client-side** and the server sent references, not definitions. | This is the encouraging structural result: mission text, item names, NPC and creature names are recoverable from an authentic 1.16.5.0 client. **Placements, behaviour and stat values are not** — see §7.11. |

### 7.11 The capability ceiling, in the words of the people who hit it

The single most useful calibration in the sweep comes from **J.H.Work**, who wrote
a TR server emulator in C in early 2011 — roughly two years post-shutdown, the
earliest substantive reverse-engineering effort found. Archived at
`web.archive.org/web/20110304171245id_/http://jhwork.net/?category_name=tabularasa`
(the live site returns a zero-byte body; Wayback is the only copy). Class (b):
first-hand implementer reporting what he observed against a retail client.

Asked directly *"how authentic can the game world be recreated? Are there maps or
scripts … that show where exactly all the objects and NPCs used to be placed on
the live servers? Or is that something that will have to be guessed?"*, he answered
(2011-03-04 13:08):

> "I think about 90% can be recreated with some effort. In general, **everything
> that is related with text is stored in the client and not sent by the server.**
> This includes mission descriptions, item names, NPC and creature names and some
> other. **The hardest part of recreating the original would be to gather
> information of NPC and creature locations, their behavior and values like
> hitpoints etc.**"

And from the post "NPCs" (2011-02-12):

> "while working on mission support I once again noticed there are some facts about
> the client that will be problematic later. For example **item and mission info is
> stored in the client files and is not dynamically sent by the server.** On the
> good side, this means **we have access to all mission descriptions, item names
> etc. that ever were added to the game.** … But on the contrary custom items or
> missions cannot be created without touching the original game files."

Corroborated in kind, independently, by `krssrb` (rank Developer) on the
InfiniteRasa forum, 2017-01-19: *"**About packets structures, we search trought
python code inside game client.**"* (typos as in original).

**Working hypothesis this supports:** retail TR kept content *definitions*
client-side and the server sent references and ids. If correct, the emulator's
"hundreds of retail missions vs 2 database rows" gap is a **client-table extraction
problem, not a lost-data problem** — and §2 shows the extraction is already done.
What remains genuinely lost is exactly what J.H. named: **NPC and creature
placements, behaviour, and stat values such as hit points.** That is the honest
boundary of the reconstruction, and it is where `AGENTS.md`'s evidence-tier
labelling and its preference for leaving a value out over an `analogue` apply with
full force.

His dated implementation timeline is also the best available calibration of what
is hard: by 2011-01-06 character create/delete and world join worked; 2011-01-16
say/shout/whisper/general chat worked but **clan and squad chat did not, because
clans and squads did not exist at all**; 2011-02-12 NPCs could be placed, named and
given appearance, with vendors and missions "working partially"; 2011-02-24 Sprint
and weapon firing worked but **damage did not, and no creatures existed to shoot**.
His Release #2 (2011-01-20) could "Join the bootcamp map" with *"a not-working
footlocker in the start area"*, no account creation and no remote connections.
**Squads, clans, control points and instances were never solved publicly by any
project found in this sweep.**

This also bounds how much oral knowledge existed: a 2011 QuarterToThree thread
quotes the InfiniteRasa site's own statistics at **"Total members 614"** (Oct
2011), and a 2018 forum post records *"I know we had a server up at one point where
you could connect in and run around. **Enemies and stuff didn't work**."*

### 7.12 Boot camp — what exists, and the pre-rebuild limit on all of it

An **official-host, player-authored** boot-camp walkthrough was recovered:
`playtr.com/community/player_guides/recruit_guide_to_boot_camp.html` (captures
`20080103074214` and `20071119131336`, both 11,562 B, identical size). It is
attributed *"Author: Bloodwolfe, Courtesy of: MyTabulaRasa, **Most Valuable
Walkthrough Contest Winner**"* — i.e. **community text published on an NCsoft host**,
so its tier is `observed`, not `original`, despite the hosting.

Mission sequence as written, with Ellatha database ids in brackets:

| # | Mission | Giver | Mechanics stated |
| --- | --- | --- | --- |
| 1 | Basic Training 101 [1] | **Commander Elvers** (yellow walkie icon; a *second* walkie on the minimap is the bypass officer) | basic commands and movement with voiceover; action bars: left = weapons/tools (keys 1–5), right = skills/items (keys 6–0, shift+arrow configs) |
| 2 | Obstruction Destruction [2] | Commander Elvers | timed detonator on the crashed dropship; `'C'` kneel gives a damage bonus; **`'F'` critical kill on red-skull enemies — *"This can sometimes give you a temporary exp modifier. The more Banes you kill in a certain time period the greater the modifier will be"***; loot triangle / auto-loot by running over a corpse; reward = rifle |
| 3 | Carpe Diem [3] | Commander Elvers | **3 detonators**: turret, turret, forcefield; *"Bane can go through red fields, but you can't"*; named boss has a white glow; **capture the base by activating the central obelisk with `'T'`**; fields then turn blue — *"Blue fields you can pass through, but Bane can't"*; reward = shotgun |
| 4 | A Tale of Elements [5] | **Specialist Vance** | first Logos (**Power**) at the Logos stand in the cave; ***"With 'Power' added to your Logos you can use your 'lightning' ability"***; Eloh hologram; **The Collector "is immune to everything but lightning"**; named Bane **The Dissector** |
| 5 | The Last Stand [6] | Commander Elvers | Bane spawn upper *and* lower outside the west forcefield, a named one among them |
| 6 | Moving Up to the Big League [7] | **Evac Pilot Constant** → **Major Bonham** (Wilderness) | dropship platform to Wilderness; *"you'll be level 4 or very close to it"* |
| — | Bootcamp Bypass [4] | the second walkie officer | mentioned in Mission 1's text |

Reward item stats from the fan mission database (`observed`, verbatim): M1 →
**Teleract Motor Assist Armor Gloves**, `Body Armor: 43`, `Regen Rate: 8% per sec`,
`Condition: 100%`, `[5] Regen Armor: +50%`, `[5] Total Armor: +10%`,
`Requirements: Novice Training: Motor Assist Body Armor`, `Min Level: 1`. M2 →
**Standard Grade Cartridges x200** and **AccuMax Rifle** (`Primary`, `71 Physical
Damage`, `68 Physical Melee Damage`, `Optimal Range 60m`, `Standard Grade
Cartridges 0/20`, `Condition: 100%`, `[2] Crit Hit Chance: +2%`). M3 → **Shinobi
Shotgun** (`Primary`, `116 Physical Damage`, `65 Physical Melee Damage`, `Cone Range
20m`, `Standard Grade Cartridges 0/45`, `3 Ammo Per Shot`, `[5] Armor Piercing:
+10%`). M4 (Bootcamp Bypass) → shotgun + rifle. M5 → Teleract and Titan Motor Assist
Armor Legs. M6 and M7 list none. The bracketed number in `[5] Regen Armor: +50%` is
the **armour-piece count** the bonus applies at, corroborated by the French
*«Ce bonus s'applique en fonction du nombre de pièces d'armure portées de ce type»*
and the official *"per piece of armor worn"*.

**The pre-rebuild limit — this applies to all of the above.** Two independent sources
establish that this material predates the D11 rebuild:

- TaRapedia's `Bootcamp` page has its last pre-shutdown revision at **2008-08-29**
  and carries the editors' own banner: `{{old|Bootcamp has been changed as per one of
  the previous updates. Everything in here needs to be updated.}}` There are **no**
  post-shutdown revisions either.
- The official-host guide was captured **2007-11-19** and **2008-01-03**, both before
  D11 went live on **2008-08-15**.

`AGENTS.md` forbids using pre-rebuild content as an analogue for rebuilt content. So
this guide and the TaRapedia `Bootcamp*` pages are **usable for the pre-rebuild boot
camp only** and must be labelled as such. **They are not a substitute for the
final-live boot camp**, which remains an evidence gap.

What they do give that is likely rebuild-independent: the mission names and sequence
skeleton, `Commander Elvers`, the bypass officer, the dropship exit, and the level-up
point awards (3 attribute + 2 skill), the last independently corroborated by
TaRapedia's `Level` page. Extracted TaRapedia boot-camp pages, each with its revision
date in the file header: `Bootcamp__20080829T062419Z`,
`Boot_Camp__20071217T234156Z` (redirect), `Bootcamp_mission_list__20080828T095352Z`,
`Bootcamp_Bypass__20080228T172615Z`,
`Destination_Outpost_mission_list__20080828T095357Z`,
`Denzil_s_Caldera_mission_list__20080828T095354Z`. From `Bootcamp`: the zone is an
**instance**, Planet = Foreas, Continent = Concordia; areas = **Destination Outpost
(center), Denzil's Caldera (west), Luna Cavern (east)**; *"The only way to leave the
Bootcamp is via dropship. To board a dropship, talk to **Evac Pilot Constant** as part
of the mission **Moving Up to the Big League** … one-way exit."* From `Denzil's
Caldera mission list` (rev 2007-07-27), an editor note: *"'Take it to the Trenches' is
now called 'Basic Training 101'"* — evidence of a mission rename during beta.

The strongest route for the rebuilt boot camp is §9.1: `CVOGSpawnPoint`,
`CVOGEnterPoint` and `CVOGOutPost` records inside `adv_bootcamp.map`, which would be
`original`-tier rather than `observed` or `analogue`.

### 7.13 Corroboration between an official walkthrough and a fan database

`rgtr.com/game_intel/official_guides/the_wilderness_walkthrough.html` (captured
2007-12-13, 6,237 B) is an official mission-chain walkthrough whose mission names
match the fan database **exactly**: `Moving Up to the Big League` (id 7), `Bootcamp
Bypass` (4), `Too Close for Comfort` (8), `Receptive Reception` (9), `Forming
Alliances` (10), `Wilderness Targets of Opportunity` (91), `Traitors to the Cause`
(49), `Machinations` (50), `Smuggler's Blue` (48, labelled **"Ethical Parable"**),
`The Walking Wounded` (69). An official publisher page and a fan database
independently agreeing on names, ordering, NPC givers and zone routing across a whole
chain is the strongest cross-source corroboration in the sweep.

Mechanics it states at `original` tier: the **waypoint rule** — register at the
*"glowing blue pillar of light"*, and *"You can't go anywhere through this teleporter
yet, as you must have at least two Waypoints"*; the **level-5 trainer gate** (§7.8);
**Logos persistence through cloning** (§7.8); the **control-point** rule (§7.6);
**damage types** — *"EM weaponry does the most damage to machines"*; and **ethical
parables have consequences** — Smuggler's Blues offers betray/warn choices and *"your
decision here will impact how the other AFS officers and enlisted respond to you in
future encounters"*, which corroborates the RU-Wikipedia parables claim though not
its 20% figure. NPC names preserved: Major Bonham, Commander Rogers, Lt. Col. Cimoch,
Council Elders Moawi & Solis, Apirka, Logos Mentor Ensine, Outpost Commander Taylor,
Elder Baruhi, Lt. Cmdr. Parsons, Private Moore, Matthew Corman, Medic Quincy Corman.

### 7.14 The upstream game server never functioned — and why that settles a provenance question

Eight independent, dated statements bound what any ancestor of this repository ever
achieved:

| Date | Source | Statement |
| --- | --- | --- |
| 2011-02-24 | J.H.Work devlog | weapons fire but **no damage**, **no creatures exist** |
| 2016-07-15 | `infiniterasa.org` f=15 t=8, `Damuras` (Site Admin) | *"**Currently there are no public test servers to play on**"* |
| 2016-07-24 | t=54, `Damuras` | *"This project is at a **current standstill**… There are a few working systems"* |
| 2016-07-24 | t=55, `Damuras` | Roadmap = the single word *"**Placeholder**"* |
| **2016-11-02** | **f=15 t=168, `Blumster` (rank Lead Developer)** | *"**Rasa.Game is the game server. It isn't functioning yet.** Rasa.Auth is the authentication server. That does function, but I did not finish the DB structure yet."* |
| 2018-05-03 | t=57, `Marmbo` | *"we had a server up at one point where you could connect in and **run around**. **Enemies and stuff didn't work**"* |
| 2016 (comment) | `r/TabulaRasa` `56w00h`, `dbouya` | *"(I've been sitting alone in `#infiniterasa` for years, **the original chatroom for the C++ project that died**)"* |
| 2021-04-06 | MassivelyOP (Chris Neal) | *"**a ways off** from being fully playable… **mostly non-gameplay things** available"* |
| 2022-07-25 | MMO Folklorist | *"progress is slow and **no public test server is available as of yet**"* |

`Blumster` is the same handle whose `DataLoader`/`ChunkReader`/`GLMUtility`-class
toolkit is the sweep's most valuable technical find (§9), and the same person credited
in t=168 with bringing `Rasa.Game` to .NET Core.

**Consequence, and this is the most important structural conclusion of the sweep.**
The upstream `Rasa.NET` **game server never functioned**. Therefore the gameplay-side
datasets this repository now holds — missions, creatures and spawnpools, XP
thresholds, skill prerequisites and grant rules, trainer gates, AI, control points,
squads, cloning, travel, loot, vendor tables — **cannot have been inherited from
InfiniteRasa as retail data.** They were necessarily authored *after* the fork, by
this project, from client data or from reconstruction. Two things follow:
`AGENTS.md`'s evidence-gap discipline applies to every one of them, and **no amount
of further forum searching will recover them from the private-server community,
because the community never had them.** The only route is the 1.16.5.0 client, which
the project already holds (§2).

### 7.15 A precisely-evidenced loss: the 2013–14 "Documentation" forum

The 2014-12-05 captures of the `infiniterasa.org` forum indexes (17 forums, f=3…21,
all HTTP 200; parsed to `forums/notes/infiniterasa_org_2014_forum_census.txt`) yield
a complete **topic-title census** of an era not previously known to exist. Forum
**f=15 was "Documentation"**, and its topic titles map almost one-for-one onto this
project's documented gaps:

- f=15 t=19 **"KTB: NavMesh generator and visualizer for pathfinding"** → AI
- f=15 t=18 **"KTB: About Items"** → item tables
- f=15 t=22 **"Parker: Basic Character Class"** → classes
- f=15 t=21 "Parker: Orders of completion"; t=20 **"Parker: Weapon Generator"** → loot/weapon data
- f=15 t=45 **"Admin/GM Commands?"** → command semantics
- f=13 t=43 **"Universal Login Library"** → auth protocol; f=13 t=42 "Auth server ported to linux"; f=13 t=12 "Milestones [WIP]"
- f=9 t=23 **"Database"**; f=14 t=14 "Setting up your client"; f=14 t=33 "Guide to install and run the Infinite Rasa server"
- f=18 t=11 **"IRC Server Info"**; f=18 t=8 "Resurrection of Richard's Garriott Tabula Rasa Petition"
- f=5 t=29 *"Buy your Tabula Rasa client before it gets 'vintage'"*

Two named contributors appear as title prefixes: **`KTB`** and **`Parker`**.

**None of it is archived.** CDX on `infiniterasa.org/viewtopic.php` returns **335
rows**, well under the 2,000 limit and spanning 2014-11-23 → 2024-01-18, so the
result is not truncated. Only **30 distinct topic ids** have any capture at all
(`1 3 4 5 6 7 8 54 55 56 57 59 60 61 62 63 119 129 136 149 166 167 168 172 173 174
175 184 185 186`), and for every 2013–14 Documentation or General-Discussion topic
the row count is **zero** (`t=12, 14, 18, 19, 20, 21, 22, 23, 30, 31, 33, 43, 45`
all → 0). The pattern is clear: **Wayback crawled the forum index pages on
2014-12-05 but did not follow the links into the topics.** The entire range
**t=9…53 is unarchived**, while t≥54 (2016 onward) is well covered.

So the most technically productive period of Infinite Rasa — when it had a real
Documentation forum, a NavMesh tool, item/class/weapon write-ups and an auth library
— is precisely the period the Wayback Machine missed. Record this as a genuine,
precisely-evidenced loss, **not** as "the project documented nothing".

**The strongest residual route is IRC.** `dbouya`'s 2016 comment (§7.14) independently
corroborates f=18 t=11 "IRC Server Info": a channel **`#infiniterasa`** existed and
was *"the original chatroom for the C++ project"*. IRC is where day-to-day technical
discussion happened in 2013–14, so **if anyone kept logs, that is the single most
promising remaining source** for the NavMesh, item, class, weapon and auth-library
material. The channel's *existence* is class (b), two independent sources; any claim
about its contents is class (d) until logs surface. Secondary routes: the named
authors **`KTB`** and **`Parker`**; GitHub commit messages and issues on
`InfiniteRasa/Game-Server` (2011-08-02 → 2016-08-18) and `Authentication-Server`
(→ 2019-02-02), which span the period; and the "Resurrection… Petition" topic, since
petitions sometimes survive on Change.org with signatory lists.

The **2016+** Documentation forum *is* archived and was recovered: f=15 t=129 (Core
Installation), t=61 (Game Server), t=62 (Let's Play), t=8 (Client Setup), t=168 (Run
Rasa.NET on Linux), t=57 (Requirements), f=12 t=167 (Auth Server, queued). From t=8,
`Damuras` gives the client acquisition route — *"**The demo which was made freely
available** and **a patch tool create [sic] by the team**"* — linking
`infiniterasa.org/tools/tabularasa_demo.exe` and `…/tabularasa_patcher.exe`, and the
launch switches `/NoPatch /AuthServer=IP:Port`, **verbatim the incantation this
repository documents at `docs/setup.md:56` and `docs/docker_setup.md:46`**. A
**team-written client patcher implies the project held a patch manifest / file list**
for the retail client — exactly the artifact that records per-file versions and
hashes, and directly relevant to the final-revision question (§12.4). Whether either
binary was archived is **INDETERMINATE** (the CDX query for `infiniterasa.org/tools`
hit `code=000` and is on the retry queue).

### 7.16 Chain of custody for the canonical 1.16.5.0 client

`r/TabulaRasa` thread `22i7vy` was recovered through a Wayback capture of
**`old.reddit.com`** (`forums/raw/reddit/r_22i7vy_old.html`, 101,370 B, HTTP 200,
title identity-verified), submitted **2014-04-08**, and its TLDR is the ancestor
project's own deployment recipe:

> "create a **`tr_auth`** database and run the query in **`auth.sql`**; create a
> **`tr_game`** database and run the query in **`ir_gameserver.sql`**; edit
> **`config.ini`** for both servers … and run **`tabula_rasa.exe /NoPatch
> /AuthServer=localhost:2106`** to start the client."

Its download list includes *"**Tabula Rasa 1.16.5.0** (magnet link): **this was the
last version of the game client to be released.** The torrent needs seeds badly."*
The MySQL user is **`infinite` / `rasa`** with role DB Manager — identical to the 2016
guide, confirming continuity of the same setup from 2014 to 2016.

**The magnet resolves.** It is aliased as `http://tinyurl.com/tabulamagnet`, **still
live in 2026**, which 301-redirects to
`magnet:?xt=urn:btih:nxyjdc45rfxsa7tq5vnyfmdnmjy3th2h&dn=Tabula%20Rasa%201.16.5.0.zip&…`.
Base32 → hex gives **`6DF0918B9D896F207E70ED5B82B06D6271B99F47`** — exactly torrent
**T3**, `Tabula Rasa 1.16.5.0.zip`, 3,091,698,303 bytes, created 2013-05-30, and the
display name matches. Chain of custody:

```
2013-05-30  torrent "Tabula Rasa 1.16.5.0.zip" created; TPB id 8521272, uploader "Anonymous"
2014-04-08  an InfiniteRasa developer posts the install guide and calls it "the last
              version of the game client to be released", aliased to tinyurl.com/tabulamagnet
2026-09-13  that alias still resolves; its infohash == 6DF0918B… == TPB 8521272  ✓
```

**Decisive consequence for §8 and §12.4:** the community's *canonical* 1.16.5.0
client is the **`.zip`, `6DF0918B…` (T3)**, endorsed by name and version in the
emulator project's own 2014 install guide. The hash this project previously knew —
**`f6165f01…`, `TabulaRasa1.16.5.0.rar`, 2,644,531,027 B, created 2019-09-07, no
tracker, DHT-harvested — is a different, later, unofficial repack, 447 MB smaller**,
and should not be treated as the reference client without a byte-level comparison.

Two comments on that thread explain where the `.rar` came from. `Zeino`: *"And I have
super fast upload speeds so I hope you all get it fast :)"*. **`edolnx`**: *"**I've
been hosting the old RAR torrent for a while, but just now added the ZIP torrent.**
Should be seeding in a few hours as well."* So one community member seeded **both**
distributions, the `.rar` being the older — a coherent explanation for two
same-version, different-size, different-year hashes, and a named person who may still
hold both.

`18o10mk/private_server_update` is a **genuine Wayback 404** (4,740/4,750 B
"has not archived that URL" pages for both `old.` and `www.` forms) — a real negative,
not a throttle failure.

**Technique that made this work:** fetch the **`old.reddit.com`** URL through
`/web/<ts>id_/`. CDX lists **only `www.reddit.com`** keys (134 rows, zero
`old.reddit.com` rows), and the 2023 `www.` captures are **JS shells** titled
`Reddit - Dive into anything` with no server-rendered content — a first pass using
`www.` URLs produced 12 content-free files. `old.reddit.com` resolves to a
server-rendered capture. Re-fetching the 13 high-value threads that way succeeded for
**12 of 13**. CDX on `reddit.com/r/TabulaRasa` gives 163 rows / **59 distinct thread
ids**, of which the game-relevant ones are now retrieved; `8tija6` is *"Tabula Rasa
Release November 2007 Screenshots"* (**launch-era stills**), `2y227h` is *"Patch Notes
- 2/28/2015"* (a link post — the notes themselves were on the forum), and four are
seed requests for the 1.16.5.0 client. **The remaining ~35 of the 59 are the Belgian
TV series *Tabula Rasa* (2017)**, plus a Star Trek Online tribute fleet — so a
subreddit-name search alone is not enough and **every hit must be title-checked**.
All posters on these threads show as `[deleted]`, so Reddit yields no contactable
identities beyond the commenters named: `edolnx`, `Zeino`, `dbouya`,
`thetimethespace`, `EthanWeber`, `MichaelStewart`. Apart from `22i7vy`, **Reddit held
no data, no code and no protocol notes** — it functioned as a signpost to the forum,
the GitHub org and the client torrent, all three of which have now been followed to
the end. `r/TabulaRasa` had **111 readers** and describes itself as *"Infinite Rasa :
a clean slate."*

## 8. Prioritized source table

Type: **official** = published on an NCsoft-owned host, capable of `original`
tier. **community** = player- or fan-authored, `observed`/`inferred` at best even
when NCsoft hosted it. **emulator** = post-shutdown private-server material that
may itself be guessed.

| # | Source | Exact location | Type | Date | Tier | Gap addressed | Value |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | **Decoded original client tables** (369, verified 0 mismatches) | `research/20260913-bootcamp/list-tables/decoded/` | original artifact | client zip mtime 2009-02-09 | **`original`** | missions, logos, creatures, maps, skills, abilities, crafting, tutorial, control-point states | **critical** |
| 2 | **Client disassembly + decompiled source** (996 `.dis`, 47 `.py`) | `research/20260913-bootcamp/client-code/verify/{dis,src,pyo}/` | original artifact | 1.16.5.0 | **`original`** | all 170 client→server argument shapes; client presentation logic | **critical** |
| 3 | **`Blumster_DataLoader`** — WAD/GLM/CHNK decoders + 62-table client XML catalogue | `github/downloads/Blumster_DataLoader/` | community RE | 2015 | `inferred` (format), reads `original` data | mission + objective **reward schema**, loot model, XP/skill-point schema, starter-character config, **spawn points** | **critical** |
| 4 | **Official EU `field_training` guides** (EN/DE/FR), incl. control points (Starr Long) and cloning (Paul Sage) | `international/eu_playtr_raw/`; `archive/wayback/eu.rgtr.com/en/field_training/` | **official** | published 2008-02-01, captured **2009-02-01** | **`original`** | control points, cloning, instances, regions, HUD, test-server access | **critical** |
| 5 | **Official `game_intel` + `afs_intel` documentation** (587 US URLs; 80 EU EN URLs; 38 class slugs) | `footage/raw/wayback/cdx-full-rgtr.com.txt`; `guides/raw/`; `international/eu_playtr_raw/` | **official** | 2007-10 → **2009-01-07** | **`original`** | classes, tiers, trainer gates, armour, weapons, bestiary, maps, Logos dictionary, planetary atlas, PvP | **critical** |
| 6 | **TaRapedia full WikiTeam dump** — 36,512 revisions / 10,778 pages, **33,767 revisions predate shutdown**, 3,450 main-namespace pages extracted | `archive.org/details/wiki-tabularasafandomcom` → `tabularasafandomcom-20220702-wikidump.7z` (sha256 `3888535292f6b9637d8d35cc438e5d890f5cdf41974665c65a159eb12ae18f56`); history index `…-history.xml.7z` (sha256 `b21426bce3b92f215f3acba958675a2aaa609e2113cc2a9d5ac4b000c424d1a2`) | community | dumped 2022-07-02; revisions 2007-04-30 → 2022-05-13 | `observed`/`inferred`, **per dated revision** | **185 per-zone mission-list pages**, 341 skill, 203 mission, 106 class, 104 map, 99 creature, 77 item, 41 pvp, 29 npc, 14 bootcamp, 13 xp, 10 loot pages; **265 `(logos)` pages** | **critical** |
| 7 | **Official game manual, "AFS OFFICIAL FIELD MANUAL"** (scan + OCR) | `archive.org/details/richard-garriotts-tabula-rasa-2007-destination-games-box-art` → `Richard_Garriott's_Tabula_Rasa_2007_Manual.pdf` (326,901,950 B, sha1 `8d72f1ffdbd435e5e7e5a0091cf64566b24c91fc`); **`_djvu.txt` 141,743 B recovered** | **official** | 2007 retail | **`original`** but **launch-era, not final-live** | Soldier Registration p.010, Using Your HUD p.014, Field Duties p.020, **Career Paths and Specialized Training p.041**, **Standard Arms and Equipment p.056**, AFS Regulation Armor p.072, Logos p.079, The Bane p.082, Foreas Intel p.092, Arieki Intel p.108 | **critical** |
| 8 | **`TabulaRasaPacketFormat.txt` + `GameServerLoginSequence.txt`** | `upstream-game-server/gameData/` (172 + 176 lines, author "J.H.", 2011) | community RE | 2011-08 | `observed` (byte capture) | framing, `gv`/`dv` prefixes, `flagMask`, channels, `mif` 1–4, `formatPrefix`, XOR checksum; login handshake | **high** |
| 9 | **J.H.Work devlog** (10 posts, 47 comments) | Wayback `20110304171245id_` + `?p={24,32,39,50,56,63,69,78,112,124}`; saved `forums/raw/jhwork/wb_p*.html` | community RE | 2011-01-04 → 2011-02-24 | (b) community RE | protocol RE method; **what is and is not recoverable**; launcher bypass | **high** |
| 10 | **Ellatha mission database — 98 retail Foreas missions** | `https://www.ellatha.com/tr/missionslist.asp` + `missionsview.asp?key=<name>&id=<n>`; parsed to `guides/ellatha-missions{,-structured}.json` | community | content retail; **DB build date unverified** | `observed` | missions (vs our 2 rows), objectives, **reward items with numeric stats**, all 7 boot-camp missions, Bootcamp Bypass | **high — but see §12 provenance caveat** |
| 11 | **JeuxOnLine FR per-class numeric tables** (15 classes) + Logos coordinates | `https://tr.jeuxonline.info/article/4373/aptitudes-competences-*`, `/article/4899/emplacement-pre-requis-logos`; extracts at `guides/jeuxonline-extracts/` | community | tier-4 **2007-10-07**; tiers 1–3 **2008-05-13** | `observed` | **skill prerequisites incl. Logos gating**, per-rank numbers, **Logos X/Y coordinates per zone** | **high** |
| 12 | **Official patch-note series** (US + EU, live + PTS), 40 deployments | `archive/deployment-timeline.tsv`, `patchnote-capture-inventory.tsv`, `archive/wayback/` | **official** | 2006-05 → 2009-02 | **`original`** | dating every content/mechanic change; live vs PTS | **high** |
| 13 | **`crypt_test_arrays.h` known-answer vector** | `upstream-game-server/src/crypt_test_arrays.h` (138 lines: `InputK[0x40]`, `CompareD[0x1018+0x10]`) | community RE | 2011–2016 | validates our implementation | cipher verification at near-zero cost | **high** |
| 14 | **`blowfish`/DES auth documentation** — Google Code issue #1 | `code.google.com/archive/p/tabula-rasa-server-emulator/issues/1`; rendered `browser/pages/20260913T192233Z__code.google.txt`; Wayback `issues/detail?id=1` (11 captures, status 200) | community RE | 2013-06-18 | (b) | login credential block: DES-ECB, key `TEST\0\0\0\0`, 24 encrypted + 6 plaintext bytes | **high** |
| 15 | **`dahrkael` — active TR reverse engineer** | `github.com/Dahrkael` (`TRRM` C# **unlicensed**, `wormhole` BSD-2 created **2026-07-14**); `tr.dahrkael.net/singleplayer/` (live, requires **client 1.16.5.0**); contact `ir@dahrkael.net`, Discord `discord.gg/KZ6dXZd` | community RE | 2015 → **active 2026** | (b) | **the retail client's full command line** — `/NoPatch /NoEULA /AuthServer=host:port /user= /password= /server=` (§2.4); TCP `select()` transport, no UDP (§2.3); auth/login and character-creation field names (§2.5, §2.7); GM command set (§2.10). **Not** a client archive format: `PhysFS-AES` is a WinZip AE-1/AE-2 patch (§3.6) and `libquicknet` has no TR bearing (§3.2). `irsingle` ships **no content data** — its 76-entry map list is a **zero delta** against our 78 | **high — the single highest-value human contact**; `irsingle` itself is **reference-only** (unlicensed, private source) |
| 16 | **`pmckeon_GLMUtility`** (reads *and writes* GLM) + `Blumster_ChunkReader` | `github/downloads/pmckeon_GLMUtility/`, `github/downloads/Blumster_ChunkReader/` | community RE | 2025-01 / 2015 | `inferred` (format) | complete GLM archive layout, zlib members, `CHNKBLXX` magic, 30 chunk fourCCs | **high** |
| 17 | **`boards.playtr.com`** — the official US forum | 1,721 distinct archived URLs; `Board=devs`, `Board=bugs`, `Board=newplayer`, `Board=Specialist`, `Board=Soldier`, `Board=ideas` | **official** | 2007–2009 | **`original`** for developer posts | developer ("blue") statements on mechanics; dated bug reports | **high — but thread coverage is poor**: only ~16 `showflat.php` captures; 653 are `showprofile`, 391 `calendar`, 177 `dosearch` |
| 18 | **`RGTR.ISO`** — US retail disc image | `archive.org/details/richard-garriotts-tabula-rasa-2007-destination-games-box-art` → `RGTR.ISO`, 2,902,261,760 B, sha1 `9532af791856b43f8277f602dbe5c0117c5b7921` | **official** | uploader claim 2007-11-02 | **`original`** (disc) | a second, **US** client source to cross-check 1.16.5.0; retail data tables | **high** — catalogued, not downloaded |
| 19 | **`rgtabularasa`** — Russian retail DVD | `archive.org/details/rgtabularasa` → `RGTREURU.iso` 2,884,829,184 B, 574 files, md5 `fb6107d1…`, sha1 `815b1619…` | **official** | uploader claim 2007-12-10 | **`original`** (disc); uploader metadata is a claim | EU/RU retail client variant. Uploader: *"published in Russia around early 2008, game files are in English only despite installer also having French and German options"* | **high** — catalogued |
| 20 | **Client torrents T1–T4** (metadata recovered; payloads not downloaded) | `forums/torrents/itorrents_*.torrent` + bencode decodes; sha256 in `forums/ARTIFACTS.sha256` | mixed | see below | **`original`** (client binaries); index metadata is a claim | client-version provenance | **medium-high** |
| 21 | **IGN TR wiki** (70 pages) | `https://www.ign.com/wikis/tabula-rasa/*` | community | footer **"Updated Mar 1, 2014"**; content likely retail | `observed` | **`Item_Disassembly_Chart`** (18 armour-mod + 26 weapon-mod → component rows), **`LvL_50_Crafting_Recipes_Locations`** (per control point, Assault vs Defense), `Player_Compiled_Cloning_Tips`, 25 zone `*_Logos`, 15 `*_Skills`, `Mobs` | **high** — content date inferred from verbatim agreement with official rgtr wording, **not independently dated** |
| 22 | **`playtabularasaonline.com`** (25 pages) | `https://www.playtabularasaonline.com/index.php?pg=<page>` | community | update archive **ends 2008-07-03** | `observed` | skills (85 KB), cloning, Logos chart/locations/skill-requirements, weapons and damage types, class tree; **a 27-entry multilingual fansite directory** | **medium-high** — ~8 months stale vs D16.5 |
| 23 | **`soul_of_a_soldier`** official article series | `www.rgtr.com/community/soul_of_a_soldier/*` (**131 URLs**), incl. `advancing_the_ranks.html` | **official** | 2008 | **`original`** | progression, ranks | **high — queued, not retrieved** |
| 24 | **Official mechanics news articles** | `rgtr.com/community/community_news/`: `double_xp_in_squads`, `xp_bonus_clone_credit_and_a_pants_allowance`, `tabula_rasa_tips_for_assaulting_control_points`, `class_changes_online`, `tweaks_to_classes_coming_soon`, `character_builder_site`; `player_guides/your_clone_and_you` | **official** | 2007–2008 | **`original`** | **squad XP multiplier**, clone credit, XP bonus, control-point tactics, class rebalances | **high** |
| 25 | **Official CE maps** (15 PNGs) + zone maps | `www.rgtr.com/media_downloads/images/TR_CEmaps-{1..15}.png` @20081116-17; `media_downloads/maps/{concordia,ligo,torden,valverde}_*` | **official** | 2008-11 | **`original`** | `map_info`, region layout, position measurement | **high** |
| 26 | **`AFS_INTELLIGENCE_PLANETARY_ATLAS.pdf`** | same box-art item, 51,035,646 B, sha1 `45618bf6…`; `_djvu.txt` 5,343 B recovered | **official** | 2007 retail | **`original`** | maps, zone layout, place names — OCR is thin (5 KB), so it is **mostly map art** | **high** |
| 27 | **`TabulaRasa-TheFinalStand`** | `archive.org/details/TabulaRasa-TheFinalStand` → `.wmv` 174,199,323 B (213.9 s, 1280×720), `_512kb.mp4` 15,332,388 B, `.ogv` 14,982,427 B | community | uploader claim: night of **28 Feb 2009** | `observed` | **documented final live events** | **high** |
| 28 | **G4TV press archive** (~15 TR items) | `archive.org/details/g4tv.com-video{11733,16141,17584,17736,27468,31492}` | community (press) | 2006–2008 | `observed` | `video17736` "Beta Walkthru Pt. 1" (693 s) with **Garriott on story, tactical combat and character-class growth**; `video27468` filename `tr_tabula_rasa_073008_garriott_full_+interview_HD_flv` (2008-07-30) | **medium-high** |
| 29 | **Raisuly progression series** (22 videos) | `footage/findings.md` §5; ids incl. `Ass7ENzCesk`, `ryufOpnpzRk` | community | recorded 2007-10-27 → **2009-03-01**; uploaded 2024-01/02 | `observed` | dated level-17→50 progression; XP/level-up; class tier choices; final live | **high** |
| 30 | **Daily screenshot series** (~28 items) | `archive.org/details/TabulaRasa--Screenshots{4..30}July2008`, `…August2008` | community | Jul–Aug 2008 | `observed` | dated UI/HUD/creature appearance at a known date | **medium** |
| 31 | **`GDC2006Garriott`** | `archive.org/details/GDC2006Garriott`, 14.4 MB MP3/OGG | **official** (UBM/GDC) | 2006 | `original` but **pre-release** | design intent for combat/progression | **medium** |
| 32 | **Naver game DB entry** (Korean) | `https://game.naver.com/game/ong.nhn?gameNo=8776`; saved `browser/pages/20260913T192909Z__game.naver.com_game_ong.{txt,html}`, sha256 `c9240c37d7b3129310ecdf8c79ecd99822e3f8bfc6b2c678d58045f650927ecc` | press/database | live 2026 | `inferred` | native title, developer, feature set | **medium** |
| 33 | **RU Wikipedia game article** | `ru.wikipedia.org/wiki/Tabula_Rasa_(игра)` | community | live 2026 | `inferred` | economy, dates, mission composition, aim assist | **medium** |
| 34 | **`manualzilla-id-5770806`** | `archive.org/details/manualzilla-id-5770806` → `5770806_djvu.txt` **52,413 B recovered** | community upload | 2021 upload | `observed` | a second, smaller manual text | **medium** |
| 35 | **`upstream-game-server/gameData/`** text files | `logos.txt` (390), `class.txt` (15), `starterItemTemplateClassIds.txt` (95 rows), `equipableClassEquipmentSlot.txt` (6,517), `mapInfo.txt` (14) | emulator | 2011–2016 | (c) | starter gear, logos names, class list | **medium — see §12 on `mapInfo.txt` coordinates** |

Torrent detail for #20, all with **0 seeders** at query time and all catalogued
only (no client started, no payload downloaded):

| # | Internal name | Infohash | Bytes | Created (UTC) | Class |
| --- | --- | --- | ---: | --- | --- |
| T1 | `install_tabula_rasa_1.0.0.0.exe` | `4518A4DA665F8FA547E395CE6999D1B586CEFED5` | 2,866,444,983 | **2007-11-14 05:57:54** (12 days post-launch) | **`a`** original retail installer |
| T2 | `Tabula Rasa.rar` = **"v1.15.7.0"** | `31504EAF008CB5299287A61ABD9A4993EEDE715F` | 2,627,837,437 | **2009-01-13 12:26:24** (~4 weeks pre-shutdown) | **`a`** |
| T3 | `Tabula Rasa 1.16.5.0.zip` | `6DF0918B9D896F207E70ED5B82B06D6271B99F47` | 3,091,698,303 | 2013-05-30 12:19:41 | a/c |
| T4 | `TabulaRasa1.16.5.0.rar` | `f6165f0146327b4398be5193e2f005cb12b73020` (**the project's known hash — metadata now recovered**) | 2,644,531,027 | 2019-09-07 14:33:06 | a/c |

T4 has **no announce URL** and the metainfo comment `dynamic metainfo from client`,
so its metadata was DHT-harvested rather than published by an indexer. **T3 and T4
are not the same payload** despite both claiming 1.16.5.0: different container
(.zip vs .rar), a **447,167,276-byte (~16.9%) size difference**, and a six-year
creation gap. With archive.org's item torrent these are **three mutually distinct
payloads, none substitutable for another** when establishing the final-live build.
T2 is the most time-critical artifact for the client-revision question. T1's
uploader description preserves retail pricing (*"EUR 44,99 ($49.99) for CD-key,
EUR 12,99 ($14.99) as monthly fee … you can apply US time-card only to US version,
and EU time-card only to EU version"*).

**No torrent or Usenet index anywhere in this sweep claims to contain Tabula Rasa
server files, a server source tree, or a database dump.** All four game torrents
are client-only single archives, so no internal file list is visible in the
metainfo.

## 9. The client-format decoding path

Three independent tools agree on the container formats, which makes this the
enabling prerequisite for every map and asset question — including boot-camp spawn
positions.

**GLM archive** (`pmckeon_GLMUtility/GLMUtility/GLM.cs:11-18,54-90`, `FileEntry.cs:1-9`):
last 4 bytes of the file are a `uint32 headerAddress`; the header holds
`nametableAddress`, `nametableSize`, `fileCount`; the name table is ASCII
NUL-separated; each entry is `uint32 offset, uint32 length (compressed), uint32
fileSize (uncompressed), uint32 nametableOffset, uint16 flags, uint32 timestamp`,
where flag bit 0 = "Uncompressed" and bit 1 = "ZLIB". Compression is zlib.
`GLM.cs:246` writes the 8-byte literal `"CHNKBLXX"`.

**CHNK container** (`Blumster_ChunkReader/StoChunkFileReader.cs:24-33`,
`StoChunkFrameReader.cs:59-62`; `Blumster_DataLoader/GLM/Entities/Catalog.cs:30-80`):
`char[4] == "CHNK"`, then `byte[4] opts` with `IsBinary = opts[0]==66 ('B')` and
`IsValid = IsBinary && opts[1]==76 ('L')`. `GLMUtility`'s `"CHNKBLXX"` literal
**resolves ChunkReader's two unexplained option bytes**: the full magic is `CHNK` +
`B`(66) + `L`(76) + `XX`(0x58 0x58). Frame header is `uint32 Name, int32 Size,
uint32 Version, int32 Reserved`. FourCCs decode **big-endian-reversed**
(`ChunkReader/Program.cs:45`), which reconciles `Catalog.cs:46`'s `0x43544C47`
with the string `"CTLG"`.

So the complete access path is: **GLM trailer → header → file table →
zlib-inflate the member → parse CHNK frames → dispatch on fourCC.**

**30 known chunk fourCCs** (`ChunkReader/Program.cs:54-105`), with their glosses:
`AEVT` Anim Events · `ANIM` Anim Master · `BBOX` Bounding Box · `BDAT` Bone Shared
Data · `BIFX` (no gloss) · `BVBX`/`BVCP`/`BVOL`/`BVSP`/`BVWS` Bounding Volume
(Box/Capsule/Sphere/Walkable Surface) · `CPDF` CP Definition · `CPDG` CP Definition
Group · `CPFX` Compile Fx · `CTLG` Catalog · `DECL` Vertex Decl · `EFCT` Effect ·
`EVTB` Event Base · `GBOD` Geometry Body · `GMPH` Geometry Piece Morphed · `GPCE`
Geometry Piece · `GSKN` Geometry Piece Skinned · `INDX` Index Buffer · `ISTR` (no
gloss) · `KERT`/`KESR`/`KEST`/`KEUV`/`KEYF`/`KEYR`/`KEYS`/`KEYT`/`KSRT` keyframe
variants · `LDAA` "LOD Handler **Auto Assault**" · `LDSD` LOD Simple Distance ·
`MWGT` Morph Weight · `PARM` Parameter · `PBON` Phy Bone · `PFXD` Precompiled FX
Data · `PSKE` Phy Skeleton · `TEVT` Animation Track Events · `TRAK` Animation Track
Master · `USDA` User Data · `VERT` Vertex Buffer.

Two caveats. `LDAA`'s gloss names **Auto Assault**, NCsoft's other 2006–2007 MMO
on a related engine, so these chunk formats are **shared across NCsoft titles** —
useful (Auto Assault tooling may decode TR chunks) but it weakens any claim that a
given chunk semantic is TR-specific. And `CPDF`/`CPDG` *plausibly* mean **control
point** definition; that is a guess from a two-word gloss and **must not be treated
as established**.

Most chunks are geometry, animation and skeleton — presentation, not gameplay. The
gameplay-relevant ones are `CTLG`, `USDA`, and possibly `CPDF`/`CPDG`.
`Blumster_TRRM` / `Dahrkael_TRRM` (verified **byte-identical duplicates** by
`diff -rq`; `Dahrkael/TRRM` is the origin, 6 stars, README *"Preservation is key!
That's why ~10 years after the tragedy this program appears"*) implement 14 chunk
readers plus real shader and skeleton parsers, adding `BVAC` and `BVSF`.

**WAD container** (`Blumster_DataLoader/WAD/WADReader.cs:29-37,170-210`), file
`clonebase.wad`: `uint32 version` with `Debug.Assert(version == 27)`, `uint32
objectCount`, then per object `uint32 type` plus a type-specific `CloneBase*`
struct. The `ObjectType` enum is TR's entity taxonomy: `ObjectGraphics=1,
ObjectGraphicsPhysics=3, QuestObject=4, Item=6, Gadget=8, PowerPlant=10, Weapon=12,
Vehicle=14, WheelSet=16, Creature=18, Character=20, Store=22, Bullet=24,
Commodity=26, Armor=28, EnterPoint=30, ExitPoint=32, ContinentObject=34, Town=36,
Encounter=38, CharacterBody=40, CharacterHead=42, CharacterHair=44,
CharacterAccessory=46, Convoy=48, TinkeringKit=50, Accessory=52, SpawnPoint=54,
Trigger=56, Reaction=58, MapModulePlacement=60, MapPath=62, MissionObject=64,
Money=66, Ornament=68, RaceItem=70, Outpost=72`.

**Map version gating** is pervasive and matters for any reimplementation:
`Catalog` loads only when `MapVersion >= 49`, and only from a sidecar
`<mapname>.cat` when `>= 52`; `CVOGSpawnPoint` reads `RandomlyOffsetSpawnPosition`
only when `>= 31` and its 12 `SpawnList`s only when `>= 29`, with
`Debug.Assert(false, "Unreachable code reached!")` on the else branch;
`SectorMap.cs:61-65` asserts `4 <= MapVersion <= 62` with the comment
`// 60 < MapVersion < 62`, reads `IterationVersion` if `>= 27`, `FileName` if
`>= 11`, `NumOfImports` if `>= 45`, weather/region blocks if `>= 47`, and carries
`Debug.Assert(false, "OLD VERSION FORMAT IS NOT IMPLEMENTED! >= 37 && < 47")`.
`SectorMap.cs:104` also **writes `coords.txt`** — the tool was used to dump
coordinates from real maps.

### 9.1 Boot-camp spawn positions are recoverable from client map data

`Blumster_DataLoader/GLM/CVOG/` is a 17-class map-object model:
`CVOGClonedObjectBase, CVOGEnterPoint, CVOGGraphicsBase, CVOGGraphicsPhysicsBase,
CVOGMapPath, CVOGObjectGraphics, CVOGOutPost, CVOGPhysicsBase, CVOGReaction,
CVOGReactionText, CVOGRiverNode, CVOGRoadJunction, CVOGRoadNode, CVOGRoadNodeBase,
CVOGSpawnPoint, CVOGStore, CVOGTrigger`.

`CVOGSpawnPoint` (`GLM/CVOG/CVOGSpawnPoint.cs:12-60`) fields: `ActivationRange:f32`,
`ChampionChance:u8`, `FactionDirty:bool`, `HasChampion:bool`,
`InitialPatrolDistance:f32`, **`Location:Vector4`**, `Loot:i32`, `LootChance:f32`,
`LootPercent:f32`, `MapPathCOID:u64`, `OriginalFaction:u32`, **`Quaternion:Vector4`**,
`Radius:f32`, `RandomlyOffsetSpawnPosition:u8`, `RespawnTime:f32`, `SpawnChance:u8`,
`SpawnLists:List<SpawnList>[12]`, `UseGenerator:bool`, read after
`ReadTriggerEvents(br, mapVersion)`. Line 10 carries a hand-written observation
from a real map: `// stabil pont: -352 == activation range | off: 80 h`.

**Consequence:** boot-camp and zone spawn positions are `CVOGSpawnPoint.Location`
records inside the map's GLM/CHNK containers, with `CVOGEnterPoint` and
`CVOGOutPost` giving entry and outpost placements. That converts a "lost server
data" problem into a **decoding task at `original` provenance** — a far better path
than the `mapInfo.txt` comment coordinates (§12.3), and it is the concrete answer
to the boot-camp half of `docs/evidence/bootcamp-d11-reconstruction-manifest.json`.
The original map file is already on disk:
`research/20260912-new-character/adv_bootcamp.map` (127,212 B), with map decoding
work at `research/20260913-bootcamp/map/` (`decode-map.py`, `format-spec.json`,
`decoded/`, `layout/`) and `pre-d11-map/` (187 files).

### 9.2 The 62-table client XML catalogue

`Blumster_DataLoader/XML/DataHolder.cs:9-70` plus 54 typed structs document the
client's entire XML data-table surface, with the client's own Hungarian-notation
element names preserved — which is what makes this `original`-tier format evidence
rather than one author's invention:

`Results, tAchievement, tArena, tBonusData, tCategory, tColor, tConfigCosts,
tConfigNewCharacters, tConsumables, tContinentExploredAreas, tContinentObject,
tCreatureAI, tCreatureEnhancement, tCreatureExperienceLevel, tDiscipline,
tExperienceLevel, tFactions, tHeadBody, tHeadDetail, tItemTemplate, tLootConfig,
tLootRarity, tLootTable, tLootWeights, tMapScaler, tModule, tNPCContinentObject,
tNPCDialogueRandom, tObjTypeRef, tOutpost, tPrefixCreature, tPrefixWeight,
tQuestBaseCredits, tQuestCreditsLookup, tQuestXPLookup, tRegion, tRegionBorders,
tRegionFactions, tRegionMaps, tRemovedObjects, tStoreInventory, tTile, tTileSet,
tTreasureWeight, tTypeNames, tVehicleTemplate, tVersionConfig, tWeaponGroup,
tWeaponGroup_x, vConsumables, vLootBaseItems, vGeneratableCreatures, vModule_All,
vHeadBody_Character, vWeaponGroup, vWeaponGroup_x, vRaceSpecificItems,
vRandomDialogues, vColorBiomek, vColorHuman, vColorMutant, vDisciplines`

Gap mapping: **loot** → `tLootTable`, `tLootConfig`, `tLootRarity`, `tLootWeights`,
`tTreasureWeight`, `vLootBaseItems`; **XP thresholds** → `tExperienceLevel`
(`IDLevel, intExperience, iSkillPoints, iAttributePoints, iResearchPoints`) and
`tCreatureExperienceLevel`; **mission rewards** → `tQuestXPLookup`
(`IDQuestXPIndex, rlLevelXP`), `tQuestCreditsLookup`, `tQuestBaseCredits`;
**boot camp / new character** → `tConfigNewCharacters`; **vendors** →
`tStoreInventory` (`CBIDStore, CBIDItem, sinQuantity`); **AI** → `tCreatureAI`,
`tCreatureEnhancement`, `tPrefixCreature`, `vGeneratableCreatures`;
**spawns/placement** → `tContinentObject`, `tNPCContinentObject`, `tOutpost`,
`tRegion*`, `tMapScaler`, `tTile`/`tTileSet`, `tRemovedObjects`; **classes** →
`tDiscipline`, `vDisciplines`, `tCategory`; **items/weapons** → `tItemTemplate`,
`tWeaponGroup(_x)`, `tConsumables`, `vRaceSpecificItems`; **NPC dialogue** →
`tNPCDialogueRandom`, `vRandomDialogues`; **appearance** → `tHeadBody`,
`tHeadDetail`, `tColor`, `vColorHuman/Biomek/Mutant`; **client version** →
`tVersionConfig`.

`tLootTable` alone documents **44 named fields** (`XML/LootTable.cs`):
`sinLootRolls`, `rlDropChance`, `rlConsumableDropChance`, `rlDropLevelOffset`,
`sinMaxLevelOffset`, `sinMaxEnhancementComplexity`, `rlLevelOffsetMultiplier`,
`intBaseChanceEnhanced`, `intChanceEnhancedModifierPerLevel`, per-category chances
(`intChanceWeapon/Armor/PowerPlant/WheelSet/Vehicle/Gadget/TinkeringKit/Accessory/RaceItem/Ornament/Other`),
`rlDropCreditsChance`, `intMinCreditsDrop`, `intMaxCreditsDrop`, **nine rarity
chances `intChanceRarity_0..8`**, and eleven per-category broken-item modifiers — a
complete loot-model schema, and the most directly actionable artifact for the loot
gap.

`tConfigNewCharacters` (`XML/ConfigNewCharacter.cs:8-21`) documents **14 fields**
for starting characters: `CBIDArmor, IDClass, IDOptionCode, CBIDPowerPlant, IDRace,
CBIDRaceItem, IDSkillBattleMode1..3, IDStartingSkill1, IDStartingTown, CBIDTrailer,
CBIDVehicle, CBIDWeapon` — the starter-gear and boot-camp-entry schema, keyed per
class/race/option-code.

Only **10 of the 62** tables have SQL mappings implemented in `Blumster_XmlToSql`
(`Achievement, Arena, ContinentExploredArea, ContinentObject, CreatureAI,
Discipline, ExperienceLevel, QuestBaseCredit, QuestCreditsLookup, QuestXPLookup`),
but each gives a ready-made table/column naming precedent — e.g.
`ExperienceLevel.cs:25-38` maps `IDLevel, intExperience, iSkillPoints,
iAttributePoints, iResearchPoints` → table `experience_level` with columns `Level,
Experience, SkillPoints, AttributePoints, ResearchPoints`. That is the same concept
as our `player_exp_for_level` but **carrying skill, attribute and research points
per level, which our table does not**.

## 10. Mission and skill structure — a schema correction

`Blumster_DataLoader/WAD/Quests/Quest.cs:48-92` and `QuestObjective.cs:31-59` give
byte-level field order with explicit padding skips. This is the **only
specification of TR mission and per-objective reward structure found anywhere**,
and it contains a structural correction.

`Quest`: `Id:i32`, `Name:unicode[65]`, `Type:u8`, skip 1, `NPC:i32`, `Priority:i32`,
`ReqRace:i16`, `ReqClass:i16`, `ReqLevelMin:i32`, `ReqLevelMax:i32`,
`ReqMissionId:i32[4]`, `IsRepeatable:i16`, skip 2, `Item:i32[4]`,
`ItemTemplate:i32[4]`, `ItemValue:f32[4]`, `ItemIsKit:i16[4]`, `ItemQuantity:i32[4]`,
`AutoAssing:i16` *(sic — typo in the original)*, `ActiveObjectiveOverride:i16`,
`Continent:i32`, `Achievement:i32`, `Discipline:i32`, `DisciplineValue:i32`,
`RewardDiscipline:i32`, `RewardDisciplineValue:i32`,
`RewardUnassignedDisciplinePoints:i32`, `RequirementEventId:i32`, `TargetLevel:i16`,
skip 2, `RequirementsOred:i32`, `RequirementsNegative:i32`, `Region:i32`,
`Pocket:i32`, `NumberOfObjectives:u8`, skip 7, then `i32 count` + `count ×
QuestObjective`.

`QuestObjective`: `QuestId:i32`, `ObjectiveId:i32`, `Sequence:u8`, skip 1,
`ObjectiveName:unicode[65]`, `MapName:unicode[65]`, skip 2, `WorldPosition:i32`,
`ContinentObject:i32`, `LayerIndex:u8`, skip 3, **`XP:i32`, `Credits:i32`,
`AttribPoints:i32`, `SkillPoints:i32`**, `ReturnToNPC:i32`, **`XPIndex:i16`,
`CreditsIndex:i16`, `XPScaler:f32`, `XPBalanceScaler:f32`, `CreditScaler:f32`**.

**The structural finding:** that `XPIndex`/`CreditsIndex` + `XPScaler`/`XPBalanceScaler`/
`CreditScaler` quintet means per-objective rewards are an **index into
`tQuestXPLookup` / `tQuestCreditsLookup`, scaled by level** — not absolute values.
That is why `tQuestXPLookup` is `(IDQuestXPIndex, rlLevelXP)` and
`tQuestCreditsLookup` is `(IDQuestCreditsIndex, rlLevelCredits)`. **Any
reconstruction of mission rewards that stores flat XP or credit numbers is
modelling the wrong thing.** This bears directly on `docs/mission-research.md` and
on the empty `npc_mission_reward` table.

`Skill` (`WAD/Skills/Skill.cs:48-90`) answers the prerequisite gap:
`Id, Class, Race, TargetType, TargetSubType, TargetObjectType, AffectedTarget,
AffectedSubType, AffectedObjectType, StatusEffect` (all i32),
**`SkillPrerequisite1/2/3` (i32 ×3)**, `LocationTree:u8`, `LocationLine:u8`,
**`MinimumLevel:u8`**, `SkillType:u8`, `Name:unicode[33]`,
`Description:unicode[1025]`, `XMLName:unicode[65]`, skip 2, `IsChain:i32`,
`IsSpray:i32`, `OptionalAction:u8`, **`MaxSkillLevel:u8`**, skip 2,
`UseBodyForArc:i32`, `GroupId:i32`, `CategoryId:i32`, `SummonedCreatureId:i32`,
`SkillOptional1..4:i32`, `NumOfElements:i16`, skip 2, then `NumOfElements ×
SkillElement`. `SkillElement` (`SkillElement.cs:18-25`) is `SkillId:i32`,
`ElementType:i32`, `EquationType:u8`, skip 3, **`ValueBase:f32`,
`ValuePerLevel:f32`**.

So skill scaling is `ValueBase + ValuePerLevel × level` per element with an
`EquationType` selector, and prerequisites plus the 5/15/30 gates are expressed by
`SkillPrerequisite1..3`, `MinimumLevel`, `LocationTree`/`LocationLine` and
`GroupId`/`CategoryId`. Cross-check against the decoded `skilldata.pyo`
(`skillData` 73, `skillLevel` 359, `skillCharacter` 73) and `abilitydata.pyo`
(`abilities` 1,084, **`skillRequirements` 53**).

### 10.1 Fan-source numeric tables worth cross-checking against the client

From JeuxOnLine FR (`observed`, Recruit page 2008-05-13). Rank names across all
skills: **1 Novice · 2 Initié · 3 Expert · 4 Maître · 5 Élite** — independently
corroborated by the Ellatha reward text `Requirements: Novice Training: Firearms`,
a source with no relationship to JeuxOnLine.

| Skill | R1 | R2 | R3 | R4 | R5 |
| --- | --- | --- | --- | --- | --- |
| Sprint — adrenaline cost | 1.5%/s | 1.35%/s | 1.25%/s | 1%/s | 0.9%/s |
| Sprint — speed bonus | +20% | +30% | +40% | +50% | +60% |
| Firearms — damage bonus | 0% | 10% | 20% | 30% | 40% |
| Firearms — pistol reload | — | 1.3 s | 1.0 s | 0.8 s | — |
| Firearms — rifle crit | — | +3~5% | +5~10% | +7~15% | — |
| Firearms — shotgun knockback | — | +15% | +20% | +25% | — |
| Motor Assist Armor — move speed **per piece equipped** | +1% | +2% | +3% | +4% | +5% |
| Hand to Hand — damage bonus | 0% | 10% | 20% | 30% | 40% |
| Hand to Hand — knockback chance | none | — | 25% | 50% | 75% |
| Hand to Hand — stun duration | none | — | — | 1 s | 2 s |
| Lightning — power cost | 25 | 50 | 75 | 100 | 150 |
| Lightning — arc length | 0 | 12 m | 18 m | 24 m | 30 m |

Lightning fixed values: activation 1.2 s, recharge 1.2 s, range 60 m, electric
damage «Extrêmes», sonic «Aucun»→«Elevés». Rank names: Décharge / Arc / Éclair
foudroyant / Champ / Orage électrique. "Per piece of armor worn" is independently
corroborated by the official career planner's Recruit row — *"Advanced training
increases movement speed per piece of armor worn"* (`original`).

**Skill prerequisite gating, three independent sources:** Lightning (`Foudre`)
requires the Logos **`Énergie` (Power)**; Sprint, Firearms, Motor Assist,
Hand-to-Hand and all four Engineering skills require **`Aucun` (none)**
(JeuxOnLine, 2008-05-13). The official-host boot-camp guide (2007-11-19) states
*"With 'Power' added to your Logos you can use your 'lightning' ability"*. IGN's
FAQ says *"you start with Lightning, but can't use it — check the requirements
listed."* This corroborates `docs/lightning-client-evidence.md` from retail-era
sources. The Logos `Ici` (Here) has prerequisites **Intellect + Énergie**.

**Unresolved conflict, recorded:** Engineering-discipline availability. JeuxOnLine
(2008-05-13) and IGN both list **all four** disciplines (Chemistry, Genetics,
Photonics, Thermodynamics) under **Recruit** with no Logos requirement, while the
official **German** class pages (2009-01-06/07) assign them branch-wise — Soldier
branch → `Photonik, Chemie`; Specialist branch → `Thermodynamik, Genetik`; Recruit
→ `Replikation`. Either an "available vs displayed" distinction or a change
between 2008-05 and 2009-01. **The client tables are the tiebreaker.**

**Attribute points per level, by race** (TaRapedia `Level`, rev 2008-09-14,
`observed`): Human +2/+2/+2 (Body/Mind/Spirit), Forean Hybrid +1/+3/+2, Brann
Hybrid +1/+1/+4, Thrax Hybrid +3/+1/+2; **+3 attribute points and +2 training
points per level**; **tiers at levels 5, 15 and 30**, each granting a choice of two
career paths **plus 2 extra training points**. Independently corroborated by the
official-host boot-camp guide: *"You get **3 points to spend on attributes and 2 to
spend on skills**. Press 'P' … press 'K' … You don't have to spend your points you
can save them."*

**XP model** (TaRapedia `Experience`, rev 2008-10-06, `observed`): XP from kills and
missions; bonus for several kills in short succession; monster XP = level base × a
hidden per-type multiplier; full XP at or below monster level, decaying
exponentially above, **zero at ≥10 levels above**; **base XP at level 50 = 2000**;
Warnet multiplier 1, Altered Xanx multiplier 2; **overkill + finishing blow =
double XP**, logged as "Crit Kill"; **squad XP = (base experience) / (squad
members) × (squad multiplier)**. **Squad** (rev 2008-11-10): max **6** members,
`/invite player_name`, leader can Kick, Promote, **Set Loot Mode**, **Set Loot
Threshold**. **Loot** (rev 2007-12-19, a `{{stub}}`): `T` loots selectively,
walk-over auto-loots; a full backpack blocks unique items but ammo and med packs
still stack; **looting a `Machina Control Chip` from a Machina disables its
self-resurrect**.

**TaRapedia currency caveat:** only **308 of 3,450 pages (9%)** were touched in the
final four months before shutdown (last pre-shutdown revision by month: 2007-12
**686**, 2008-01 410, 2008-09 **566**, 2008-10 244, 2008-11 78, 2008-12 **24**,
2009-01 86, 2009-02 **119**). So it is strong for 2007–2008 content and **weak for
late-2008/2009 changes**. Every extracted page carries its own revision date;
treat 2007-dated pages as describing an earlier build. Latest single revision found:
`Charons_Crossing_mission_list` @ **2009-02-27T04:02:36Z — one day before shutdown.**

TaRapedia also has an `Updates/` and `News/` namespace (83 pages) transcribing
official patch notes **with the live/test split already in the page titles** and
**the exact official source URL cited in the body** — e.g. `Updates/2007-12-05
(test)`, `Updates/2008-03-07 (test)` vs `Updates/2008-02-26`,
`Updates/2008-02-29 Son of Patch Notes`, `Updates/2008-12-19`. `Updates/2007-12-11
v 1.3.2.2` carries a client version string (a data point in a series, not the
final answer). Its content yields explicit same-day **EU-vs-US divergence** (a
Europe-only hotfix for repair cost, versus US-only "all items … now at 100%
condition" plus adjusted Green/Blue/Purple repair prices) and establishes **item
rarity tiers Green/Blue/Purple**, item condition 100 vs 20, and repair-cost
economy.

## 11. Non-English sources

Original-language text is preserved verbatim beside its translation. The original
is the citable artifact; the translation is a reading of it.

### 11.1 Korean — Naver game database

`https://game.naver.com/game/ong.nhn?gameNo=8776`, retrieved 2026-09-13 19:29 UTC,
1,758 B rendered text, sha256
`c9240c37d7b3129310ecdf8c79ecd99822e3f8bfc6b2c678d58045f650927ecc`. Tier
`inferred` (press/database summary credited to IT매일).

| Field | Original | Reading |
| --- | --- | --- |
| Title | `타뷸라 라사` | fixes the **native Korean title** |
| Genre | `MMORPG` | MMORPG |
| Rating | `등급: 심의결과 없음` | "rating: **no rating-review result**" |
| Maker | `제작사: 엔씨소프트(주)` | "developer: **NCsoft Co., Ltd.**" |

> "근 미래를 배경으로, 우주를 지배하고 있는 외계의 적군을 상대로 인류를 구하기 위해
> 싸운다는 내용의 MMORPG다. 다양한 캐릭터 클래스를 체험할 수 있는 **캐릭터 복제
> 시스템**, 다이나믹한 전투 액션을 경험할 수 있는 **배틀 필드**, **원격 이동 시스템**
> 등으로 색다른 재미를 준다. 다양한 난이도의 미션과 박진감 넘치는 최전방 전투의
> 묘미도 느낄 수 있다. (정보제공:IT 매일)"
>
> "An MMORPG set in the near future, about fighting to save humanity against an alien
> enemy force that dominates the universe. It offers novel fun through a **character
> cloning system** that lets you experience various character classes, **battlefields**
> where you can experience dynamic combat action, and a **remote movement [teleport]
> system**. You can also feel the thrill of missions of various difficulties and
> exciting frontline combat. (Information provided by: IT Maeil)"

It names three documented gaps as headline features — **cloning**
(`character_cloning_system`), **battlefield/control-point** play, and **teleport
travel**. The `ong.nhn` path and the absent rating suggest Naver lists it as a
closed title; record that as the page's own framing, not as a fact about the
shutdown.

Korean press corroboration (`gametoc.co.kr/news/articleView.html?idxno=56784`,
2020-12-02 retrospective, `press`/secondary):

> «'타뷸라 라사'는 2007년 11월 2일에 정식 출시되었다 … '타뷸라 라사'는 2009년 2월
> 28일에 서비스가 종료되어 … 분기당 18억 정도의 매출»
>
> "Tabula Rasa was officially released on 2 Nov 2007 … service ended on 28 Feb 2009 …
> revenue of about 1.8 billion [KRW] per quarter."

Also records Garriott's suit at 471,335 shares @ 32,130 KRW. Launch was delayed
from 2007-10-19. **Korean and RU press independently agree with the US/EU closure
date of 2009-02-28**, so that date now has two non-English corroborations.

Korean negatives, all verified: `ko.wikipedia.org/wiki/타뷸라_라사` is the
**epistemology concept** (Locke/Aristotle/Avicenna), not the game — there is no
Korean game article. `plaync.co.kr` and `ncsoft.co.kr` CDX greps for `tabula|rasa`
return empty on HTTP 200, so **no archived Korean official site exists**. `namu.wiki`
has a game section («리처드 개리엇의 게임») but returns **403** to curl and
**Cloudflare "Just a moment…"** in a real browser. Naver and Daum web search work
but return only Korean games press or Lost Ark / Path of Exile noise. **No Korean
private-server, reverse-engineering or protocol community exists** — five queries
each on Naver and Daum produced only press coverage. One original-era datapoint did
surface: RaGEZONE thread 209535 (2007-01-12) says TR **closed-beta sign-up ran
through PlayNC**, so a Korean beta existed even though no Korean technical community
followed. Whether a distinct KR shard or client existed is **unverified**.

### 11.2 Russian — Wikipedia game article

`ru.wikipedia.org/wiki/Tabula_Rasa_(игра)` (recovered as `ru_game.html` /
`ru_game.txt`). Tier `inferred` — community text, live 2026.

| Gap | Claim | Original | Translation |
| --- | --- | --- | --- |
| Mission composition | ethical parables ≈ **20%** of all missions | «Эти так называемые „этические притчи" составляют около 20 % всех миссий.» | "These so-called 'ethical parables' make up about 20% of all missions." |
| Economy | EU pack **€4.99**, US pack **$4.99**, full version **$49.99** | «Европейский пак продавался за €4.99 и американский — за $4.99 с возможностью приобрести полную официальную версию игры за $49.99.» | as translated |
| Final events | free server access from **2009-01-10**; closure **2009-02-28**; bonuses for subscribers active **2008-11-21** | «С 10 января 2009 года доступ на игровые серверы стал бесплатным, а 28 февраля 2009 года игра была официально закрыта. Всем активным на 21 ноября 2008 г. подписчикам были предложены специальные бонусы.» | as translated |
| Control points | some missions require attacking and capturing Bane-held points | «Другие требуют доступ к некоторым контрольным пунктам, которые могут быть под контролем Пагубы, что требует атаки и захвата контрольного пункта.» | as translated |
| Combat / aim assist | aim-assist configurable; **shotguns have none**; damage multiplied by a random factor | «…сила атаки множится на случайно сгенерированное число … Некоторое оружие, к примеру ружьё, не имеет привязку прицеливания.» | as translated |
| Beta timeline | invites 2007-01-05; beta start 2007-05-02; extra invites 2007-08-08 (FilePlanet/Eurogamer); NDA lifted 2007-09-05; beta end 2007-10-26 (kill-General-British event) | «NCSoft начала раздавать приглашение … 5 января 2007, который начался 2 мая … тест был окончен 26 октября…» | as translated |
| **Client version — CONFLICT** | «Последняя версия **1.12.5.0** (18 сентября, 2008)» | as quoted | "Latest version 1.12.5.0 (18 September 2008)" |

The version claim **conflicts with 1.16.5.0** and is both lower and dated ~5 months
pre-shutdown, consistent with an infobox last edited in September 2008 and never
updated. **It does not displace 1.16.5.0**; record as a conflicting RU-locale claim.
The 20% parables and shotgun aim-assist claims are **not corroborated** by any guide
found; the shotgun claim is *consistent* with the French Firearms table (which gives
shotgun bonuses as knockback chance with no aim-assist row) but that does not prove
it. The closure date agrees with `docs/final-retail-target.md`, and the price points
agree with a fansite's 2007-12-16 entry (*"Other stores have it at $49.99 for the
regular version, but Amazon.com has the Collector's Edition for $29.99"*).

The article's references yielded four RU community/press leads: **`tabularasa.goha.ru`**
(**NXDOMAIN** — Wayback only), `news.goha.ru/c/archive/item/0/242941.html` and
`/243228`, **`ag.ru/games/tabula_rasa`** (403), **`lki.ru/games.php?Game=TabulaRasa`**
(LKI print magazine, data-rich; connection failure), **`igromania.ru/articles/55552/Igraem_Tabula_Rasa.htm`**.
The publisher Новый Диск was not confirmed from a primary page because Yandex is
captcha-blocked; the known RU retail DVD on archive.org remains the only publisher
artifact. `nnmclub.to` search returned 6 "tabula rasa" hits, **all music** — no
Russian game or server torrent.

### 11.3 French

JeuxOnLine `tr.jeuxonline.info` is the richest fan numeric source found in any
language: 98 articles harvested (97 fetched OK), including per-class 5-rank numeric
tables for all 15 classes with **`Logos requis` per ability**, and
`/article/4899/emplacement-pre-requis-logos` (2008-05-13 23:46, 18 comments) giving
**every Logos per zone with exact X/Y coordinates** obtained via `/loc`, plus
Logos→Logos prerequisites and instance Logos, across 13 zones in
Concordia/Torden/Valverde/Ligo. That is directly relevant to the `logos`
**placement** half of the gap (§6). Provenance: **retail** — the section ran
live-service news through «Fin de transmission» and «27 fév. 09 — Dernier baroud
d'honneur» and was then closed. Note the tier-4 class tables are dated
**2007-10-07, three weeks before launch** — treat those as launch-era, not
final-live. French-unique class slugs: `bio_technicien`, `exobiologiste`,
`medecin`, `tireur_delite`, `sentinelle`, `artificier`, `eclaireur`, `sapeur`; and
`field_training/guide/guide_du_tireur_delite_par_lee`.

Dailymotion preserves a **French original-era set** whose consistent
"Tabula Rasa : <topic>" naming matches the official localized press videos served by
`eu.rgtr.com/fr`'s player: `x89hk6x` "Tabula Rasa : Halloween" (2007-11-06, four
days post-launch), `x89hk63` ": Foréas" (2007-12-11), `x89hjlb` ": Combats"
(2007-12-11), `x89hjlm` "**: Opération Immortalité**" (2008-08-01), `x89hkh6`
": Patch 1.5" (2008-02-27), `x89hjly` ": Patch 1.6" (2008-03-28), `x89hk6g`
": Friday Night Fights" (2008-07-22), `x89hkgy` ": Interview de Richard Garriott"
(2008-07-31), `x89hj8q` ": Les Hybrides" (2008-02-01), `x89hj90` ": Propagande"
(2007-11-02, launch day), `x89hjz1`/`x89hjn1` ": Walkthrough partie 1/3"
(2007-09-14), `x2u1qf` "(Exemple Gameplay Instance)" (2007-08-25), `x3dja9`
"BETA by SiRiOn" (2007-11-03), and `x8g8gn` "Tabula_Rasa_final_21_02__2009"
(2009-02-21, 613 s, three days pre-shutdown — but the uploader describes it as a
*diaporama*, i.e. a **slideshow**, so it is still-image evidence rather than motion
footage). Owner accounts are mostly deleted (`owner.screenname` null); dates are the
API `created_time`.

### 11.4 German

The German locale is the richest official non-English surface: a **full localized
patch-note series** (`/de/news/patch_notes` index paginated **P0–P60**, 7 index
pages recovered, plus ~47 German deployment articles with German slugs such as
`patchnotes_und_bekannte_fehler_17_januar_2008`, `offensive_11_kleiner_patch`,
`offensive_124_auf_dem_oets`, `hotfix_01_mai_2008`, `freitags_feedback_*`), 14
class pages, ~33 enemy pages, 7 armour types, 13 weapon categories,
`understanding_logos` + `logos_dictionary` + `die_geschichte_von_logos`,
`planetary_atlas`, `veteran_rewards`, `pvp_rankings`, the whole `field_training`
tree, and `de_about_story_ethical_parables_in_tabula_rasa` — **the official German
page on ethical parables, which is the best available check on the RU Wikipedia
"≈20% of missions" claim and has not yet been read.** German shutdown/farewell pages
are queued: `/de/news_article/message_from_the_tabula_rasa_team` (@20090126185852)
and `/de/community/events/eine_denkwuerdige_zeit`.

German class-tier data, verbatim (2009-01-06/07, `original` for the DE locale):
*„Erforderlicher Level 15"* / *„30"*; *„Hierarchie Rekrut, Spezialist,
Biotechniker, Xenobiologe"*; and the Biotechniker page presenting the two tier-4
alternatives — *„…sich zur ultimativen Unterstützungsklasse ausbilden zu lassen –
den Mikrobiologen. Als Alternative dazu steht ihnen allerdings auch noch eine
Xenobiologen-Karriere in Aussicht…"* ("…to train into the ultimate support class —
the Microbiologist. As an alternative, a Xenobiologist career is also available…").
The two tier-4 pages' bodies: Mikrobiologe *„beginnen ihre Laufbahn als
Spezialisten … stehen ständig bereit, um zu heilen oder die Energieversorgung ihrer
Gruppe zu verbessern"*; Xenobiologe *„…versuchen diese Soldaten, Feinde gegeneinander
aufzubringen … können Assistenten herbeirufen, Verbündete aus Leichen von Feinden
erzeugen oder sogar sich selbst klonen"* ("…try to turn enemies against each other …
can summon assistants, create allies from enemy corpses, or even clone themselves").

German press CDX (4players.de, buffed.de, gameswelt.de, pcgames.de filtered on
`original:.*tabula.*`) is **INDETERMINATE — throttled, not "no coverage"**, logged
in `cdx_log.tsv` for retry. `buffed.de/suche/` returns 200 with 178 KB but **zero
occurrences of "Tabula"** (client-side JS search); `4players.de` search 404s
(redirects to `4p.de`). German TR sites `tr.gamona.de` and `tr.onlinewelten.com` are
**dead** (NXDOMAIN / timeout) → Wayback only.

### 11.5 Japanese — a documented-presence, no-evidence locale

An official JP site existed: `static.tabularasa.jp` has captures 2007-11 → 2008-01
(`/images/library/art/picture/`, `/images/library/art/thumbs`,
`/images/library/wallpaper` — the same `images/library/` CMS path family as the US
site). The main `tabularasa.jp` host has only a 2021 squatter capture, so **JP
official content is effectively unarchived**. `ncsoft.jp`, `plaync.jp`,
`tr.plaync.jp`, `tabularasa.plaync.jp`, `tr.ncsoft.jp`: no TR captures. **Japanese
Wikipedia has no game article** (API search for タブララサ / タブラ・ラーサ + ゲーム
returns only generic pages; タブラ・ラーサ is the philosophy article).

Both live Japanese wiki candidates are **empty shells**: `w.atwiki.jp/tabularasa/list`
returns only the 9 default @wiki pages, all "632日前", never used for TR;
`wikiwiki.jp/tabularasa/` is titled «タブラ　ラサ Wiki*» but its body is the stock
PukiWiki sample. Yahoo! Japan search works (3 queries, 338–451 KB, 52 genuine result
URLs) but yields only Japanese games press — `4gamer.net/games/010/G001036/20081122001/`
(2008-11-22 shutdown coverage) and `game.watch.impress.co.jp/docs/20080401/tr.htm`
(2008-04-01) — plus one personal blog. **No 2ch/5ch archive hits.** Consistent with
TR never having had a Japanese service. **Any JP patch schedule is an evidence gap;
do not invent one.**

Nico Nico Douga preserves **~12 original-era clips, all 2007-09 → 2008-04 and
therefore all pre-D11** — they show the pre-rebuild game and cannot evidence the
final-live tutorial: `sm990131` タブララサ(Tabula Rasa) ベストムービー (2007-09-05),
`sm996264` リチャードギャリオット最新作のタブララサ (2007-09-06), `sm1003713` (2007-09-07),
`sm1260343` Tabula Rasa (2007-10-12, 9:07, the longest), `sm1285260`
CG?それとも映画？ タビュララサ (2007-10-15), `sm1313289` **Tabula Rasaでの棒術**
(2007-10-19, staff combat), `sm1470947` **Tabula Rasaのハロウィン** (2007-11-07,
retail, five days post-launch), `sm1742382` **RGTR_CG_Intro** (2007-12-10, the
official CG intro, matching `tr_intro_720.flv`), `sm1915021` 【TabulaRasa】…首無しの男が
暴れています・・・ｗ (2007-12-31), `sm2054864` **【TabulaRasa】…Engineerで砦攻略**
(2008-01-16, retail Engineer-class play), `sm2991041` (2008-04-14), `sm2771301`
(2008-03-24, an Ultima Online episode *about* TR — context only). No TR-MMO video
was uploaded to Nico after 2008-04-14; of 70 candidate ids, **34 were the Touhou
track "Tabula rasa ~ 空白少女"** or other music. Nico requires an account for
playback download, so these are catalogued leads; watch pages are fetchable
anonymously for metadata.

### 11.6 Spanish, Italian, Polish, Portuguese, Czech, Dutch

ES and IT received **localized retail CE manuals without a localized website** —
`TR_Manual_CE-72dpi-ES.pdf` (@20081013045652, 127,924 B) and `-IT.pdf`
(@20081007172715, 127,863 B) — implying boxed retail distribution in those
territories. All five manuals (EN-UK/DE/FR/ES/IT, ~127 KB each) are **single-page
image-only reference cards, not full manuals**; extracted JPEGs are saved.
`pl.wikipedia` has `Tabula Rasa (gra komputerowa)` but cites only `us.ncsoft.com`;
`pt.wikipedia` has **no** game article (404); es/it articles exist and cite official
EU/US URLs including `ncsoft.net/global/gamenservice/playncgames.aspx?game=TR` — an
NCsoft **global** game-service portal with a `game=TR` parameter, worth one CDX
probe as a regional link farm. The 2006 pre-launch EU site has `/eu/promos/gamer_nl`,
a **Dutch** promo page: Benelux marketing existed without an NL locale section.
`tabularasa.juegaenred.com` (ES) returns an empty reply; `arenammo.com.br/portal/games/7/`
(BR) times out; `tabula-rasa.xf.cz` (CZ/SK) 404s. `tabularasamemorial.org` is a
post-shutdown fan memorial cited by es.wiki.

### 11.7 The official manuals, and the missing strategy guide

The official guide is **BradyGames**, not Prima: *Richard Garriott's Tabula Rasa
Official Strategy Guide*, Open Library work `OL8455997W` / edition `OL10724849M`,
publisher `BRADY GAMES`, **2007-10-23**, ISBN-10 `074400943X` / ISBN-13
`9780744009439`, **272 pp.**, author credit `['BradyGames']`. No Prima-published TR
guide was found in any catalogue queried — treat "Prima" as a mis-attribution.

**No public full text or scan exists.** `archive.org` `q=tabula+rasa+AND+bradygames`
→ numFound **0**; `q="tabula rasa" AND "strategy guide"` → **0**; `q=9780744009439`
→ 2 hits, both `mediatype:data` Better World Books donation manifests
(`BWB-2019-12-24`, `bwb_daily_pallets_2020-01-03_PFS`) — metadata only;
`q=tabula+rasa+AND+mediatype:texts` → 135 hits, none a TR guide; Open Library
`/works/OL8455997W/editions.json` → exactly 1 edition with **no `ia:` lending
identifier and no borrow link**. Legitimate routes remaining: physical purchase,
library loan, or a rights-holder request. **No bypass was attempted or is
recommended.** If ever obtained its tier is close to `original` for content data,
but it is dated **ten days before launch**, so it would describe **launch-era**, not
final-live D16.5, content.

The official site's `game_intel/images/strat_guide/TRguide*.jpg` (15 files, captured
2008-11-15, 32,267–59,040 B, all downloaded) are **not** page scans: two were
inspected visually and both are in-game screenshots — `TRguide_1.jpg` shows two AFS
soldiers beside a blue **waypoint pillar** with a yellow mission-objective marker;
`TRguide_11.jpg` shows an AFS soldier facing a Bane/Logos structure. They are
`original`-tier **visual** evidence (waypoint pillar appearance, objective markers,
Bane architecture) carrying **no text, tables or numbers**. Also downloaded:
`Field-Guide-Cover.jpg` (28,696 B) and its thumb — the cover of the **AFS Field
Guide**, a separate free official download announced at
`news/latest_news/the_afs_field_guide_is_available_for_download.html`.

## 12. Lineage, and what must not be seeded

### 12.1 The emulator family is one lineage, so agreement is not corroboration

```
NCsoft live service — shut down Feb 2009; final deployment D16.5; client 1.16.5.0
   │  no server-side data survived publicly
   ▼
2011-01…04  "J.H.Work" TR server emulator, written in C (jhwork.net devlog)
              source `tabularasa_src.zip` + `tr_release_2.zip` — both LOST (§7.10)
              reposted on RaGEZONE thread 740465 (2011-04-03)
2011-04-19  Google Code `tabula-rasa-server-emulator` = "InfiniteRasa" (SVN, C#/C++)
              targets TR client v1.11; "we don't aim to reproduce the original worlds"
              reposted on RaGEZONE thread 812299 (2012-01-20)
2011-05-19  github.com/InfiniteRasa org created
              Authentication-Server (C) · Game-Server (C++, 2011-08-02→2016-08-18)
              Web (PHP) · RasaWeb (PHP) · Launcher (C#)
2012-10…2013  Infinite Rasa SQL dumps (ir_gameserver / tr_auth / tabuladb)
2016-09-18  **Rasa.NET (C#)** — "A C# version of the Infinite Rasa project."; last push 2023-12-27
2019-20     Rasa.NET fork database patches (itemtemplate_itemclass, 30,225 rows)
2026-09     this repository
```

File names map 1:1 across all three implementations (`Actor`, `CellMgr`,
`CombatMgr`, `Communicator`, `Creature`, `DynamicObject`, `EntityMgr`, `GameData`,
`GameEffect`, `GameMain`, `Inventory`, `Manifestation`, `MapChannel`, `MethodIDs`,
`Mission`, `Msg`, `NetMgr`, `Npc`, `Packing`, `SpawnSystem`), which is what
establishes the lineage. `Damuras` (Site Admin) states it definitively on
`infiniterasa.org` topic `t=166`, 2016-09-21: *"Rasa.NET, aka Rasanet, is a new take
on the older C++ server using .NET core."*

**Consequence, and this is the load-bearing provenance rule for the whole project:
agreement between these implementations is not corroboration.** They are one
lineage, so shared values trace to a single ancestor. Genuinely independent
agreement found in this sweep is limited to four things: the login handshake
(2011 byte capture ↔ our parser), the game Blowfish P-array (IR C++ ↔ our C#), the
client command line (2013 issue comment ↔ 2026 `wormhole` launcher), and the
`logos` count (IR `logos.txt` ↔ decoded client `.pyo`). **Everything else —
`player_exp_for_level`, `map_info`, `equipableclass`, `itemtemplate_itemclass`,
`itemtemplate_armor` — is single-lineage and needs client-data verification**, per
`AGENTS.md`'s rule that emulator agreement is not proof of retail behaviour.

Chronology also matters for fidelity: the closer to Feb 2009, the weaker the
recall. J.H.Work (2011) is ~2 years out and is the earliest RE; the IR SQL dumps
(2012–2013) are ~4 years out; `DataLoader`/`ChunkReader`/`XmlToSql` (2015) are
~6 years out **but read the client files directly**, so their format work is not
memory-dependent and carries more weight than their age suggests. **The Blumster
format tools should be weighted above the Infinite Rasa databases despite being
newer.**

The upstream project's own charter makes the caveat concrete and citable. Google
Code `tabula-rasa-server-emulator`, created **2011-04-19**, SVN-based, 5 stars,
labels `[CSharp, Server, Emulator, Tabula, Rasa, TabulaRasa]`, licence "Other Open
Source" with CC 3.0 BY content licence, members `xtremescript`, `bingomou…@gmail.com`,
`matt.cla…@gmail.com`, "1 committer":

> "This server/emulator project will be a basis for re-building the TR universe, with
> full compatibility with the TR client **v1.11**. **As we don't aim to reproduce the
> original worlds**, we will welcome suggestions and ideas to help us improving this
> awesome game to its most!"
>
> "A legitimate copy of the Tabula Rasa Client DVD is required to work on this
> project. We do not provide nor encourage piracy." — footer: "TabulaRasa and logo
> above are copyright NCSoft."

So everything inherited from upstream is unverified **by its authors' own statement**,
and the project's stated target was v1.11, not our 1.16.5.0. Two independent routes
reached this (Wayback capture `20160427153522id_` of the project page, and the
archive's `project.json`); they agree on every overlapping fact, which is itself a
confidence signal.

### 12.2 Client-revision provenance is conflicted — resolve before trusting inherited values

An earlier draft conclusion that "the whole lineage was built against 1.11.6.0" was
**premature and is withdrawn**. The honest state is a conflict:

| # | Source | Date | Version stated | Class |
| --- | --- | --- | --- | --- |
| A | **J.H.Work**, comment on `jhwork.net/?p=69`, answering *"what version of the game are you working on?"* → *"lol right I forgot to mention that in my posts. **It's 1.11.6.0** and as far as I know **that's the latest?**"* | 2011-02-01 | 1.11.6.0 — first-person and specific, but about *his* project, and explicitly hedged: he believed 1.11.6.0 was final, which was wrong | b |
| B | InfiniteRasa Google Code blurb → *"full compatibility with the TR client **v1.11**"* | undated copy on a project created 2011-04-19; page archived 2016-04-27 | v1.11 — but this is marketing text still unchanged in 2016, i.e. demonstrably never maintained | c |
| C | **InfiniteRasa public test server**, "How to Connect" → *"**Acquire Tabula Rasa version 1.16.5.0.** … set **Hostname to `login.infiniterasa.com` and port to `2106`**"* | captured **2011-09-29** | **1.16.5.0** — the version the project demanded of *users*, 7 months after A | b/c |
| D | **dahrkael** `irsingle` singleplayer page → *"Get a copy of **Tabula Rasa version 1.16.5.0**"* | live 2026-09-13; builds dated 2020-04 | 1.16.5.0 | c |

C and D agree with `docs/setup.md`. B is most plausibly **stale copy written around
April 2011** that nobody updated when the project moved to 1.16.5.0 — which is
exactly what C then documents. A is firm but describes a *different* emulator. The
defensible statement is: **InfiniteRasa's deployed target in September 2011 was
1.16.5.0**; its own blurb nevertheless advertised v1.11; the *other* 2011 emulator
genuinely targeted 1.11.6.0.

What web evidence cannot settle is which client the surviving
`github.com/InfiniteRasa/Game-Server` C++ code (2011-08-02 → 2016-08-18) was written
against. That requires reading the code for a hardcoded version check,
patch-manifest expectations or table offsets and comparing against the 1.16.5.0
client we already hold.

Concrete lineage artifacts verified read-only in this repository:
`src/Rasa.Auth/appsettings.json:14` → `"Port": 2106`, **the identical auth port**
source C told 2011 users to configure; and `docs/setup.md:56` /
`docs/docker_setup.md:46` instruct launching the client as
`tabula_rasa.exe /NoPatch /AuthServer=localhost:2106` — the same launcher-bypass
technique J.H. described in 2011 (*"there is a .bat file which must be copied into
the Tabula Rasa directory … it will then skip the launcher and start the game"*) and
that `Dahrkael/wormhole` generalises. The launcher contract is now double-sourced:
`wormhole/core/src/launcher.cpp:150-165` builds
`"tabula_rasa.exe /NoPatch /AuthServer=%s:%s"` with port hardcoded `"2106"` (line
59), appending `/user=` and `/password=`; and `InfiniteRasa/Game-Server` issue #6
(2013-05-04, `kthxbye`) documents the identical parameters, with `Dahrkael`
answering in the same thread: *"the launcher just passes some arguments to the games
exe, so we pass them by hand."* Provenance for the argument names and port is
`observed`-grade (two independent community sources, one original-era) — still not
`original`, there being no NCsoft documentation.

**Recommended audit:** do not assume inherited values are stale, and do not assume
they are final-live. For each protocol or table constant in `src/` with no recorded
provenance, classify it as deriving from (i) 1.16.5.0 client data we already hold,
(ii) InfiniteRasa C++/C# code of unknown client vintage, or (iii) a hardcoded
default. **(ii) and (iii) are evidence gaps**, and (ii) is closable only by reading
`InfiniteRasa/Game-Server` against the 1.16.5.0 client.

Note `upstream-game-server/.git` is branch **`experimental`** (not `master`),
`HEAD = 4a9ab5f1fcdf6a18ab6911c384189cc41ddae651`, committed **2016-08-18**, subject
*"Merge pull request #29 from krssrb/experimental"*, shallow (`rev-list --count HEAD`
= 1). That is the branch `Dahrkael` told people to use (*"use the experimental
branch, the master branch is more like a memorial repository lol"*, issue #7,
2013-05-05), at its final upstream commit — the best available instance.
`TabulaGameServer.vcxproj` lists 61 `ClCompile` entries while `src/` holds 65
`.cpp` files, so ~4 sources may be excluded from the build, consistent with the
duplicate-files-after-merge problem in issue #24.

### 12.3 Do not seed these

| Item | Why |
| --- | --- |
| **`mapInfo.txt` comment coordinates** — e.g. `(1985, 'adv_bootcamp', 783, 4) #Planet Foreas - Bootcamp -225,102,-67` | **These are not data.** IR's own parser reads only four fields (`upstream-game-server/src/gameData.cpp:85,104`: `sscanf(line, "(%d, '%[^']', %d, %d)", …)`); the `#` comment and its coordinate triple are **never parsed and never used**. They are a human annotation of unstated origin — no source, no uncertainty. At best `analogue`, and `AGENTS.md` says to prefer leaving an optional value out over an `analogue`. Use `CVOGSpawnPoint` from map data instead (§9.1). |
| **IR's `ReviveMe` graveyard coordinates `(786.92, 294.83, 362.38)`** | A single hardcoded graveyard for all maps with `graveyardId` discarded (`// todo: use this`). Post-shutdown invention. Reuse the arity and `Recv_Revived(sourceId)`, not the numbers. |
| **IR's `itemtemplate_armor.armorValue` (13,893 rows)** | Our own `armorclass` re-keyed plus a ~96.9% derivation with an unidentified second input and no client counterpart (§5). |
| **The 16 EU-only "Antagonist"/"(Adversaire)" armour ids (122112–122128)** | Confirmed present in FR/DE `itemtemplatelanguage` and absent from EN/JP/KR, but with **zero official documentation** anywhere and status **undetermined** (retail vs promo vs public-test). Working hypothesis: the 2008 official armour-design contest (`ArmorCompWinnersBanner` asset) — a hypothesis, not evidence. Per `AGENTS.md`, must not be seeded as general retail content. Four complete 4-slot sets covering exactly the four Tier-3 armours (symbiotic/Bio, Graviton, stealth/furtive, mech/Nanotech); exactly 16 ids **skipping 122114**. |
| **`mmotopforge.com`'s "DEPLOYMENT 14 (FINAL)"** | Third-party private-server directory boilerplate contradicting official **D16.5** evidence. Carries no weight; record as a conflict, never as a version claim. |
| **Any TaRapedia page dated before the D11 rebuild, for boot-camp content** | Pre-rebuild content, forbidden as an analogue for rebuilt content (§7.3). |
| **Pre-revamp Nico footage (all 12 clips)** | All pre-D11 by upload date, so all show the pre-rebuild game. |

### 12.4 Open data conflicts to record, not resolve silently

- **`adv_bootcamp` map version and base region.** IR `mapInfo.txt` says version
  **783**, region **4**; TRE's 2011 copy says **792**, region **2**; our
  `rasaworld.db` `map_info` matches IR exactly (783/4), so **our row descends from
  the IR file**. Neither file states which client it was read from, and
  `docs/setup.md` pins us to 1.16.5.0 — so the correct values at shutdown are
  **unverified**. Resolve from the client's own map data; record the conflict rather
  than defaulting to the source we happen to have inherited.
- **`entityclass` has 15 more rows than the original client.** The emulator holds
  more than the artifact. Needs provenance review.
- **Item `17131`'s class.** TRE's `itemTemplates.txt` says `classId = 27120`; our
  `NewCharacterTests.cs:90` says `EntityClasses 9100301`. `9100301` is an order of
  magnitude outside every other class id in that fixture (6048, 3147, 15602, 15662,
  15632), so one of the two is likely wrong. Unresolved.
- **Boot-camp control-point capture key.** The official-host guide says activate the
  obelisk with **`'T'`**; the fan mission DB's objective text for the same mission
  says **"Use [F] to capture the control point"**. Both retail-era. `'T'` is the
  general interact key; `[F]` may be CP-specific or a transcription error. Recorded,
  not reconciled.
- **Armour Regeneration "Low" vs "Average"** for Motor Assist and Reflective across
  official captures of 2007-10-12 and 2008-08-31 (§5).
- **RU Wikipedia "1.12.5.0 (18 Sep 2008)"** vs our 1.16.5.0 (§11.2).
- **T2 torrent claims v1.15.7.0 dated 2009-01-13**, four weeks pre-shutdown, while
  T3/T4 claim 1.16.5.0 — three mutually distinct payloads (§8). A genuine data point
  for the final-revision question; do not assume they are the same build.
- **Game-Server issue #5**: a player attests *"the client as it was the day TR
  closed. My trpython.zip has date of **Feb 17 2009**"* — an **8-day conflict** with
  our acquired members (Feb 9–10), bearing directly on the open shutdown-revision
  question.

### 12.5 Dead ends — recorded so nobody re-chases them

| Item | Evidence | Verdict |
| --- | --- | --- |
| `tabularasa-reborn.com` | Returns exactly `<html><head></head><body></body></html>` — **39 bytes**, 0 bytes rendered text, in a real browser. Its only listing shows 0 players, 0 reviews, "Launched at: -". | **Phantom.** A directory-generated placeholder, not a project. |
| `discord.me/teamtabularasa` | Archived page: *"The Tabula Rasa (T∆B) **eSports and team and community server based in Bournemouth UK**… building our eSports rosters"*, status "Unavailable". | **False positive — not the MMO.** |
| `xentax.org` | HTTP 200, 28,608 B, `<title>Home</title>`, body is **Commodore 64** material (GoatFM, FM-YAM, "Written by Mr.Mouse", 2024–2026). | **Domain reused.** The real `xentax.com` / `forum.xentax.com` / `wiki.xentax.com` all 403; `zenhax.com` is a 296-byte placeholder; `forum.zenhax.com` does not resolve. No TR format thread locatable without a working search engine. |
| `trwiki.com` | "TRWiki — The open knowledge hub for **Total Rewards**" (HR/compensation). | Unrelated. |
| `logosatlas.com` | **Domain recycled** — now "LOGOSATLAS — Structural Patent & Trademark Intelligence", © 2026. | Not TR. Wayback only. |
| `planettr.com` | Parked / for sale at HugeDomains, $395. | Wayback only. |
| `tabularasavault.ign.com` | **301 → `ign.com` homepage.** The modern `ign.com/wikis/tabula-rasa` is live and was harvested (70 pages). | Vault gone; Wayback would **date** the modern wiki content. |
| `tabularasa.pw` vs `tabula-rasa.pw` | **Two different domains.** Unhyphenated = a dead **NZB indexer** (reviewed at `usenetreviews.org/nzbsites/tabula-rasa/`, contact `admin@tabularasa.pw`). Hyphenated = a live Laravel login shell whose `/register` says *"Registration Not Available — Registrations are currently closed."* | Do not conflate. Neither is a data source. |
| `infiniterasa.org` (live) | **HTTP 522** (Cloudflare origin down); DNS resolves. | All content must come from Wayback. |
| `launcher.dahrkael.net` | DNS resolves to **5.135.162.3** (OVH, FR); `http://` → 308 to `https://`; TLS handshake fails (`tlsv1 alert internal error`, `no peer certificate available`, TLS1.3 group `<NULL>`); **zero Wayback captures**. | Origin still provisioned but not serving TLS. An origin that still exists may still hold the patch manifest. |
| `TabulaRasaXI/TabulaRasa`, `TabulaRasa11/TabulaRasa`, "Tabula Rasa XI" (nostalgic.gg, ffxiprivateservers.com) | Lua, "XI", LandSandBoat lineage. | **Final Fantasy XI servers, not this game.** Recurring false positive — filter it. |
| RaGEZONE threads 927236, 1271894, 597478, 875824 | Fetched and grepped: zero "rasa" matches; 927236 is **Ran Online** (`glogic.rcc`, `MAPLIST.ini`). | Excluded. |
| `wiki-tabularasaddfandomcom` and siblings | `tabularasadd.fandom.com` = "Tabula Rasa **D&D** Wiki", a tabletop campaign. Also `wiki-tabuladndfandomcom`, `wiki-dawn_of_worlds_tabula_rasafandomcom`, `wiki-tabula_divinitusfandomcom`, `wiki-pbeuropeanhistorytabularasa.wikispaces.com`. | **Not the MMO.** |
| Non-English TR fandom wikis | `tabularasa-{de,fr,ja,es,ru,ko}.fandom.com` → 404; `tabula-rasa.fandom.com` → **410 Gone**; `de.tabularasa.fandom.com` → TLS failure. | **No non-English TR fandom wiki exists.** |
| Usenet | `alt.games.tabula-rasa` / `alt.games.ncsoft`: Google Groups 429, Atom feed trick 404, `al.howardknight.net` does not resolve. `binsearch.info` **worked** (175,905 B parsed, ~60 hits) but **all** are TV/film/music from recent `alt.binaries.*`. `nzbindex` needs login, `nzbgeek` login-walled, `nzbstars` 403, `tabularasa.pw` dead. | **Structurally exhausted:** 2007–2009 `alt.binaries.games.*` posts are beyond retention on every public index. The one untried route is Wayback captures of the Google Groups URLs. |
| Torrent indexes | `torrage.info` returns an HTML wrapper; `btdig.com` 502 (dead); `1337x.to` 403; `limetorrents.lol` 404; `idope.se` connection failure; `torrentdownloads.pro` has no game hits; `rutracker.org` 403; `nnmclub.to` works but all 6 hits are music. | No server-file or database torrent exists on any index checked. |
| GitHub code search | `npc_mission_reward` (9 hits), `wonkavate` (7), `adv_bootcamp` (1), `WorldPlacementDescriptor` (2) → **`InfiniteRasa/Rasa.NET` only**. `itemtemplate_equipment`, `creature_ai_pathnodes`, `actionarguments actionmodules` → **0**. TR gists → **0**. `creature_type_npc` (93) → Korean MMO servers, generic naming collision. | **No public GitHub repository outside our own lineage contains TR mission-reward data, boot-camp references, spawnpool data or `itemtemplate_equipment`.** GitHub is closed out as a source for that content. |
| GitHub repo searches returning `total=0` (20) | `tabula rasa private server`, `ncsoft rasa`, `tabula rasa packet`, `tabula rasa sql`, `tabula rasa bot`, `tabula rasa mmo`, `tabula rasa ncsoft`, `rasa mmo server`, `tabula rasa сервер`, `раса сервер emulator`, `타뷸라 라사`, `tabula rasa servidor`, `tabula rasa servidor emulador`, `topic:tabularasa`, `topic:mmorpg-server tabula`, `rasa mmo emulator server`, `ncsoft mmo server emulator`, `tabula rasa resurrection`, `tabula rasa revival`, `tabula rasa restore` | Non-English and revival/resurrection/restore phrasings are **exhausted** — the community never used that vocabulary. |
| GameFAQs | **HTTP 403** on `curl` and `web_fetch` for `/pc/939505-tabula-rasa/faqs` and `/guides`; Wayback of the 2008 FAQ index → 404; CDX on `gamefaqs.com/pc/939505…` resolves to **`939505-ore-wa-kanojo-o-shinjiteru`**, i.e. **the guessed game ID is wrong**; CDX prefixes `9395` return only neighbouring titles; filtered prefix queries return 0 rows; EN Wikipedia's external links contain **no GameFAQs link**. | **Access failure, not evidence of absence.** The TR game ID was never resolved, so neither the FAQ/guide index nor any board thread could be enumerated. Two threads are known to exist from search snippets: `boards/516716-richard-garriotts-tabula-rasa/48504779` "**A Post Mortem**" and `/47261970` "Any chance of private servers?". Route to close: resolve the ID from a third-party page that links to GameFAQs (MobyGames also 403s; both reachable via Wayback once throttling subsides). |
| Reddit `r/TabulaRasa` — **direct access** | **403** on `www.`, `old.` and `api.` for both HTML and `.json`; `old.reddit.com/r/TabulaRasa/top/.rss?t=all` returns **HTTP 200 but a 320,582-byte "Welcome to Reddit" login interstitial with zero `<entry>`** (a 200 here is *not* success); `search.rss` 429; `api.pullpush.io` 403 Cloudflare; four redlib/libreddit mirrors all 000/429/empty; **a real desktop browser also gets 403** (190 KB challenge page). | **Direct access is closed, but Wayback works — see the next row.** |
| Reddit via Wayback — **partially recovered** | CDX lists only `www.reddit.com` keys (134 rows, **zero** `old.reddit.com` rows), and the 2023 `www.` captures are **JS shells** titled `Reddit - Dive into anything` with no server-rendered content, so a first pass produced 12 content-free files. Fetching the **`old.reddit.com`** form through `/web/<ts>id_/` instead succeeded for **12 of 13** high-value threads. | **Recovered**, including the decisive `22i7vy` install guide and its magnet (§7.16). `18o10mk/private_server_update` is a **genuine 404** ("has not archived that URL", both forms) — a real negative. Of 59 distinct thread ids, ~35 are the **Belgian TV series**; every hit must be title-checked. |
| `fosstodon.org/@dahrkael` | All **217** statuses retrieved via the paginated REST API (2020-08-27 → 2026-08-31). **Zero** mention Tabula, Rasa, NCsoft or TR reverse-engineering. | Branch closed. |
| RaGEZONE coverage | Full sitemap enumeration: `sitemap-{1..8}.xml` → **399,155 URLs** (`forums/raw/rz_all_urls.txt`). Threads whose slug contains "tabula": 135585, 153006, 209535, 219708, 485246, 740465, 802236, 812299, 1255495 — **all fetched**. The first four are 2006–07 pre-launch hype, 485246 is a one-line question, 1255495 (2025-11-06) asks *"Anyone know if there is a tabula Rasa emulator out there… all i get for that now are dead links"* with **zero replies**. | **RaGEZONE coverage is exhausted.** Only **740465** and **812299** carry substance, and both are pointers (§12.1 lineage). Note `forum.ragezone.com/f857/` was **"MMORPG Extra Releases"**, not a TR section — there was never a dedicated TR subforum. |
| Other platforms | `raw/other_platforms/`: GitLab results (6,212 B + 3,072 B) **not triaged**; Bitbucket responses 359 B (too small to hold results); Codeberg 22 B (empty); SourceForge OpenTNL page 94,107 B unexamined. Fork lists for `Game-Server` and `Rasa.NET`, `org_InfiniteRasa_members.json`, `releases_*.json`, `issuesearch_tr*.json` **not enumerated**. | **Outstanding work, not negatives.** Forks of `Rasa.NET` are the most likely place for another party's seeded content database. |

## 13. Video and still-image evidence

1,268 candidate videos were enriched with metadata. **526 (41.5%) were not Tabula
Rasa at all** — filtered into `footage/catalog-false-positives.jsonl`, not into the
catalogue. An unfiltered pool would have overstated the evidence base by nearly
half. Final classification, carried as a `content_era` field with a
`content_era_basis` string naming the evidence for each call:

| `content_era` | Count |
| --- | ---: |
| **retail-live** | **224** |
| unknown | 252 |
| pre-release/promo-or-press | 98 |
| pre-release/beta | 42 |
| post-shutdown/private-server | 38 |
| **shutdown-event** | **27** |
| **pre-revamp/obsolete** | **5** |

254 core videos (retail-live + shutdown-event) carry gap tags.

**Upload date is not content date, and classifying on it would corrupt the
catalogue.** `wRha6SVaP_4` was uploaded **2009-02-10** but is titled "Tabula Rasa
**GDC 2007** Trailer HD"; `HjhIUyu7IK8` was uploaded **2019-10-19** and contains the
**08-24-07** opening cinematic. That 2009-02-10 cluster is D16.4's release week and
the week before shutdown, when people mass-uploaded TR media to preserve it — so a
Feb 2009 upload date documents *archival behaviour*, not video content. Both dates
are recorded as separate fields, and an uploader's stated recording date is recorded
as a *claim*.

**The Raisuly series is the single most valuable footage source found**: a complete,
date-stamped, level-tagged progression of one character from closed beta
(27.10.2007, level 17) through **level 50 at server close (01.03.2009, title reads
"PC SERVER CLOSE")** — 22 videos, enumerated at `footage/findings.md` §5. All were
uploaded 2024-01/02 as re-uploads, with recording dates in the titles. It is
`observed`-tier evidence for XP and level-up presentation, class tier choices and
their dates, and skill acquisition order, and episode #21 is direct evidence for the
documented final live events. **Caveat:** episode #01 is explicitly **Closed Beta**,
i.e. pre-retail, and `AGENTS.md` warns against treating obsolete pre-release rules as
final — beta-era and live-era episodes must not be merged into one timeline.

**Shutdown and farewell evidence:** `j_4B22Y8z28` / `P40g1AEuLlY` / `CUnkvStC93o`
"Tabula Rasa Server Shutdown Event HD" (CommanderGrog, uploaded 2009-03-05);
`i1IkD1KM4mc` "Tabula Rasa Forever Clan - Empire Sector The Last Stand" (VWAndi1981,
2009-02-16); `xz-3Sy0eaXE` "Tabula Rasa The Last Moments" (KevSniper, 2009-03-02);
`ryufOpnpzRk` Raisuly #21 (2009-03-01). Plus `archive.org/details/TabulaRasa-TheFinalStand`
(Max "Sigoya" Taha), a 213.9 s 1280×720 WMV described as the 28 Feb 2009 AFS last
stand in New York's shattered streets, *"Thanks to the live team for the final
stand"*. **The Russian "Ландыши - Мать (Tabula Rasa 13.02.2009)" video's id remains
unresolved.**

Also relevant: `Q-wyrlaxf6M` "General British (Richard Garriott) Tabula Rasa Q&A"
(2007-09-23); three `gamesradararchive` closed-beta walkthroughs (09-05-07) and its
Opening Cinematic (08-24-07); `nceurope`'s GDC 2007 trailer (**NCsoft Europe's own
channel**); MassivelyOP items `127788` "Relive the final moments of Tabula Rasa"
(2017-08-27) and `94319` "One Shots: Tabula Rasa's 21-gun salute" (2017-01-15); and
an Xfire video `xfire.com/video/4b986d/` on a platform long dead.

**Gap coverage from footage is uneven and the gaps are honest ones.** Boot camp has
two project-verified sources (`7Lrst9SG3pk` "Creating Character 1/8", frame-verified
by the 20260913-bootcamp sweep; `Ycxm8Pa1-v4` "Tutorial gameplay", uploaded
2009-01-18 before D16.5 by a different player and agreeing with the final build).
Class/skill/level and XP/leveling are well covered by Raisuly. But **vendor prices,
inventory and economy UI have zero core videos**, and coverage is thin for
missions/dialogue (4), travel/teleport (2), instances/endgame (4) and death/hospital
(5). The official image gallery cannot substitute, because it hides the HUD (§7.10).

## 14. Infrastructure and method

Recorded because these constraints shaped every result and will shape any resumption.

**archive.org.** Port **80 is blocked outright** from this machine — every request
fails with curl exit 7 in ~18 ms. Only HTTPS works. An early EU-domain probe batch
used port 80 and returned nothing at all, which nearly produced a false
"`eu.playtr.com` is empty" finding; it has **1,502 archived status-200 URLs**. Port
443 **intermittently refuses connections** (~1 in 3 during bursts, `http_code=000` in
~17 ms) while a connected fetch completes in under a second — so **long exponential
backoff is the wrong policy and rapid short retries win** (up to 16–20 attempts at
1→2→4 s). `https://archive.org/wayback/available` returns **429 machine-wide**; avoid
it. **CDX and `/web/<ts>id_/` capture fetches work fine at ~4–5 s pacing, one request
at a time.** Always use the `id_` raw form so Wayback's injected toolbar markup
cannot corrupt text that gets hashed and cited. `curl -L` is **required** for
`archive.org/download/` (without it a bare 302 silently saves 0 bytes, whose sha256
is the empty-string hash `e3b0c442…` — a useful canary). Validate that a CDX response
actually begins with `[[`: two early "successful" CDX files were 11,832-byte
*"Internet Archive: Temporarily Offline"* HTML pages saved as `.json`.

**The cardinal rule this sweep adopted:** a 429, `code=000`, or truncated response is
**`INDETERMINATE (throttled)`**, never "not found". Two concrete failures prove why —
an early parallel CDX sweep scored `4players.de` and `pcgames.de` as `cdx=0` while
direct queries return captures from 2000 and 1997 (output quarantined at
`footage/raw/wayback/_discarded-shardAB-2026-09-13/`), and a media filter matching
only video mimetypes recorded **zero** hits against 84 `rgtr.com` captures that were
overwhelmingly `application/x-shockwave-flash`. Retry lists are kept separately:
`archive/RETRY-throttled-attempted.tsv`, `archive/RETRY-not-attempted.tsv` (432
planned fetches outstanding at hand-off), `footage/wayback-retry-list.tsv` (83 rows),
`/tmp/scratch/eu_retry.tsv`, `forums/RETRY.md` + `forums/raw/RETRY_QUEUE.txt`.

**Search engines are captcha-blocked from this machine, in both curl and a real
desktop browser.** Verified: DuckDuckGo HTML 202 and `lite` captcha ("Select all
squares containing a duck"); Mojeek captcha; Brave worked for ~94 URLs across 10
queries then captcha'd (73,862 B, identical for every query) and later 429'd; Bing
returns 200 with only `r.bing.com` asset links via curl, and **in a real browser
ignores the query entirely** — two different queries returned byte-identical results
about "Tabula" the PDF-table tool; Google blocked (0 external links, `<title>Google
Search</title>`); Startpage "Robot"; Qwant 1 link; Ecosia 403; Yahoo 500; searx.be
and seven other SearXNG instances 429 or 0–3 non-result links; Yandex captcha
(`cap=1`); Rambler and `go.mail.ru`/`sputnik.ru` portal shells; Marginalia works but
has no TR content; Reddit 403 by every route; namu.wiki Cloudflare. **Working
substitutes:** DuckDuckGo in a real browser (English only — Russian, Korean and
Japanese queries return English results or unrelated Korean games), **Naver**,
**Daum** and **Yahoo! Japan** via curl, Wayback CDX, `archive.org/advancedsearch`,
Open Library, MediaWiki APIs, Discourse JSON APIs, Mastodon REST APIs, and — most
productively — **fan-site link directories**: one request to
`playtabularasaonline.com/index.php?pg=community_links` yielded a **27-entry
multilingual fansite directory** that identified `tr.gamona.de`,
`tr.onlinewelten.com`, `tabularasa.goha.ru`, `tabula-rasa.xf.cz`,
`tr.jeuxonline.info`, `tabularasa.juegaenred.com`, `arenammo.com.br`, `drool.dk`,
`tabulawiki.com`, `tr.stratics.com`, `blrp.info`, `tabularasavault.ign.com`,
`tr.warcry.com`, `trcpt.crymore.de` and `zeus.jrq.ch/trcb`. Two further directories
remain unmined: Ellatha's `directory.asp?cat=6063` (FanSites) and `?cat=6064` (Other
Forums), and JeuxOnLine's `liens-generaux`.

**DDG has no `OR` or parenthesis support** — it treats them literally and returns
"No results found", which is easy to misread as a genuine negative. Unqualified
queries are dominated by the Latin phrase (Wikipedia, Britannica, Path of Exile's
unique item, Arvo Pärt, CSS resets, philosophy texts) and by **"Tabula Rasa XI"** (a
Final Fantasy XI server). Qualify with `ncsoft`, `Richard Garriott`, `mmorpg`.

**A real browser channel exists** and is the way past captcha and JS-only pages:
`/home/blizz/.orca-relay/bin/orca` — **not `orca-ide`**, which targets `local` and
returns `runtime_unavailable`, because this shell runs on headless `vps-b1952d16`
while the Orca desktop is remote. Helpers written and tested during the sweep:
`/tmp/scratch/osearch.py <engine> <query>…` (ddg/bing/google/mojeek/startpage/yandex/naver;
appends to `browser/search-results.jsonl`; decodes Bing's `/ck/a` base64 redirects and
uses `textContent` rather than `innerText`, since Bing renders result bodies without
visual layout) and `/tmp/scratch/ofetch.py <url>…` (saves rendered text plus raw HTML
with SHA-256 to `browser/pages/`, manifest `browser/pages-manifest.tsv`, auto-flags
captcha/blocked). Note that Bing's decoded results are unusable regardless, and that
namu.wiki and Reddit remain blocked even here.

**Google Code Archive** serves only `project.json` via
`storage.googleapis.com/google-code-archive/v2/<project>/project.json`; `downloads/`,
`wikis/` and `issues/` return **401/403 AccessDenied** to anonymous callers even in a
real browser, and `code.google.com/archive/p/<project>/*.json` returns only the
2,438-byte Angular SPA shell. The **`issues` tab is nevertheless readable in a real
browser** (which is how issue #1 was recovered), and `/w/list` has 200-status
captures.

**Reusable recovery technique for login-gated forum links:** RaGEZONE hides release
URLs behind *"To view the content, you need to sign in or register"*, and the current
XenForo HTML contains no target URL at all. Old **vBulletin** Wayback captures do.
Find the old slug form `forum.ragezone.com/f<forum>/<slug>-<threadid>`, CDX it, then
fetch `.../web/<ts>id_/<url>`. That recovered
`jhwork.net/tr/tabularasa_src.zip`, `jhwork.net/wp-content/uploads/2011/01/tr_release_2.zip`,
`test.infiniterasa.com`, `infiniterasa.com/index.php`,
`code.google.com/p/tabula-rasa-server-emulator/` and `github.com/InfiniteRasa` with
no account anywhere.

**Discovery trick worth reusing:** mine every URL cited inside already-recovered
community transcriptions, normalise, and diff against cached CDX inventories. Doing
this to TaRapedia's `Updates_*`/`News_*` pages yielded 68 URLs, of which 57 were
already in CDX and **11 were not** — and those 11 surfaced `boards.playtr.com` (the
official forum), `plaync.com/us/news/2007/08/…` and third-party interviews. It is a
cheap way to find official URLs that prefix crawling misses.

Also: **MediaWiki APIs bypass HTML bot-blocks** —
`tabularasa.fandom.com/api.php?action=query&list=allpages&aplimit=500` returned 500
page titles in one call where the HTML site 403s. And **avoid CDX with large
prefixes**: it returns an empty 200 rather than an error, which is easy to misread as
"no captures exist".

**Wayback technique notes, learned the hard way and worth keeping:**

1. **phpBB / XenForo / Reddit thread URLs must be fetched with their exact query
   string.** The bare `infiniterasa.org/viewtopic.php?t=61` form returns a Wayback
   **404** even though CDX lists 200-status captures for
   `…viewtopic.php?f=15&t=61&sid=…`. Always take the `original` URL and `timestamp`
   **from CDX** and pass both; a fetcher that omits the timestamp uses `/web/2id_/`,
   which silently picks a *different* capture and can 404.
2. **`matchType=domain` combined with `filter=` 504s on large forums** — confirmed for
   `forum.ragezone.com` (all year windows) and `forum.xentax.com`. It works on small
   domains (`infiniterasa.org`, `jhwork.net`, `tr.dahrkael.net`,
   `code.google.com/p/<proj>`). For big forums use `matchType=prefix` on a specific
   path (`/viewtopic.php`, `/f857`).
3. **Distinguish four response shapes,** because three of them look like failures and
   only one is a real negative: a Wayback-rendered 404 is **~4.6–4.8 KB** and contains
   `has not archived that URL` (**real negative**); a Wayback JS shell is **~4.6 KB**
   with `<title>Wayback Machine</title>` (**not the page**); `code=000` with 0 bytes is
   a **throttle**; and an **~11.8 KB** "Internet Archive: Temporarily Offline" HTML
   page is **also a throttle** — two such pages were saved as `.json` before this was
   caught, so validate that a CDX response begins with `[[`.
4. **Reddit needs the `old.` host.** CDX lists only `www.reddit.com` keys, whose modern
   captures are JS shells; the `old.reddit.com` form resolves to server-rendered
   content (§7.16).
5. **Google Code wiki lives at `/w/`, not `/wiki/`**, and the archive's GCS bucket now
   refuses anonymous reads for every project, so only the SPA-rendered project
   description and issues list are obtainable (§7.10).
6. **A 200 response is not a successful parse.** Reddit's RSS returned HTTP 200 with a
   320 KB login interstitial and zero `<entry>` elements; Bing returned 200 with
   byte-identical results for two different queries. Assert on content, not status.

No accounts were created, nothing was logged into, no paywall or access control was
bypassed, no torrent client was started, and no downloaded binary, DLL, `.pyo` or
script was executed anywhere in this sweep. **No leaked proprietary NCsoft source or
intrusion-derived data was found.** The only "leaked server" RaGEZONE threads the
sitemap grep surfaced (`recent-ncsoft-activity-on-leaked-server.43329`,
`ncsoft-server-pack.182312`) are Lineage-era and unrelated; they were not opened. All
TR material located is community-written emulator code, community reverse-engineering
notes, or retail-client redistribution.

One repo-integrity incident, corrected within a minute: two `curl -o` calls used
**relative** output paths, so `wb_rz_740465.html` and `wb_rz_812299.html` were
briefly written to the repository root before being moved into `forums/raw/`.
`git status --porcelain | grep '^??'` was re-checked; the only untracked entries are
pre-existing project files, and pre-existing unstaged modifications were not touched.
**Rule adopted for the remainder of the sweep: absolute output paths only.**

## 15. Ranked next actions

Each names the evidence that would support it and the tier it could reach.

1. **Record the decoded client corpus in `docs/`, then use it.** 369 verified tables
   and 996 disassembly files exist and no doc references them. Supporting evidence:
   `list-tables/verify/decode-completeness-check.json` (0 mismatches). Tier:
   **`original`**. This is a documentation fix with no new research required, and it
   unblocks items 2–6.
2. **Fix the armour defect.** Retire `itemtemplate_armor` and derive mitigation from
   the verified `armorclass` (3,377 rows, `Min`/`MaxDamageAbsorbed`/`regen`), wiring
   up the fields `ManifestationManager.cs:1098` already asks about. At minimum
   reconcile the seeder against `NewCharacterTests.cs:82-91`. Tier: **`original`**.
   Evidence: §5.
3. **Decode `CVOGSpawnPoint` from `adv_bootcamp.map`.** The GLM→CHNK access path is
   fully specified by three agreeing tools, the map file is on disk (127,212 B), and
   `research/20260913-bootcamp/map/` already holds a decoder and format spec. Tier:
   **`original`** — this replaces `measured` footage-derived positions and removes the
   need for `analogue` estimates in
   `docs/evidence/bootcamp-d11-reconstruction-manifest.json`. Evidence: §9.1.
4. **Reconstruct missions from `missionobjective.pyo` (3,454) +
   `missionconversation.pyo` (5,821) + `missiontextlanguage.pyo` (32,988),** using the
   `Quest`/`QuestObjective` byte spec and modelling rewards as **index + scaler**
   (`XPIndex`/`CreditsIndex` × `tQuestXPLookup`/`tQuestCreditsLookup`), not flat
   values. Cross-check names and ordering against the Ellatha 98-mission DB **and**
   the official Wilderness walkthrough, which already agree exactly on ten mission
   names. Tier: **`original`** for definitions; `observed` for the fan cross-check.
   Evidence: §10, §8 #10.
5. **Extract the 224 missing Logos definitions** by re-running the existing decoder
   over `logosstonelanguage.pyo` and diffing all 390; keep the placement half as a
   separate gap and use JeuxOnLine's per-zone X/Y coordinates only as `observed`
   corroboration. Tier: **`original`** for names, `observed` for placements.
   Evidence: §6, §8 #11.
6. **Implement the 56-entry `tutorialdata.pyo` tutorial catalogue** behind
   `DisplayPlayerTutorialNotification` (449) and `PlayTutorialAudio` (707), which
   `docs/progression-preservation-plan.md` currently records as unrecovered. Tier:
   **`original`**. Evidence: §6.
7. **Add the cipher known-answer test** expanding `InputK[0x40]` and comparing
   `CompareD`. Near-zero cost, and the only executable check on the cipher available.
   Tier: validates implementation, not retail fidelity. Evidence: §4.6.
8. **Log the bytes at `PythonReader.cs:192,227` in a live session** to resolve
   `0x41`/`0x42`/`0x52` and remove two connection-killers. Tier: **`observed`** from
   our own client session. Evidence: §4.4.
9. **Hunt for `#infiniterasa` IRC logs.** The channel is attested by two independent
   sources (§7.15) and covered 2013–14 — precisely the period whose Documentation-forum
   contents Wayback missed, including NavMesh/pathfinding, "About Items", "Basic
   Character Class", "Weapon Generator", "Admin/GM Commands" and "Universal Login
   Library". Check public IRC log archives and the named authors **`KTB`** and
   **`Parker`**. Tier: (b) if logs surface; the channel's existence is already (b).
   This replaces the earlier "read the Google Code `/w/list`" action, which is now
   **closed as a confirmed negative** — the wiki list was read and the project had
   **zero** wiki pages (§7.10).
10. **Date the Ellatha mission DB** before using it: find a pre-2009 Wayback capture
    of `missionslist.asp`, or cross-check its reward item names and stats against the
    decoded `itemtemplate`/`armorclass`/`weaponclass`. Its content is retail (retail
    manufacturers Teleract/Titan/Pulsar/Olympia/Vextronics/Vitalius/Astra/AccuMax/Shinobi/Dynamo,
    retail NPCs Commander Elvers and Major Bonham, no private-server vocabulary
    anywhere), but the site was active long past shutdown, the DB is self-labelled
    "UNDER DEV", and **no page carries a publication date**. It also covers **Foreas
    only** — no Arieki, Torden, Valverde, Ligo or Infernas missions. Tier: `observed`
    once dated.
11. **Retrieve the official prose that is still queued:** `community/soul_of_a_soldier/*`
    (131 URLs), `community/community_news/*` (428, including `double_xp_in_squads` and
    `xp_bonus_clone_credit_and_a_pants_allowance`), `newsletters/` (9), `support/` (6),
    the DE farewell pages, `de_about_story_ethical_parables_in_tabula_rasa`, the five
    remaining DE `armour_type_*` pages, the English `magma_caverns`/`magma_caverns_ii`/
    `sanctus_grotto`/`velon_hollow`, and the DE `afs_class_scharfschuetze`. Tier:
    **`original`**.
12. **Resolve the final-client-revision question.** Three lines of evidence now bear on
    it, and they must be weighed rather than merged: the **chain of custody establishing
    T3 (`6DF0918B…`, `.zip`, created 2013-05-30) as the community-canonical 1.16.5.0**
    via the still-live `tinyurl.com/tabulamagnet` alias in the ancestor project's own
    2014 install guide (§7.16); torrent **T2 (v1.15.7.0, created 2009-01-13**, four weeks
    pre-shutdown**)**; and the Game-Server #5 attestation of a `trpython.zip` dated
    **Feb 17 2009**. Note the project's previously-known hash `f6165f01…` is now
    identified as a **later unofficial `.rar` repack, 447 MB smaller**, and must not be
    treated as the reference client without byte-level comparison. Also chase
    `infiniterasa.org/tools/tabularasa_patcher.exe` — a **team-written patcher implies a
    patch manifest / file list** recording per-file versions and hashes, which is exactly
    the artifact that would settle this. Reading the ISO-9660 primary volume descriptor
    and path table for `RGTR.ISO` and `RGTREURU.iso` via HTTP **byte ranges** (sector 16
    = offset 32768) enumerates all 574 files **without downloading either 2.9 GB image**.
    Tier: **`original`**.
13. **Audit inherited constants** against §12.2's three-way classification, and read
    `InfiniteRasa/Game-Server` for a hardcoded version check to settle which client it
    targeted. §7.14 removes one worry and adds another: the upstream game server never
    functioned, so inherited *gameplay data* cannot be retail — but inherited
    *protocol and crypto constants* can be, and those are the ones worth auditing
    against the 1.16.5.0 client.
14. **Contact `dahrkael`** (`ir@dahrkael.net`, Discord `discord.gg/KZ6dXZd`) — on
    InfiniteRasa staff in 2011, author of `TRRM` and `TRExplorer`, keeper of
    `PhysFS-AES` (a WinZip AE-1/AE-2 ZIP patch — **not** TR's client archive format; see
    the dahrkael report §3.6) and `libquicknet` (**no Tabula Rasa bearing at all**, §3.2),
    shipper of a
    1.16.5.0-targeting singleplayer server, and creator of `wormhole` on
    **2026-07-14**, i.e. actively working on Tabula Rasa now. `TRRM`'s README states a
    preservation motive identical to this project's. **Requires a user decision.**
    Note `irsingle` is **unlicensed** (no LICENSE; VERSIONINFO gives only
    `LegalCopyright: "Since 2011"`, `CompanyName: Dahrkael`) and its source repo is private,
    so it is **reference-only** — record its facts, never copy its code. It also ships **no
    content data**: its only TR payload is a 76-entry map-name list, all 76 already present
    in `MapInfoPreloader.cs` (we have 78), i.e. **delta zero**.
15. **Decide on the remaining user-decision items,** now with named people attached:
    **`Blumster`** (Infinite Rasa Lead Developer in 2016, author of the .NET Core port
    and of the format toolkit — the highest-value technical contact after `dahrkael`);
    **`edolnx`**, who seeded *both* the `.rar` and `.zip` 1.16.5.0 distributions and may
    still hold both; **`KTB`** and **`Parker`**, authors of the lost 2013–14
    Documentation-forum write-ups; `Damuras` (Site Admin) and `krssrb` (Developer);
    joining the Infinite Rasa Discord (`discord.com/invite/n7QCPNrPem`, from the 2021
    MassivelyOP article) or contacting `tehwizen` ("Wizen"); downloading and
    reverse-engineering `irsingle-0.1.0.exe` (652,288 B) / `irsingle-0.2.0.exe`
    (649,216 B) for the `/gotomap` map list; pursuing `TRExplorer.7z` from Wayback;
    seeding any of torrents T1–T4; and whether to register on RaGEZONE or Facebook to
    ask `markb`/`oliverdk` for copies of the lost J.H.Work archives.
16. **Complete the throttled and outstanding fetches:** 432 planned archive captures,
    83 rgtr media items, 37 throttled captures, the German press CDX re-run,
    `infiniterasa.org/tools` (the demo and patcher binaries), the `2oi9sb` Reddit
    thread, `f=12&t=167` (Auth Server), the Wayback pass on
    GameFAQs/`tabularasavault.ign.com`/`tr.stratics.com`/`tabulawiki.com`/`drool.dk`/
    `trcpt.crymore.de`, the `infiniterasa.org` forum indexes f=3,5,6,8–21
    (@2014-12-05), `jhwork.net/?p=112` ("NPCs") with its comments, and
    `boards.playtr.com` `Board=devs` / `Board=bugs`.
17. **Enumerate what was not triaged:** `Rasa.NET` and `Game-Server` fork lists (the
    most likely place for another party's seeded content DB), the two GitLab result
    files, `TaskmasterSoftware/Tabula-Rasa` (594 KB, pushed **2026-08-20**, described as
    *"A repository for the materials used to construct the game Tabula Rasa"* —
    **triage provenance before use**; if genuine dev material it needs the leaked-source
    protocol), `Nokddu/TABULA-RASA` (123 MB), `fallahipunk/tabularasa` (329 MB),
    `Oniboy6775/TabularasaData`, `DimitrisMich/tabularasafx` (a **second** Google Code
    TR project, same 2015 migration path), `Blumster/NMF` (2024, may supersede
    `DataLoader`), `Blumster/UniversalAuth`,
    **`InfiniteRasa/Launcher` (MIT — the only TR patcher contract in existence:
    `index.tri` = `Name;PatcherUri;Website;Host;Port`, `files.tri` = `path:md5`)**,
    `InfiniteRasa/Authentication-Server`, and the 44 unread
    `Blumster_DataLoader/XML/` struct files (a mechanical, bounded job that would
    complete the client-schema map). **Removed from this list as misattributed:**
    `Blumster/PatcherServer` and `Blumster/AutoCore` are **Auto Assault** artefacts, not
    Tabula Rasa (`PatcherServer`'s own page title is `Auto Assault - Patcher`; both are
    unlicensed or MIT-with-a-GPL-2.0 `lib/TNL.NET` submodule, so neither is vendorable).
    `Blumster/AutoCore` retains value as **lineage evidence only** — it shares our
    `ServerOpcode` names, the Blowfish table and the DES key `"TEST"`.
18. **Gaps no source found can close**, to be recorded as evidence gaps rather than
    filled: **loot tables and drop rates** (no client loot table; only qualitative
    stubs; `tLootTable`'s 44-field schema is the best available structure), **XP
    thresholds per level** (no guide corpus publishes one; `tExperienceLevel` is the
    authoritative route), **vendor inventories and prices**, **squad mechanics** beyond
    the TaRapedia `Squad` revision, and **NPC/creature placements, behaviour and stat
    values** — the last being exactly what the 2011 implementer named as the hardest
    unrecoverable part (§7.11).
