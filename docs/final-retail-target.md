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

The repository's [setup guide](setup.md) requires **1.16.5.0**. That is proven as
the emulator's supported client requirement. Its relationship to the official
D16.5 release is consistent with the numbering, but this audit has not inspected
a retail executable version resource, patch manifest, original binary checksum,
or archived patch package that proves the exact correspondence.

| Question | Evidence status |
| --- | --- |
| Was official D16.5 released to live servers? | Proven by the original official announcement. |
| Latest positively identified live patch in this audit | D16.5, announcement dated 17 February 2009. |
| Is 1.16.5.0 required by this emulator? | Proven by the checked-in setup guide. |
| Is 1.16.5.0 the exact final official executable revision? | Not yet proven by a binary or patch manifest. |
| Were there later unannounced client or server changes before shutdown? | Unknown; a missing later search result does not prove absence. |
| Were all regional servers on identical final content/configuration? | Unknown; European official announcements alone cannot prove every region. |
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
The archive may be incomplete. Next evidence needed: original final client
version resources and file hashes; last official patch manifest/package; later
server hotfix records; final US and European event captures; and versioned client
tables. Correlate every rule and content import with that evidence before
claiming final-retail fidelity.
