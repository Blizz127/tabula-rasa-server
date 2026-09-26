#!/usr/bin/env python3
"""Losslessly rewrite/verify the original C# item-template/class seed.

Hash encoding: ordered pairs of little-endian signed Int32 values, no header.
Accepts the historical yield format and the replacement primitive-array format.
"""
import argparse
import hashlib
import json
import re
import struct
from pathlib import Path


def pairs(source):
    if "yield return new object[] {" in source and "private static readonly int[] Rows" not in source:
        rows = re.findall(r"yield return new object\[\] \{ (\d+), (\d+) \};", source)
        assert len(rows) == source.count("yield return"), "Unrecognized row or numeric type"
        return [(int(a), int(b)) for a, b in rows]
    body = re.search(r"private static readonly int\[\] Rows\s*=\s*\{(.*?)\};", source, re.S).group(1)
    assert re.fullmatch(r"[\d,\s]+", body), "Unrecognized primitive-array data"
    numbers = [int(n) for n in re.findall(r"\d+", body)]
    assert len(numbers) % 2 == 0
    return list(zip(numbers[::2], numbers[1::2]))


def digest(rows):
    return hashlib.sha256(b"".join(struct.pack("<ii", *row) for row in rows)).hexdigest()


parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("source", type=Path)
parser.add_argument("--convert", action="store_true")
args = parser.parse_args()
source = args.source.read_text(encoding="utf-8-sig")
before = pairs(source)
if args.convert:
    assert "private static readonly int[] Rows" not in source, "Already converted"
    start = source.index("        protected override IEnumerable<object[]> GetRows()")
    source = source[:start] + '''        // The 30,225 literal yield cases generated an enormous MoveNext method.
        // Store the same Int32 pairs as a data blob and box only the current row.
        // Ordered-pair hash and reproducible conversion: docs/evidence/item-class-seed-parity.json.
        protected override IEnumerable<object[]> GetRows()
        {
            for (var index = 0; index < Rows.Length; index += 2)
                yield return new object[] { Rows[index], Rows[index + 1] };
        }

        private static readonly int[] Rows =
        {
''' + "".join(f"            {a}, {b},\n" for a, b in before) + '''        };
    }
}
'''
    assert pairs(source) == before, "Conversion changed seed values/order"
    args.source.write_text(source, encoding="utf-8-sig")
after = pairs(args.source.read_text(encoding="utf-8-sig"))
assert before == after
print(json.dumps({"rows": len(after), "columns": 2, "boxed_type": "System.Int32",
                  "encoding": "ordered little-endian signed Int32 pairs; no header",
                  "before_sha256": digest(before), "after_sha256": digest(after)}, indent=2))
