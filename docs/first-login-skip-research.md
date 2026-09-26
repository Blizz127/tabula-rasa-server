# First-login skip and reconnect audit — 2026-09-22

The original selection UI offers boot-camp bypass only when the pod has zero
logins, no last context, and an account entitled to skip. Its `OnPlayBtn`
(source 271, bytecode 39–95) otherwise sends the ordinary selection request
with `False`. Live Deployment 11 notes describe the account-wide entitlement
after another character completes Boot Camp. Exact artifacts, hashes and
rule confidence are in [the evidence record](evidence/first-login-skip.json).

The server incremented `NumLogins` only in the new manifestation. Selection
saved the last-login timestamp, but the counter reached persistence only when
leaving a registered map through `CommunicatorManager.PlayerExitMap`. Normal
registered socket disconnects already ran that callback. A crash before the
callback, or disconnect before `MapLoaded` registered the player, could leave
zero saved logins. Reconnecting then advertised an unplayed pod and could offer
the skip choice again after the player had already accepted a first entry.

Selection now commits the login count with its timestamp, selected slot and
optional skip destination. The manifestation still uses the detached row read
before the commit, so it has the same incremented count; later map-exit saves
do not increment it twice. Both calculations saturate instead of overflowing
to zero. The exact original server commit boundary is unavailable: persisting
at admission is an explicitly labelled inference that preserves the server's
existing in-memory login semantics.

Skip admission also checks the account flag loaded from persistence with the
character, rather than a potentially stale account cache. Tests exercise both
stale-cache directions, the player's No choice, replay after interrupted world
initialization, atomic failure during the skip/login save, and count consistency
between admission and later logout. They use an isolated database and deliberately
fail world initialization before map registration; they do not claim an original
client network comparison.

Skip rewards remain an evidence gap. The February 2008 *Bootcamp Bypass* page
describes Captain Burba, Major Bonham and a level-4 bypass, preceding the rebuilt
Deployment 11 tutorial. It remains excluded as a reconstruction analogue. Fresh
web searches did not establish a final-live replacement grant sequence. The
existing reconstructed exit destination and empty skip rewards are unchanged,
and no claimed 1:1 completion follows from these persistence fixes.

## Bounded grant follow-up (2026-09-22)

The entitlement citation can be strengthened: a retained **official live**
Deployment 11.6 page, captured 2008-08-28, directly preserves the account-wide
completion rule. Its hash matches the earlier boot-camp source inventory;
the first-login evidence now cites it alongside the wiki copy. The separate
public-test pages are not used to establish live grants.

Original `PM_PROMPT_SKIP_BOOTCAMP` is player message **1293**. English
`playermessagelanguage` bytecode offsets 41728–41762 contains the prompt:
“Would you like to skip Bootcamp and go straight to Wilderness?” This establishes
the destination context, not an exact spawn, level or reward package. The
selection callbacks send only the slot and boolean; no client-side grant
sequence was found there.

A reproducible scan of the 43 retained decoded English language tables for
skip/bypass within 45 characters of boot/tutorial found only that message
(its two variants, repeated in the decoder's assignment and final-value views).
This is a bounded negative result, not proof that the server gave no rewards.
Current web searches also returned old accounts of an NPC-mediated bypass,
including January and December 2007–08 reports of level 4 and weapon grants.
Those precede the August 2008 rebuild and remain excluded. No retrieved
post-rebuild selection-to-Wilderness recording established the grants.

| Required field | Established for rebuilt skip | Evidence still needed |
| --- | --- | --- |
| Eligibility | Unplayed pod, no last context, account completion entitlement | Original server completion-grant boundary |
| Arrival | Wilderness context | Skip-specific first position and hospital/waypoint list |
| Level and XP | No exact value | First arrival character level and XP before any mission or kill |
| Gear, ammo and currency | No exact skip package | First inventory/tooltips and equipment, including reserve and loaded ammo |
| Logos and skills | Ordinary creation ranks are separately established | Skip-specific Logos list, training points and attribute points |
| Missions and titles | No exact skip grants/completions | Arrival mission log, title list, and first available Alia Das conversations |

The useful next acquisition is one continuous **post-D11** recording starting
with the selection skip prompt and ending with first Wilderness inventory,
character/Logos, mission and title panels before play. An original server packet
trace of the same transition would be stronger. A present-day run of our server
with the original client can verify prompt, protocol and display compatibility,
but cannot discover the lost original server's grants. No runtime or seed values
were changed in this follow-up. See the bounded-search details in
[the grant audit record](evidence/bootcamp-skip-grant-audit.json).
