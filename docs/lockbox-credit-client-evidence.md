# Lockbox credit transfers: original client and persistence

Target: the last original live service. Evidence inspected 2026-09-12 from the
archived **1.16.5.0** client's `trpython.zip`, Python 2.4 bytecode compiled
2009-02-10 UTC. This artifact is the current compatibility reference; its identity
as the exact final shutdown build still requires an authentic final manifest.
See `final-retail-target.md` and `client-artifacts.md` for the wider target
and acquisition limitations. This change does not certify the whole economy.

## Original contract

Static inspection retained the original modules, SHA-256 manifest, extraction
script, structured instructions and raw disassembly under
`/home/blizz/backups/rasa-net/research/20260912-lockbox-credits/`.
The acquired game modules were never imported or executed.

| Original member / source line | Instructions and supported behavior |
| --- | --- |
| `client/inventory.pyo`, `DepositCreditToLockbox`, 918 | Offsets 6–15 call `TransferCreditToLockbox` with the unchanged amount in a one-element tuple. |
| Same, `WithdrawCreditFromLockbox`, 925 | Offset 12 applies `UNARY_NEGATIVE`, then constructs the one-element tuple and sends the same method. |
| `client/ui/lockboxwindow.pyo`, `GetCreditAmount`, 721 | Converts text with `int` at 33–42; amounts `<= 0` become `None` at 45–61. Any positive integer survives this check; there is no 500-credit floor. |
| Same, `OnTransferWithdrawBtn`, 693 / `OnTransferDepositBtn`, 707 | A non-`None` amount calls the corresponding inventory function; invalid input uses `PM_INVALID_AMOUNT`. |
| `client/augmentations/manifestation.pyo`, `Recv_UpdateCredits`, 398 / `_SetCreditAmount`, 415 | Stores the absolute amount by currency type and posts `PLAYER_CREDITS_UPDATE`. The additional floating-gain branch in the receiver is specific to positive prestige. |
| Same, `Recv_LockboxFunds`, 445 | Stores the absolute bank amount and posts `PLAYER_LOCKBOX_FUNDS_UPDATE`. |

SHA-256 of original members:

- `client/inventory.pyo`: `fea5b3bc09103b261cdccd8b9396b0cef0e93c0b484fcb1039655da57990bd70`
- `client/ui/lockboxwindow.pyo`: `f4209434382cd297d70e2c31090ebff69869e3a99b56d65a41d188d35ce0e5ae`
- `client/augmentations/manifestation.pyo`: `724f68eb70e5a47338db82977c55abfa09f1b187f86a0b9a560c012a8588886d`

Confidence is high for these client predicates and call shapes. They do not
establish every original server-side permission, balance cap or error response.

## Implemented correction

The old handler explicitly documented its 500-credit minimum as a temporary
workaround for negative amounts below 256 being misread as positive. Signed
compact integer decoding has already been corrected; the invented floor is now
removed. Positive deposits and negative withdrawals conserve credits, including
amounts 1–499 and exact full-balance transfers.

A single serializable database transaction conditionally updates the character's
wallet and the account's lockbox. Both expected balances and the character's
account must match; either failed comparison or a late write error rolls back
both changes. Only committed transfers alter session balances and publish the
two absolute-balance packets. Withdrawals no longer invoke the loot reward
helper or emit its claim that money was looted from an unknown source.

Zero, insufficient funds, negative stored balances and values outside the
existing signed 32-bit balance representation cause no mutation. Arithmetic is
performed in 64 bits so `int.MinValue` cannot overflow during negation. The
representation limit is an emulator constraint, **not a recovered retail cap**.
The request parser checks tuple arity, numeric type and representable width;
malformed input uses the connection's handled message exception.

## Verification and remaining gaps

Tests exercise compact signed bytes/shorts/ints, sub-500 and full-balance
transfers, insufficient funds, arithmetic limits, wrong account, stale wallet,
stale shared bank, missing bank and a trigger-induced second-write failure.
They reopen the database to check conservation and verify that failures publish
no partial balance updates. Transfers between two characters through the same
bank preserve account ownership and reject stale expectations.

Original bank-access authorization, original insufficient-funds messages,
original credit caps, concurrent-session bank refresh behavior and the exact
original delta field on a bank-related `UpdateCredits` packet remain unverified.
The existing zero delta is retained; client acceptance alone cannot prove the
original server's choice. Other credit reward/spending paths and tab purchases
still need separate economic/persistence audits. Native-client end-to-end
comparison against original service captures remains outstanding.
