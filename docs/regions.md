# Regions

A region is the client's per-area atmosphere: ambient sound, music, sky, environment map, the
name that appears on screen when you enter, and for caverns their own minimap. Every `.map`
carries a table of them (1,343 across the 77 maps; Wilderness has 55) and the client applies the
highest-priority ones the server says the player is in, via `UpdateRegions(regionIdList)`. It
adds the map's default region 0 itself and ignores ids it does not know.

Which regions a player was in was decided by volumes on the original server, and those are gone.
The `map_region` table is the replacement: authored volumes, each naming one region id, checked
once a second per player by `RegionManager`. The set of matching region ids is compared with the
last one sent and `UpdateRegions` goes out on a change (and once on arriving on a map).

## Rows

| column | meaning |
| --- | --- |
| `map_context_id`, `region_id` | the map and the id in its region table (`regions.csv` next to WORLD_ZONES.md lists them with names) |
| `shape` | 1 circle: `radius` around (`pos_x`, `pos_z`); 2 box: `half_x` by `half_z` around it |
| `min_y`, `max_y` | the volume's height range; -2000..2000 means any |
| `underground` | 0 anywhere; 1 only while the player stands on navmesh flagged as under the terrain; 2 only while they do not |
| `enabled` | 0 keeps the row without applying it |
| `comment` | the map and region name |

Several rows may name the same region; a region is in the set when any of its enabled volumes
contains the player.

## The seed

The client knows two things about where regions are. Each `REGION_LABEL` marker on the map
screen names a region and gives the point its label is drawn at: 198 of them became circles,
surface only, with a radius of half the distance to the nearest other label on the map,
clamped to 60-200 m. Each region with its own minimap gives the rectangle that minimap covers
(`gamecontextuiradarinfo`; the radar offset's z is negated to get world z): 175 of them became
boxes, underground-only where the walkable floor inside the rectangle is mostly under the
terrain (51, the caverns), anywhere otherwise (nested base floors, settlements with a detailed
minimap, instance maps). Regions with neither - the unnamed ambience zones, the caverns without
a minimap such as Alia Caverns (32) and Enigma Cavern (36) in Wilderness - have no volume yet.

That is an approximation in both directions: a label circle is not the region's real outline,
and a cavern rectangle covers the whole cave system as one region. It is what makes the caverns
dark and quiet and swaps the minimap, and the names appear where they should most of the time.
Fix the rest in game.

## In game

`.regions` (Observer) says whether you count as underground, which region ids you are being
sent, and lists the map's volumes nearest first, marking the ones you are in.

`.region` (GameMaster) edits them; every change is written to the table and applied at once:

```
.region here 32 40 Alia Caverns          circle of 40 m at your feet for region 32
.region box 32 60 45 Alia Caverns        box of 120 x 90 m around your feet
.region 380 here                         move #380's centre to your feet
.region 380 radius 55                    (turns a box into a circle)
.region 380 size 70 40                   (turns a circle into a box)
.region 380 y 240 290                    only between those heights
.region 380 underground 1                0 anywhere, 1 underground only, 2 surface only
.region 380 region 36                    point it at another region id
.region 380 disable | enable | comment text | delete
```

`.setregion 32 36` forces a list on your own client and holds it, so a region's ambience and
minimap can be checked without a volume; `.setregion off` hands control back. Leaving the map
does too.

To author a cavern: walk in, check `.regions` says underground, `.region here <id> <radius>`
then `.region <id> underground 1`. A cavern whose rectangle also catches the hillside above it is
either underground-only already or needs `.region <id> underground 1`. A named place whose label
circle is too big or off-centre: `.region <id> here` from where its centre should be and
`.region <id> radius`.

## Underground

"Underground" comes from the navmesh: `Rasa.NavMesh` flags every polygon most of whose vertices
lie more than 2 m below the terrain heightmap, and `NavMeshQuery.IsUnderground` reads the flag
of the polygon nearest the player. It needs `.nav` files built with that version of the tool;
older ones load fine and report everyone as on the surface, which disables the underground-only
volumes and leaves the surface-only ones always on. Maps without a terrain archive have no
underground.
