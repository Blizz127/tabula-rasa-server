# Final retail preservation target

Research date: 2026-09-12. The user's target is the **exact final live retail
state immediately before shutdown**, reproduced one-to-one. Earlier patch
behavior, emulator convenience defaults, and speculative improvements are not
the target. Unknown details remain open research items.

## Version and date evidence

**Deployment 16.5 was live on 17 February 2009.** This is established by the
original European official site, not inferred from an emulator version string.
The [archived official announcement](https://web.archive.org/web/20090221122056id_/http://eu.playtr.com:80/en/news_article/d165_now_on_the_live_servers)
is titled “D16.5 now on the Live servers”, dated **17th February 2009 17:31**,
and attributed to Avatea. It reports a correction allowing players below level
50 to use Vulcan and Angel mechs. The page does not identify its displayed
time zone. Its archive capture is 2009-02-21 12:20:56 UTC; this is the capture
time, not the release time.

The [preceding official D16.4 notes](https://web.archive.org/web/20090214230759id_/http://eu.playtr.com:80/en/news_article/deployment_164_patchnotes_known_issues_february_2009_live)
are explicitly labeled Live and display **9th February 2009 16:07**. They contain
the D16, D16.3, and D16.4 changes, and say the Vulcan/Angel correction would come
later. Therefore, copying the D16.4 restrictions would preserve a bug that the
subsequent live update fixed. Contemporary publication dates differ: the
[TaRapedia front page](https://tabularasa.fandom.com/wiki/Main_Page) records
D16 becoming live on 10 February, while the
[GameBanshee reproduction](https://www.gamebanshee.com/news/91522-tabula-rasa-deployment-16-launched.html)
was published on 11 February. Use each date for the event it actually establishes;
do not silently treat article dates as executable build dates.

The repository's [setup guide](setup.md) requires **1.16.5.0**. This audit has now
obtained an executable and client data from a community archive. Static PE
inspection confirms embedded file and product versions **1.16.5.0**, with a
February 2009 linker timestamp. Selected members passed ZIP size/CRC checks and
have locally recorded SHA-256 hashes; see [client artifacts](client-artifacts.md).
The original US D16.5 notes also corroborate the European announcement. These
observations support the version correspondence, but an official distribution
manifest/checksum or authenticated installation is still needed to establish
the package's byte-for-byte authenticity and exact shutdown revision.

| Question | Evidence status |
| --- | --- |
| Was official D16.5 released to live servers? | Proven by the original official announcement. |
| Latest positively identified live patch in this audit | D16.5, announcement dated 17 February 2009. |
| Is 1.16.5.0 required by this emulator? | Proven by the checked-in setup guide. |
| Does the acquired executable identify itself as 1.16.5.0? | Yes: both fixed and string PE version resources; see the artifact record. |
| Is that executable the exact final official distribution? | Consistent with official D16.5 notes; independent official checksum/manifest and final-state authentication remain missing. |
| Were there later unannounced client or server changes before shutdown? | Unknown; a missing later search result does not prove absence. |
| Were all regional servers on identical final content/configuration? | Unknown; matching US/European patch announcements do not establish identical final server configuration. |
| Have the final server scripts, event schedule, and content data been recovered? | No. |

Do not call the last positively identified patch the conclusively final binary.
Retain the supported client while collecting the evidence needed to verify it
against the user's actual final-retail target.

## Final content and event state

The D16.4 official notes establish expanded CELLAR/Edmund areas, mech pads and
five mech types, and new drop content. Mechs are described as usable by any
class, with Logos requirements for their abilities, and confined to Edmund.
The drops include three armor sets with level-50 stats usable at any level,
weapons named after players, pet/emote items, and Hyper-EXP tokens. These unusual
end-of-service rewards must not be rejected as non-retail simply because they
differ from earlier progression. The notes establish their existence, not their
complete numeric definitions or drop probabilities.
[Source: official D16.4 notes](https://web.archive.org/web/20090214230759id_/http://eu.playtr.com:80/en/news_article/deployment_164_patchnotes_known_issues_february_2009_live).

The [official 27 February transmission](https://web.archive.org/web/20090228223014id_/http://eu.playtr.com:80/en/news_article/feedback_friday_27th_february_2009)
is dated **27th February 2009 18:00**. It calls players to defend AFS bases
against a final multi-front Bane offensive involving Neph commanders and
wormhole reinforcements, with Penumbra preparing a last-resort response.
This proves an announced final event; it does not supply precise encounter
timers, spawn counts, AI, stats, rewards, or regional execution logs.

The [official farewell](https://web.archive.org/web/20090306010828id_/http://eu.playtr.com:80/en/news_article/transmission_over)
is dated **1st March 2009 18:00** and confirms the end of the adventure. The
shutdown date of **28 February 2009** is also recorded on
[TaRapedia](https://tabularasa.fandom.com/wiki/Main_Page). The farewell's posting
date must not be substituted for the shutdown time. Exact regional shutdown
timestamps and the immediately preceding event state still require evidence.

## Final night as filmed

Three player recordings of the EU Centaurus shutdown night (2009-02-28/03-01) supply direct,
`observed`-tier evidence of the shutdown sequence and the closing Earth Last Stand, cross-checked
against each other and against the recovered 1.16.5.0 client tables. Full citations, timestamps and
uncertainties are in [retail-accuracy.md](retail-accuracy.md)'s 2026-09-26 supplied footage ledger
and `docs/evidence/footage-fanout-20260926/`.

- **The countdown.** The server broadcasts yellow, unprefixed text: `ADMIN MESSAGE: Server Shutting
  Down in 10.`, then bare numbers 9 down to 1 with no further wording. The step cadence is irregular,
  about 3-7 s per step (mean 3.8 s, +/-1 s per reading from 1 fps sampling) — it is **not** one number
  per second. This is confirmed from two independent viewpoints/uploads of the same EU Centaurus
  night within about 1 s of each other, and separately cross-checked against a bystander's own
  closure timer.
- **The disconnect.** A modal dialog appears: English client "You have been disconnected from the
  server" / Ok; German client "Ihre Verbindung zum Server wurde getrennt." / Ok — the same client
  string (`uielementlanguage` 9), read from two different client languages. The game world, mission
  tracker, squad frames and minimap stay rendered behind the dialog. No attack, explosion, fireworks,
  cinematic or fade is shown before the drop.
- **Servers OFFLINE.** After disconnecting, the server selection list shows all four servers
  (Cassiopeia, Centaurus, Hydra, Orion) as OFFLINE, and stays that way after a manual refresh. This is
  the post-shutdown state only; it must not be seeded as a live server-list condition.
- **The admin broadcast.** A German-client capture shows `ADMIN-NACHRICHT: ALERT: PLATEAU IS LOST!`
  — the German UI prefix (`uielementlanguage` 4146) wraps an English payload, proving zone-loss
  broadcasts are server-sent free text rather than a fixed client string. Only this one alert was
  captured; whether other zone losses trigger the same broadcast is unverified.
- **The Earth Last Stand.** The closing event plays out in numbered instances of context 2375
  ("Empire Sector: The Last Stand" / German "Das letzte Gefecht", map
  `adv_earth_unitedstates_manhattan_01_shared`): Bane Stalker-type walkers, Thrax riflemen, a
  ballistic mini-turret, and dozens of level-50 players fighting at named New York locations
  (Madison Square Park, 28th Street Station, a sandbagged street line). Chat in one recording claims
  "Earth 2" (a different numbered instance) had its Empire Sector re-taken shortly before shutdown;
  this is a player claim, not a server message, and is not proof of the outcome in every instance.
- **The Neph-led offensive.** Target frames show a level-50 "Neph Waven" combatant carrying seven
  player-style buff icons, fought alongside ordinary AFS defenders at Dybukkar Forward Camp and
  Charon's Crossing. This suggests a staff- or event-controlled Neph combatant; it is an inference,
  not a confirmed GM avatar.
- **Explicit do-not-implement list**, confirmed verbatim from the shutdown-night research READMEs:
  - **Fireworks, explosions, a nuclear detonation or "shockwave".** A running player chat joke
    ("The day Earth exploded b'cuz of fireworks XD") in both recordings; no such effect is visible in
    either. Do not implement any of these as a shutdown effect.
  - **Bases turned permanently Bane-held on Centaurus.** In the last roughly 2.5 minutes of the EU
    Centaurus recording, Foreas Base in Concordia Divide was held and normal. Secondary claims of
    bases falling are from other, US-server accounts and must not be generalized into an "all bases
    Bane-held" final state.
  - **Player mechs outside Edmund.** A large armoured bipedal figure appears among posing players
    during the pre-shutdown gathering, with chat asking "where is freemech?" and "mechs for
    everyone". The D16.4 notes confine mechs to Edmund, so a player mech elsewhere conflicts with
    that. This is an unresolved, low-confidence lead; do not implement mechs outside Edmund from this
    footage alone.
  - **Auto-promotion to level 50.** A player's chat claim, "for some reason I was auto promoted to
    level 50 / well all my alts were anyway," is explicitly unverified against any official or client
    evidence and must not be implemented as a mechanic.

## Preserved original evidence

The raw HTML was retrieved with Wayback's `id_` modifier and saved outside the
repository in `/home/blizz/backups/rasa-net/research/20260912-final-retail/`.
The local byte hashes below are SHA-256. They identify the archived HTML received
in this audit, not the game binaries. All four pages identify NCsoft Europe in
their footer. Original URLs use the prefix
`http://eu.playtr.com:80/en/news_article/` followed by the slug below.

| Original article slug | Publication date as displayed | Archive capture UTC | Local file | SHA-256 |
| --- | --- | --- | --- | --- |
| `d165_now_on_the_live_servers` | 17 February 2009 17:31 | 2009-02-21 12:20:56 | `20090221122056-d165_now_on_the_live_servers.html` | `1c0cc53e6b7be88d154981b94c172e0ccfa1b3fa8231802c33ecbcf18e3020fc` |
| `deployment_164_patchnotes_known_issues_february_2009_live` | 9 February 2009 16:07 | 2009-02-14 23:07:59 | `20090214230759-deployment_164_patchnotes_known_issues_february_2009_live.html` | `efc81c6749bd0108cc22a956d8edce1f50c8d53b23505886ea641b5f0c000343` |
| `feedback_friday_27th_february_2009` | 27 February 2009 18:00 | 2009-02-28 22:30:14 | `20090228223014-feedback_friday_27th_february_2009.html` | `0a4b4eeeed8e4361159325bab73b19b27174ce60789b96cc364d74bd7c2dea0f` |
| `transmission_over` | 1 March 2009 18:00 | 2009-03-06 01:08:28 | `20090306010828-transmission_over.html` | `540c02791cb8f42001069d32b934143448ac1a339c219a09ee05cdc8089ab950` |

The normal browser fetch of these Wayback pages failed, but the raw captures
returned the original HTML successfully. The captures were discovered through
the public Wayback CDX index with these constraints:

- URL pattern: `http://eu.playtr.com/en/news_article/*`
- Date interval: `20090201` through `20090310`
- Filters: `statuscode:200`, `mimetype:text/html`
- Output fields: timestamp and original URL, collapsed by URL key

The bounded index search did not reveal a later numbered deployment announcement.
The archive may be incomplete. Client version resources, selected file hashes,
and versioned client tables have since been recovered; their provenance and
limits are in [client artifacts](client-artifacts.md). Next evidence needed:
an independent official patch manifest/checksum, later server hotfix records,
and final US and European event captures. Decode the recovered tables and
correlate every rule and content import with that evidence before claiming
final-retail fidelity.
