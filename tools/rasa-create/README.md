# Reviewed local create-service repair

Task `58499c39efe7`. This is a patch for AX41's separately deployed
`/home/blizz/servers/rasa-create/app.py`; this repository does not deploy it.
It **was deployed** on 2026-10-01 02:08 UTC under owner approval `1b243742`
(see `docs/login-investigation-2026-10-01.md`, "Deployment" section): live
`app.py` now hashes to the candidate value below, the baseline is kept beside it
as `app.py.bak-before-login-contract-20261001T020751Z`, and only
`rasa-create.service` was restarted. No production account was changed during
preparation, verification or deployment.

The patch keeps the original client protocol, existing six-character minimum,
ASCII/character restrictions, upsert mechanism and account mappings. It limits
usernames to 14 bytes and passwords to 16 bytes, and rejects whitespace instead
of silently removing it. Accepted secrets reach hashing unchanged. It does not
choose a derived credential, truncate a password or reset an account.

Reviewed baseline source SHA256:
`dd218495885256109336a3ae3c4cb006f4b9a0fec203615541ab2ba86888e082`.
Tested candidate SHA256:
`09af43ecc400ec5eb17f7bf3a05d800be77d7195c63322c7628e5a9396068651`.
These identify source files, not credentials.

Existing local source snapshots and candidate:
`/home/blizz/scratch/tr-login-58499c39efe7/app.original.py` and
`app.candidate.py`. The snapshots are not added to the repository.

Run focused verification:

```sh
python3 tools/rasa-create/test_login_contract.py /home/blizz/scratch/tr-login-58499c39efe7/app.candidate.py
```

Six tests pass, covering both form and JSON handlers, insert and update refusal,
exact confirmation, unsupported whitespace/control/Unicode input, existing
character restrictions, and accepted passwords matching the packet's bytes and
Rasa SHA256 algorithm. The live baseline fails four tests. The harness compiles
only selected AST functions/constants; startup, networking and production data
are excluded. Writes use a disposable SQLite database. Patch application in a
temporary directory produces the tested candidate byte for byte. This does not
verify FastAPI HTTP transport, launcher typing, or actual owner authentication.

Exact proposed deployment action for coordinator review, if authorized:

1. Recheck the live source hash against the reviewed baseline. If changed,
   reconcile the diff and rerun verification rather than force the patch.
2. Back up only the create-service source, apply `login-contract.patch` with
   `-p1` in its directory, and verify the candidate hash.
3. Restart only `rasa-create.service`, then inspect `/healthz` and unit health.
   Roll back the source and restart that same service if it fails.

No Auth/Game restart, image rebuild, DIT change, database rewrite, password reset,
access-policy change or owner foreground input is part of this proposed action.
Later launcher upserts retain their current credential-writing behavior; this
patch does not independently authorize invoking them. Incident diagnosis and
actual owner login verification remain pending. A shared password outside the
client's representable range needs an explicit owner decision in the existing
account workflow; there is no game-only substitute in this candidate.
