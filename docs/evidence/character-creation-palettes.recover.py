#!/usr/bin/env python3
"""Recover original DDS palettes, preserving final executable's integer rounding.

Usage: python3 character-creation-palettes.recover.py CLIENT_DIRECTORY OUTPUT_DIRECTORY
Does not execute client code. Its DXT5 output was exhaustively compared against
isolated x86 emulation of tabula_rasa.exe VA 0x006fa2a0 (see adjacent manifest).
The 156px color sets are layout-derived candidates, not server admission data.
"""
import argparse
import hashlib
import json
from pathlib import Path
import struct

NAMES = ['palette_standard.dds'] + [f'palette_skintone_{r}.dds' for r in ('human', 'forean', 'brann', 'thrax')]
DEFAULTS = {'palette_standard.dds': [(82, 52, 32, 255), (168, 140, 66, 255)],
            'palette_skintone_human.dds': [(214, 178, 132, 255)],
            'palette_skintone_forean.dds': [(99, 113, 90, 255)],
            'palette_skintone_brann.dds': [(82, 125, 181, 255)],
            'palette_skintone_thrax.dds': [(115, 56, 41, 255)]}


def sha(data):
    return hashlib.sha256(data).hexdigest()


def rgb565(value):
    r, g, b = value >> 11, (value >> 5) & 63, value & 31
    return (r << 3) | (r >> 2), (g << 2) | (g >> 4), (b << 3) | (b >> 2)


def decode(data):
    assert data[:4] == b'DDS ' and struct.unpack_from('<II', data, 12) == (256, 256)
    fourcc = data[84:88]
    if fourcc == b'\0\0\0\0':
        assert struct.unpack_from('<IIIII', data, 88) == (32, 0xff0000, 0xff00, 0xff, 0xff000000)
        return bytes(channel for i in range(128, len(data), 4) for channel in (data[i + 2], data[i + 1], data[i], data[i + 3]))
    assert fourcc == b'DXT5'
    result = bytearray(256 * 256 * 4)
    for by in range(64):
        for bx in range(64):
            offset = 128 + (by * 64 + bx) * 16
            a0, a1 = data[offset:offset + 2]
            alpha_indices = int.from_bytes(data[offset + 2:offset + 8], 'little')
            c0, c1, color_indices = struct.unpack_from('<HHI', data, offset + 8)
            colors = [rgb565(c0), rgb565(c1)]
            # Original native decoder truncates each weighted term separately.
            colors += [tuple(2 * a // 3 + b // 3 for a, b in zip(colors[0], colors[1])),
                       tuple(a // 3 + 2 * b // 3 for a, b in zip(colors[0], colors[1]))]
            if a0 > a1:
                alphas = [a0, a1] + [((8 - i) * a0 + (i - 1) * a1 + 3) // 7 for i in range(2, 8)]
            else:
                alphas = [a0, a1] + [((6 - i) * a0 + (i - 1) * a1 + 2) // 5 for i in range(2, 6)] + [0, 255]
            for i in range(16):
                pixel = ((by * 4 + i // 4) * 256 + bx * 4 + i % 4) * 4
                result[pixel:pixel + 4] = bytes((*colors[(color_indices >> (2 * i)) & 3], alphas[(alpha_indices >> (3 * i)) & 7]))
    return bytes(result)


def recover(client, output):
    output.mkdir(parents=True, exist_ok=True)
    glm = (client / 'data/ui.glm').read_bytes()
    header = struct.unpack_from('<I', glm, len(glm) - 4)[0]
    assert glm[header:header + 4] == b'CHNK'
    strings, size, count = struct.unpack_from('<III', glm, header + 8)
    names = glm[strings:strings + size].rstrip(b'\0').decode().split('\0')
    records = []
    for index, name in enumerate(names):
        if name not in NAMES:
            continue
        offset, packed, unpacked, modified, scheme, pack = struct.unpack_from('<IIIIHI', glm, header + 20 + index * 22)
        assert packed == unpacked and scheme == 0
        dds = glm[offset:offset + packed]
        rgba = decode(dds)
        (output / name).write_bytes(dds)
        (output / (name + '.native.rgba')).write_bytes(rgba)
        pixels = [tuple(rgba[i:i + 4]) for i in range(0, len(rgba), 4)]
        opaque = {p for p in pixels if p[3] == 255 and p[:3] != (0, 0, 0)}
        # Every interior integer coordinate can be clicked directly. The Python
        # radial clamp truncates toward the center and cannot add exterior points.
        candidates = {pixels[(y * 256 // 156) * 256 + x * 256 // 156]
                      for y in range(8, 149) for x in range(8, 149)
                      if (x - 78) ** 2 + (y - 78) ** 2 <= 70 ** 2} & opaque
        packed_candidates = bytes(channel for color in sorted(candidates) for channel in color)
        (output / (name + '.156px-candidates.rgba')).write_bytes(packed_candidates)
        records.append({'member': name, 'glm_offset': offset, 'bytes': packed, 'sha256': sha(dds),
                        'native_rgba_sha256': sha(rgba), 'unique_opaque_nonblack': len(opaque),
                        'layout_156px_candidate_count': len(candidates), 'layout_156px_candidates_sha256': sha(packed_candidates),
                        'defaults': [{'rgba': c, 'in_156px_candidates': c in candidates, 'in_texture': c in pixels} for c in DEFAULTS[name]]})
    assert len(records) == 5
    report = {'ui_glm_sha256': sha(glm), 'members': records}
    (output / 'recovered-palettes.json').write_text(json.dumps(report, indent=2) + '\n')
    return report


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('client', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    print(json.dumps(recover(args.client, args.output), indent=2))
