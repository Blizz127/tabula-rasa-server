# Original client artifact acquisition — 2026-09-12

Target: the final live game immediately before shutdown, preserved 1:1 under
`AGENTS.md`. This audit obtained a client executable whose **embedded file and
product versions are both 1.16.5.0**, together with client Python and generated
game tables. It also obtained a matching executable/symbol pair from an earlier
2007 build. None of the downloaded executables or game Python modules was run
or imported.

The recovered final-version package is a community upload. Its version fields
are direct binary observations; its identity as a byte-for-byte official final
distribution still requires an independent official manifest/checksum or an
authenticated original installation. Do not replace that distinction with the
archive item's title or uploader-supplied creator field.

## 1.16.5.0 package and selected files

The [Internet Archive item `TabulaRasa1.16.5.0`](https://archive.org/details/TabulaRasa1.16.5.0)
was added on **2017-03-19 18:15:21**, according to its
[public metadata](https://archive.org/metadata/TabulaRasa1.16.5.0). It describes
itself as the latest client. It is incorrectly categorized as `texts`, which
explains why a preceding software-only title search missed it. The item contains
`Tabula Rasa 1.16.5.0.zip`, sized **3,091,698,303 bytes**. Archive metadata lists:

- MD5: `42d00c1e1744607180d479ec0b260306`.
- SHA-1: `03eba96d79edfbc9ba014b88699e526248bdbe1e`.
- CRC32: `7c9d7c42`.

Those are archive-provided **whole-ZIP** hashes, not independently calculated
ones. This audit used anonymous HTTPS byte-range requests to inspect the central
directory and acquire three useful members without downloading the entire
3.09 GB file. HTTP 206 responses supplied the requested bytes. The central
directory describes **713 members and 4,610,494,877 uncompressed bytes**.
Available disk space before acquisition was approximately 39.0 GB.

Each acquired member was inflated as data, checked against the central
directory's uncompressed size and CRC32, and hashed locally with SHA-256:

| ZIP member, under `Tabula Rasa 1.16.5.0/` | Bytes | CRC32 | Locally calculated SHA-256 |
| --- | ---: | --- | --- |
| `tabula_rasa.exe` | 9,719,808 | `963593fe` | `73258cf99a12b653b6d7145e76cb8cf62bdc94d18793f89aead9ae813bd90bc8` |
| `trpython.zip` | 7,037,841 | `ba1ed558` | `cd2ffe5a88c5cfd82bcc34cc38cc4ab9aedb479d70fa76c0f4476da7bb7225e6` |
| `data/game.zip` | 37,860,642 | `d8bcdb69` | `e78b53640e75954b6b36e88eccfb780fcc79ec7b7ee51edbd213073e26406ca6` |

Original retrieval URL:
`https://archive.org/download/TabulaRasa1.16.5.0/Tabula%20Rasa%201.16.5.0.zip`.
The observed download redirect was to the same item on
`https://dn760004.eu.archive.org/0/items/TabulaRasa1.16.5.0/`.
The metadata response itself has local SHA-256
`aa66cd810f3e66fe9c0acd7fcda90a19f05fe09d7df7e2b01fd29f25078721b3`.

The files are stored outside the repository at:

`/home/blizz/backups/rasa-net/research/20260912-client-artifacts/final-client-selected/Tabula Rasa 1.16.5.0/`

The parent research directory preserves `archive-final-client-metadata.json`,
`final-client-zip-tail.bin`, `final-client-zip-directory.json`,
`final-client-selected-manifest.json`, and `final-client-pe-observations.json`.
The directory file records each outer ZIP member's name, sizes, compression,
CRC32, local-header offset and DOS timestamps, permitting further bounded
acquisition. DOS dates and local ZIP timestamps do not establish release dates.

### Binary observations

Static PE resource parsing with `pefile` 2024.8.26 found:

- `VS_FIXEDFILEINFO.FileVersion` and `.ProductVersion`: **1.16.5.0**.
- String `FileVersion` and `ProductVersion`: `1, 16, 5, 0`.
- `CompanyName`: `NCsoft`; `LegalCopyright`: `Copyright (C) 2007 NCsoft`.
- PE machine: `0x14c` (32-bit x86).
- PE linker timestamp: **2009-02-10 03:25:59 UTC**. This is a build-header
  observation, not the D16.5 deployment date or independently authenticated time.
- CodeView PDB path:
  `e:\depots\game_release_candidate\tabula_rasa\build\win32\bin\Release\tabula_rasa.pdb`.

Confidence is high that the acquired executable identifies itself as 1.16.5.0.
The embedded version and February 2009 timestamp are consistent with the
official D16.5 announcements. They do not prove an unmodified binary, absence of
later unannounced builds, exact regional deployment, or final server content.

### Static reconstruction resources

`trpython.zip` contains **970 `.pyo` modules**, totaling 6,897,835 uncompressed
bytes. Useful entries include `shared/gameversion.pyo`, `client/missionlog.pyo`,
`client/ui/skillsabilitieswindow.pyo`, `client/ui/deathwindow.pyo`,
`client/gameeffects/deatheffects.pyo`, and the client ability implementations.

`data/game.zip` contains **369 generated `.pyo` modules**, totaling 37,796,230
uncompressed bytes. Useful entries include:

- `generated/client/skilldata.pyo`, `abilitydata.pyo`, and `abilityproperty.pyo`.
- `generated/client/logosstone.pyo`, `characterclass.pyo`, and `classset.pyo`.
- `generated/client/missionobjective.pyo`, `missionconversation.pyo`,
  `missionstate.pyo`, and `missionobjectivestate.pyo`.
- `generated/client/language/english/missiontextlanguage.pyo` (2,117,979 bytes),
  with other language tables alongside it.

Member inventories with timestamps, sizes and CRC32 are preserved as
`trpython.zip-directory.json` and `data-game.zip-directory.json`. These establish
the presence of candidate evidence, not the correctness of an emulator's
interpretation. Subsequent research must cite exact member names and recovered
structures, distinguish client presentation from server rules, and keep
undecoded or missing server behavior explicit. Static parsing/disassembly can
inspect the original bytecode without executing it.

## Supporting 2007 executable and matching symbols

The [pre-order bonus pack archive](https://archive.org/details/tabula-rasa-pre-order-bonus-pack)
has an uploader-supplied date of **2007-07-12** and was added on **2023-07-16**.
Its description says the disc included client debug symbols, also supplied as
a separate ZIP. The small
[EXE/PDB package](https://archive.org/download/tabula-rasa-pre-order-bonus-pack/tabula_rasa_pdb.zip)
was downloaded anonymously in full: **17,182,351 bytes**. Its locally calculated
MD5, `2a7e53f5a6513665726b870909aa0c4a`, matches the archive metadata.
Local SHA-256 is
`37da83c270d77c874b36f7cebd7542fac1a701a1ca0bd55cff59074b6f35d236`.

| Member | Bytes | Locally calculated SHA-256 |
| --- | ---: | --- |
| `tabula_rasa.exe` | 8,495,104 | `7037766f3b7b12bf0130ca61d43b6d0ddf0b4abda01f31029ded4c9f52916b7b` |
| `tabula_rasa.pdb` | 80,186,368 | `816c8de282a597d4898ddcaa0d573fd12c71351377d9c1cd871eb6d2a6abe6c3` |

The EXE's PE version is **0.1.0.0**, with linker timestamp
**2007-06-27 05:57:03 UTC**. Both ZIP members have June 27, 2007 timestamps.
The PDB's MSF information stream and the EXE's CodeView record agree on GUID
`8c5b85e2-3068-4115-a359-99e30ef9b5cf` and age **1**, establishing that the symbol
file matches this older executable. Static observations are preserved in
`preorder-pdb-identity.json` and `preorder-pe-observations.json`; the files are
under `preorder-extracted/` in the research directory.

This is useful older-client structural evidence, with strong internal pair
matching. It cannot establish final gameplay, final protocol equality, or the
meaning of changed table values without comparison to the later client.

## Earlier acquisition routes and recovered official corroboration

The [archived Infinite Rasa setup post](https://web.archive.org/web/20190820155538id_/https://infiniterasa.org/viewtopic.php?f=15&t=8)
by Damuras is displayed as **2016-07-15 10:54**, with the forum reporting
UTC-04:00. It distinguishes a freely available demo from a patch tool made by the
emulator team. Its original links are `http://infiniterasa.org/tools/tabularasa_demo.exe`
and `http://infiniterasa.org/tools/tabularasa_patcher.exe`. Their HTTPS endpoints
returned 403 during this audit; the bounded Wayback `infiniterasa.org/tools/*`
index returned no successful captures. The raw post is preserved as
`20190820155538-client-forum.html`, SHA-256
`fff3f298be7dfacfa4c3ca37fd9e4e0cddd826e990d53a80b80862f8a3fa7c43`.

[Game-Server issue 12](https://github.com/InfiniteRasa/Game-Server/issues/12#issuecomment-31155577)
also supplies the public BitTorrent infohash
`f6165f0146327b4398be5193e2f005cb12b73020` in a December 24, 2013 comment.
A [December 29 reply](https://github.com/InfiniteRasa/Game-Server/issues/12#issuecomment-31313131)
identifies the retrieved filename as `TabulaRasa1.16.5.0.rar`.
That is a second acquisition lead, not an authenticated whole-file checksum.
Public torrent cache attempts did not yield its metadata. No torrent client or
seeding service was started; the working HTTPS archive made it unnecessary.
Archive.org's own item torrent has a different infohash and packages different
outer files, so the two infohashes must not be treated as a content match.

The official US site's original
[D16.5 notes](https://web.archive.org/web/20090221145102id_/http://www.playtr.com:80/news/patch_notes/d165_021709.html)
were also recovered. They are titled **D16.5: 02/17/09** and describe the same
below-level-50 Vulcan/Angel correction as the European announcement. The US
[D16.4 notes](https://web.archive.org/web/20090213133551id_/http://www.playtr.com:80/news/patch_notes/deployment_164_02092009.html)
are titled **Deployment 16.4: 02/09/2009** and corroborate the five mechs and
late content catalog. These support the existing
[final retail target](final-retail-target.md) across both official websites;
they still provide no executable checksum or server manifest.

| Preserved official HTML | SHA-256 |
| --- | --- |
| `20090221145102-us-d165.html` | `047f56afb644563c209bc939a3f97cbfabe7106da74d9d3eb32339684bcc3b93` |
| `20090213133551-us-d164.html` | `a0b93d01dac797d3b9f8091c5d9ad2179d2622c87ec4efc963b98527cc5b06f4` |

Bounded CDX queries for `ftp.playtr.com/*`, `patcher.plaync.com/*`, and
`launcher.dahrkael.net/*` returned no successful captures. The live launcher
index at `https://launcher.dahrkael.net/index.tri` failed TLS negotiation.
These are observations about particular queries/endpoints, not proof that all
official patch artifacts are lost. The `playtr.com/*` download/patch index did
return the official notes used above and is preserved locally.

## Access and remaining verification

The selected 1.16.5.0 files and the older EXE/PDB ZIP were publicly retrievable
over HTTPS without an account, payment, credentials, or access-control bypass.
This establishes technical availability. The community item metadata does not
establish copyright ownership or a redistribution license; do not treat its
collection labels as permission to republish the client. Raw artifacts remain
outside the source repository for this preservation research.

The highest-value next steps are to decode the recovered client tables and
presentation logic statically, record member-level evidence for each rule, and
compare with authentic final gameplay and server-side records. The full outer
ZIP can later be downloaded and its archive-provided hashes independently
checked if preserving every asset is needed. An independently sourced final
official manifest or authenticated installation remains necessary for stronger
whole-client authenticity and shutdown-state claims.
