#!/usr/bin/env python3
"""Check the deployed speaker classes and estimated outfits against their manifests."""

import json
import sqlite3
import sys
from pathlib import Path


root = Path(__file__).resolve().parent
db = Path(sys.argv[1]) if len(sys.argv) > 1 else root.parent.parent / "rasaworld.db"
manifest = json.loads((root / "mission-speaker-dialogue-classes.json").read_text())
moawi = json.loads((root / "moawi-dialogue-class.json").read_text())

with sqlite3.connect(db) as connection:
    row = connection.execute("SELECT class_id FROM creature WHERE id = 38").fetchone()
    assert row == (moawi["value"],), ("Moawi", row)

    for group_name in ("human_speakers", "brann_speakers"):
        group = manifest[group_name]
        expected = {(row["slot_id"], row["class_id"], row["color"])
                    for row in group["appearance"]["rows"]}
        for creature_id in group["creature_ids"]:
            row = connection.execute("SELECT class_id FROM creature WHERE id = ?", (creature_id,)).fetchone()
            assert row == (group["class_id"]["value"],), (creature_id, "class", row)
            actual = set(connection.execute(
                "SELECT slot_id, class_id, color FROM creature_appearance WHERE id = ?", (creature_id,)))
            assert actual == expected, (creature_id, "appearance", actual, expected)

print("13 mission speaker classes and 12 appearance sets match their manifests")
