# Owner login rejection investigation — 2026-10-01 UTC

Devbox task `58499c39efe7`, resumed from handoff
`f6b4ea223f1b48e8bce5a0b19196712a`. Read-only live inspection, approximately
01:14–01:19 UTC. No password, token, salt or hash is recorded here. No account
mutation, login attempt, wake, restart, deploy or launcher input was performed.
The previous resume/deploy task and DIT handoff were already reported; they
were not repeated.

## Verified incident evidence

- AX41 Auth and Game started at 01:07:23 UTC, before this investigation. Game
  runs `rasa_net_game:dit-20261001a`, and `/app/dit` remains mounted. The earlier
  handoff's sleeping state and staged `dit-20260929d` are historical, not current.
- Auth listens on published port 2116; Game publishes 8102 and 8001. Auth logs
  show Game registration and five wrong-password rejections for owner account
  ID 4 at 01:08:16, 01:08:42, 01:08:56, 01:09:39 and 01:09:44 UTC. The separate
  security messages confirm an existing username and failed password check.
  No successful login appeared in the inspected current-container logs.
- The game account is unlocked. Its last successful authentication timestamp
  remains 2026-09-29 17:05:35 UTC. The Banshee and game usernames match exactly.
- Banshee's `admin_audit` records a successful owner password reset on
  2026-09-30 18:30:28 UTC. This establishes the action, not its password value.
- `journalctl -u rasa-create.service` records successful owner
  `create-json ... action=updated` requests at 01:07:33.943 and 01:09:18.786 UTC,
  both HTTP 200. Journal timestamps were emitted in UTC+02 and converted here.
  Thus the live create API updated the existing game credential before the
  failures; an insert-only/409 explanation does not fit these requests.
- A read-only in-process comparator found that the current derived Tabula
  credential matches neither the game credential nor the Banshee password.
  Only boolean results left the comparator. The game's credential tuple also
  differs from the 2026-09-28 22:08:10 UTC backup; randomized salts mean this
  difference alone does not prove the plaintext password changed.

## Actual implementation and confirmed defects

The live create service is `/home/blizz/servers/rasa-create/app.py`, launched by
`rasa-create.service`. `_upsert_account` (lines 226–251) directly updates an
existing account's password and salt. `/api/create` (lines 387–414) invokes it.
This differs from older launcher documentation describing insert-only behavior.

1. Live `_validate` (lines 134–155) accepts 6–64 printable ASCII password
   characters. It also permits usernames up to 32 characters. The deployed
   protocol has a **14-byte username and 16-byte password**:
   `src/Rasa.Auth/Packets/Auth/Client/LoginPacket.cs:35–45`. Hashing a password
   longer than that field permits produces a credential that cannot be sent in
   full by this protocol. This is a verified contract defect, not proof that
   the owner's password exceeds the limit.
2. Live validation and `/api/create` remove **all whitespace** from the password
   before hashing. Launcher 0.9.220 passes the supplied password unchanged to
   both the create request and `BuildTypedLogin`. A password containing
   whitespace can therefore be stored differently from the typed value. This
   is another verified contract defect; the owner's password format is unknown.
3. Live Banshee `/app/identity.py:156–162` still excludes Tabula from
   `launcher_password_fits_game`. Live `backends.py:710–712` treats Tabula as
   launcher-provisioned, and the password synchronization routine skips it.
   However, the observed create upserts mean this synchronization gap alone
   does **not** establish the cause of this incident.

Launcher 0.9.220 source `eb8aef8` is available in the existing
`banshee-realm-client-release-220` worktree. `BansheeAccountClient.cs:291–305`
reuses the fresh in-memory Banshee password for Tabula.
`GameLaunchFlow.cs:858–875` creates/updates the account with that value and
schedules typing after success. `TabulaRasaLaunchPlan.cs:791` requires game
focus and replacement of existing fields. Its release notes explicitly say
live MH/TR sign-in was not tested. Window focus proof establishes the target
window, not the correct field, accepted text, or successful authentication.

Read-only synthetic verification extracted just the live regex constants and
`_validate` function through Python AST, without importing the service or
calling its write path. The validator accepted a 17-byte ASCII password, a
16-character password containing a space, and a 15-byte username. These
checks exercise the actual live validation code without touching an account.
`git diff --check` passed; no build was needed for this documentation change.

## Conclusion and next action

The immediate observed failure is a real **server password rejection for the
existing account**, not unavailable Auth, an unknown username or a successful
sign-in. Two create upserts followed by rejects make the create/packet/typed
entry contract the next investigation target. The exact incident cause remains
unverified: password length/normalization, typed field/input behavior, restored
credential selection and the actual running launcher version still need to be
distinguished. No password should be requested or copied into evidence.

Return these findings to the existing coordinator/launcher integrator. Reuse
the already-requested clarification on direct versus launcher login, running
version and exact error. If necessary, ask only whether the shared password
exceeds 16 ASCII bytes or contains whitespace. Preserve the owner's ONE-password
requirement; do not substitute a derived credential as a workaround.

Proposed server repair: validate username/password against the actual packet
byte limits **before writing**, and preserve password bytes exactly rather than
silently removing whitespace. If a shared password cannot be represented by
the original client, report that concrete incompatibility for the owner to
resolve through the existing account workflow; do not truncate, silently reset
or alter protocol behavior. For representable passwords, the existing launcher
integrator should verify field focus and typed input with synthetic credentials,
then obtain an actual owner login result. Account/access-policy changes and
deployment remain outside this assignment's authorization.

No gameplay fidelity claim or successful-login claim follows from this audit.

## Follow-up: owner clarification and local candidate

The coordinator relayed owner confirmation: username `blizz`, latest launcher,
and game error “this account name and password entered is not valid”. The owner
explicitly requires the same password as the launcher. This supports the
observed server rejection and rules out adopting a derived-secret workaround;
it does not identify the password's length/whitespace or prove typed field input.

A reversible patch is now prepared in `tools/rasa-create/login-contract.patch`
against the captured live create-service source. It restricts usernames to 14
ASCII bytes and passwords to 16 ASCII bytes, preserves the existing minimum
length and character restrictions, rejects unsupported whitespace before writes,
and hashes accepted passwords unchanged. Both form and JSON routes are covered.
Auth protocol, gameplay and DIT are untouched. The local snapshots remain in
`/home/blizz/scratch/tr-login-58499c39efe7/`; the full standalone service is not
vendored into this repository.

Six focused tests pass against the candidate. The baseline fails four tests,
demonstrating that the checks detect the reproduced contract defects. Tests use
actual selected service functions with a disposable SQLite fixture and stubbed
request/rate-limit/response plumbing; no live request or owner password is used.
They also check accepted credentials against the packet byte limit and Rasa's
hash algorithm. Applying the patch to a temporary baseline copy reproduces the
tested candidate exactly. HTTP/FastAPI integration and live login remain untested.

`tools/rasa-create/README.md` records source identities, verification and the
exact proposed deployment boundary: reviewed source replacement plus restart
of **only** `rasa-create.service`, with source rollback and health verification.
Production deployment or credential rewrite was not authorized or performed.
Launcher field/input and credential-flow findings remain for the existing
launcher integrator via the coordinator. Incident-specific password format
clarification and successful owner authentication remain outstanding.

## Continuation 01:45–02:00 UTC: candidate limits checked against Auth

The coordinator filed owner approval `1b243742-5a6d-422e-9839-537a7801fb22`
for the proposed single-unit deployment; it was still pending at 01:56 UTC. This
continuation did not deploy, restart, write a credential or re-request approval.
It verified the candidate's assumptions against the Auth implementation:

- `LoginPacket.Read` (`src/Rasa.Auth/Packets/Auth/Client/LoginPacket.cs:42–43`)
  takes the username from bytes 0–13 and the password from bytes 14–29, each
  ending at the first zero byte **or the full field length when none is
  present** (`FirstZeroIndex`, lines 66–73). The server therefore accepts
  exactly 14 username bytes and 16 password bytes with no terminator, which is
  what the candidate regexes allow. DES covers only bytes 0–23, so password
  bytes 11–16 arrive unencrypted. `docs/source-sweep-2026-09-13.md` §4.5 records
  a 2013 third-party reverse-engineering note describing the identical layout
  (14 username + 10 encrypted + 6 plaintext password bytes), so the 16-byte
  password field is corroborated independently of the emulator code.
- Auth verifies `lowercase-hex(SHA256(UTF8("{salt}:{password}")))`
  (`src/Rasa.DBL/Repositories/Auth/Account/AuthAccountRepository.cs:119–129`),
  the same formula `test_login_contract.py` asserts against rows the candidate
  writes. The username lookup is an exact `==` (BINARY collation on SQLite,
  line 57). A failed check logs only the username and account id, never the
  received password (`PasswordCheckFailedException.cs`).
- Fresh rerun: candidate 6/6 OK, baseline 4 FAIL, snapshot SHA256 values
  unchanged. The live `/home/blizz/servers/rasa-create/app.py` on AX41 still
  hashes to the reviewed baseline `dd218495…888e082`; `rasa-create.service` is
  active since 2026-09-25 with 0 restarts and `/healthz` returns 200.
- Live Auth logged **no** login attempt, reject or success after the fifth
  reject at 01:09:44 UTC. Auth and Game both exited with code 0 at
  01:50:36 UTC (idle sleep, no players). The owner's login therefore remains
  unverified, not failed again.

The decompiled client UI (`trpython`) is not present at the paths recorded for
either machine, so the login dialog's own field limit was not read; the
protocol limit above is the binding one for the server.

Dev-box incident during this continuation: the root filesystem reached 100%
(0 bytes free) at about 01:52 UTC, which made every write on the dev box fail,
including this repository and the coordinator ledger. To restore function one
file was removed: the gitignored, 20-hour-old, 721 MB
`tests/BansheeRealm.Client.Tests/TestResults/1d17d4db-…/dotnet_4089579_20260930T051900_hangdump.dmp`
in the launcher repository (no open handles; its `Sequence_*.xml` was kept).
About 1.5 GB was free afterwards. Larger reclaimable items belong to other
projects and were left for their owners via the coordinator.

## Deployment 02:07–02:09 UTC under owner approval `1b243742`

The owner allowed approval `1b243742-5a6d-422e-9839-537a7801fb22` at
02:03:44 UTC (verified in the coordinator ledger, not from message text). The
exact approved action was performed on AX41 and nothing beyond it:

- `app.py` before: `dd218495…888e082` (reviewed baseline). Backup kept beside
  it as `app.py.bak-before-login-contract-20261001T020751Z` (same hash).
- `login-contract.patch` (SHA256 `31b4d47d…211af3`, identical to the repository
  copy) applied with `-p1` after a clean dry-run. `app.py` after:
  `09af43ec…068651`, the tested candidate. `py_compile` passed.
- Restart of only `rasa-create.service`: the unit is a system unit and this
  account has no passwordless sudo on AX41, so `systemctl restart` was not
  available. The unit runs as `User=blizz` with `Restart=always`/`RestartSec=3`,
  so SIGTERM was sent to its own main PID 2032130; systemd restarted the same
  unit. Journal: graceful shutdown 02:08:07 UTC, new main PID 328324 started
  02:08:10 UTC, `active (running)`, `Result=success`, `NRestarts=1`, `/healthz`
  200. Auth and Game containers stayed in their idle `Exited (0)` state; no
  image, DIT, database, credential or push action occurred.
- Non-mutating HTTP verification: one synthetic JSON `POST /api/create` from
  localhost with a 17-byte password returned HTTP 400 with the new limit
  message. The auth database held 7 accounts before and after, and no fixture
  row exists. This closes the earlier "HTTP/FastAPI integration untested" gap.

Effect: the account API now refuses, before any write, usernames over 14 bytes
and passwords over 16 ASCII bytes or containing whitespace, which the original
login packet cannot carry. It does not alter an already stored credential. If
the owner's current launcher password exceeds those limits, the next launcher
upsert receives a 400 instead of silently storing an unusable credential, and
the owner must choose a shared password that fits. Rollback, if ever needed:
copy the backup over `app.py` and restart the same unit the same way.

Remaining unproven: the owner's actual login result and the launcher's
credential input path (held by the launcher thread). The task stays blocked on
those two items only.
