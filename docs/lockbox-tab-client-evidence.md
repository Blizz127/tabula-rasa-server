# Lockbox tab purchases: original prices and admission rules

Inspected 2026-09-12 for the final-live preservation target. Source is the
archived 1.16.5.0 compatibility client's original Python 2.4 bytecode. Exact
identity with the shutdown service's last build remains a manifest evidence gap;
see `final-retail-target.md` and `client-artifacts.md`.

## Primary artifact evidence

Static-only extraction, originals, SHA-256 manifests, raw disassembly and
structured instructions are retained under
`/home/blizz/backups/rasa-net/research/20260912-lockbox-tabs/`.
The literal decoder interprets a restricted set of data-building instructions;
acquired game modules were never imported or executed.

`data/game.zip::generated/client/lockboxtabdata.pyo` has SHA-256
`1e905255ca7a5a33be4c914982644ef268377d10a4c4e770bdc4cbedc803f981`, embedded
source `data/game/generated/client\\lockboxtabdata.py`, timestamp 1234238135
(2009-02-10 UTC). Its five `lookup` assignments at byte offsets 11, 20, 29, 38
and 47 are:

| Tab ID | Purchase price | Slots |
| --- | ---: | ---: |
| 1 | `None` | 96 |
| 2 | 100,000 | 96 |
| 3 | 1,000,000 | 96 |
| 4 | 10,000,000 | 96 |
| 5 | 100,000,000 | 96 |

Original consumers in `trpython.zip`:

- `client/inventory.pyo`, `GetPurchasePriceForTabId`, source line 954: looks up
  the tab, unpacks `(price, size)` at offsets 31–40 and returns the price at 43–46.
- `PurchaseLockboxTab`, line 966: requires a known table key and sends
  `PurchaseLockboxTab` with one tuple field (offsets 32–47).
- `LockboxTabCanBePurchased`, line 998: requires the requested tab to be locked
  and its immediately preceding tab to be unlocked (offsets 0–39). The standard
  sequential unlock state therefore permits only the next tab.
- `LockboxTabIsLocked`, line 985: uses the server-provided availability dictionary.
  Missing keys default to available in this original predicate; that quirk is
  recorded, not used to invent additional purchasable IDs outside the table.
- `client/ui/lockboxwindow.pyo`, `OnAcceptTabPurchase`, line 326: requires an
  existing, living avatar (offsets 0–38), compares wallet `FUND` against the table
  price using `<` (39–69), and sends the purchase if sufficient (118–127).
  Insufficient funds play `UI_VENDOR_INSUFFIENCENT_CREDIT` and post
  `PM_INSUFFICIENT_FUND` locally (73–113).

Original member hashes:

- `client/inventory.pyo`: `fea5b3bc09103b261cdccd8b9396b0cef0e93c0b484fcb1039655da57990bd70`
- `client/ui/lockboxwindow.pyo`: `f4209434382cd297d70e2c31090ebff69869e3a99b56d65a41d188d35ce0e5ae`

Confidence is high for literal prices, sizes, the request shape and these client
admission predicates. There is no original server implementation or capture here.

## Correction and verification boundaries

The emulator previously passed a positive price to `LossCredits`; that helper
forwards a signed adjustment unchanged, so the purchase **added** money. It also
accepted arbitrary tab IDs and independently saved payment and unlock state.

The corrected handler uses the recovered prices, rejects dead avatars and
unknown tabs, and atomically deducts the wallet price and unlocks the next tab.
The repository verifies account ownership, affordability, expected wallet and
expected tab count in one serializable transaction. A stale shared-account
purchase, failed comparison or second-write failure rolls back payment. It
updates only the tab-count column in the bank row, preserving stored credits.
Session balances and permission notifications follow a committed transaction.

Tests cover each original price at exact affordability, the encoded five-tab
permission dictionary, insufficient funds, dead avatars, skipped/owned/invalid
tabs, stale wallet, stale tabs, wrong account, missing bank, late write failure
and two characters attempting to pay for the same shared tab. They inspect
reopened database state and verify that failures publish no partial update.

Existing 480-slot storage matches the table's total, but enforcement of owned
tab ranges across every item placement path still needs work. Original bank
access/proximity policy, exact server rejection messages, shared-account session
refresh and original bank-related credit delta fields remain unverified. The
client already supplies its own insufficient-funds feedback; this correction
adds no guessed server message. The existing zero credit delta is retained.
No prices, bank balances or previously corrupted purchases are retroactively
rewritten without transaction history that establishes the original values.
