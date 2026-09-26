# Character creation appearance, 2026-09-22

Creation and cloning now validate the appearance selections offered by the
original compatibility client. Previously, any world item template could be
inserted into any appearance slot, cross-race faces were accepted, mandatory
hair/face could be missing, and malformed color channels wrapped into bytes.
An unknown template could also reach a database lookup and throw during creation.

The restored contract requires Hair and Face and allows optional Eyewear and,
for Humans, Beard/accessory. Bald is a real Hair selection, template 60/class 3812, including for all
three hybrids. Submitted entries must match their dictionary slot and the
original template's slot/race eligibility; clothing and weapon slots cannot be
supplied by the creation request. Colors must be present and opaque. Invalid
selections fail before character, family, item or clone-credit changes.

## Original client evidence

The [machine-readable manifest](evidence/character-creation-appearance.json)
records every field of all 92 selectable template rows, exact source table keys,
source hashes, compiled timestamps, bytecode locations and remaining gaps. Data
come from compatibility client 1.16.5.0; its exact equivalence to the shutdown
revision remains unverified. The original bytecode was inspected, not executed.

`client/ui/charactercreationwindow.pyo` establishes:

- `Init`, original line 107, offsets 2199–2389, limits selectable slots to Hair14,
  Face17, Eyewear19 and Beard20. It joins `starterinfo.starterItemTemplate` through
  `starterItemTemplateClassIds` to `equipmentdata.equipableClassEquipmentSlot`.
  Three Recruit clothing entries in the 95-row starter catalog are excluded.
- `_AddItemAsChoice`, line 1151, offsets 0–104, reads
  `itemclass.itemTemplateRaceRequirement`. A missing requirement adds a choice
  to every race; a present requirement adds it only to that race.
- `__init__`, line 76, offsets 204–474, initializes Hair/Face choice lists without
  a `None` option. Only Eyewear/Beard lists start with `None`. `_CreateCharacter`,
  line 694, offsets 219–313, omits only selections whose template is `None`.
- `_UpdateGenderToggles`, line 948, offsets 266–357, changes the female Beard
  label to Accessory. It does not remove those choices. `_InitMannequin`, line
  863, offsets 30–59, selects the male/female manifestation body. Shared template
  choices must therefore remain valid for both genders; a beard-only male filter
  would change original behavior.
- `_UpdateRaceToggles`, line 980, offsets 164–328, resets Beard to `None` and Hair
  to bald for non-Humans, then disables both groups. This overrides the otherwise
  unrestricted Beard template race metadata. Admission rejects a Beard/accessory
  entry for hybrids; Eyewear remains available. This constraint was discovered
  during the subsequent palette audit and corrects the initial table-only rule.

`client/ui/colorwheelpicker.pyo`, `_SetColorWheelPicker`, line 121, offsets
289–345, obtains the selected RGBA pixel and rejects alpha other than 255. The
creation window's defaults (offsets 396–465) also all have alpha255. Creation
appearance parsing now requires its two-field entry tuple and four-channel color
tuple, accepts integer/long channel representations, and rejects out-of-range
channels rather than truncating them. The unrelated general `Color` parser was
left unchanged.

The frozen original template-to-class mapping is used when saving appearance.
A conflicting world-database mapping cannot silently replace original creation
assets. This is a catalog of character customization choices, not an inventory
reward list. The existing server-controlled Recruit outfit remains separate.

## Validation and limits

Tests compare every catalog entry against the provenance manifest across all
four races; exercise wire tuple/channel boundaries; and verify that missing,
misplaced, unknown, cross-race, null or transparent selections cannot create a
character or consume clone credits. Fixtures now send original-valid Hair/Face
choices, and a female accessory test verifies shared templates persist correctly.
Original-client visual/session comparison remains necessary.

RGB membership is not yet enforced. The five original textures and exact native
sampling routines have now been recovered; the remaining limits are described
below. No arbitrary RGB range or gender restriction is invented. The precise original
server response to malformed appearance packets is unknown; `InvalidEncoding`
uses the existing protocol error and is labelled an inference.

The pre-existing DIT bot creator needed a small compatibility update: it now
supplies valid Human Hair/Face selections before native creation, then preserves
its existing source-character appearance-copy workflow using `AddOrUpdate` so
the newly required slots are replaced instead of duplicated. That developer
fixture remains separate from retail progression rules.

## Palette recovery and native sampling

[Palette provenance](evidence/character-creation-palettes.json) records the exact
DDS offsets/hashes in `data/ui.glm`, the original executable hash and native
addresses, XML layout, Python consumers, decoded hashes and remaining gaps.
[The recovery script](evidence/character-creation-palettes.recover.py) uses only
Python's standard library and accepts an original client directory and output
directory. It extracts the five DDS files and reproduces the native decoder;
no client code is executed by that script.

The final compatibility executable's `UIBitmap` RTTI/vtable resolves
`GetPixelColor` to VA `0x008f4de0` and `FindPixelForColor` to `0x008f4f40`.
For stretched textures, the getter truncates `coordinate / widgetSize * regionSize`
and samples that texel without blending. The creation XML defines a 156×156
wheel displaying a 256×256 region. Python applies an eight-pixel buffer and
radius 70, rejects alpha other than 255, and rejects RGB black. The picker uses
the Standard texture for Hair, Beard and Eyewear, and a separate skin texture
for each race's Face.

Standard is raw ARGB32; skins are DXT5. The native DXT routine at VA `0x006fa2a0`
expands RGB565 endpoints by bit replication, then truncates each weighted RGB
term separately. For example, an interpolated channel is
`floor(2*A/3) + floor(B/3)`, rather than `floor((2*A+B)/3)`.
Alpha interpolation adds 3 before division by 7, or 2 before division by 5.
These original rounding details must be retained. Pillow's ordinary DDS decoder
differs on 18,376–23,527 texels per skin texture.

The independent reconstruction was compared against every pixel returned by
isolated x86 emulation of the original pure native routine: all 262,144 skin
texels match exactly. That experiment mapped the executable bytes and synthetic
texture/stack memory into Unicorn; it did not run the game, its entry point,
imports, network or operating-system calls. Research scripts and disassembly
are retained under the artifact paths in the manifest.

The XML-derived 156-pixel mouse grid yields these candidate color counts:

| Palette | Distinct opaque, nonblack texture colors | 156-pixel grid colors |
| --- | ---: | ---: |
| Standard | 37,815 | 13,972 |
| Human | 3,367 | 2,979 |
| Forean | 3,727 | 3,163 |
| Brann | 3,882 | 3,221 |
| Thrax | 3,905 | 3,179 |

Hair/Beard default `(82,52,32,255)` and Eyewear default `(168,140,66,255)` exist
in the Standard texture but are absent from that mouse grid. All four skin
defaults occur in their respective grid. `Show` finds an exact texture match and
positions the selector, but does not invoke the color-change callback; opening
the picker therefore does not replace the stored default. An admission check
must explicitly retain defaults instead of requiring every color to come from
a mouse click.

Cloning has another relevant original quirk: `_ResetSelections` initially copies
Hair, Face and Beard colors from the source pod, but `Show` subsequently calls
`_UpdateRaceToggles`, resetting Face to its race default and hybrid Beard to
`None`. Human Hair/Beard inherited colors survive. Eyewear starts omitted with
its default color. Future RGB validation must preserve inherited Human colors,
including valid colors from earlier original versions or customization paths.

The grid sets remain research artifacts, not enforced server rules. The native
screen-to-widget input/scaling path across supported resolutions and texture
resource format fallback still need original-client runtime verification. The
original server's RGB rejection behavior also lacks a packet capture. Tests
for the separate hybrid Beard correction cover Human and all three hybrids;
the palette work adds no .NET build or live-database change.
