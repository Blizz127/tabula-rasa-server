# Navmesh

Creature AI moves along a Detour navmesh, one per map, so creatures walk on the ground, around
rocks and buildings, and through caves instead of in straight lines at spawn height. The meshes
are built offline by `Rasa.NavMesh` from the client's own data and loaded by the game server at
startup. A map without a navmesh falls back to the old straight-line movement.

## Building the navmeshes

The tool reads a Tabula Rasa 1.16.5.0 install: `data/maps/<map>/<map>.map` for the placed
entities, `data/maps/<map>/t*_terrain.glm` for the heightmap, and `data/mesh*.glm` for the
collision geometry of every placed mesh (the `BVWS` walkable-surface and `BVBX` box volumes the
client itself collides with). `src/Rasa.NavMesh/data/entity_meshes.csv` maps entity class ids to
mesh files; it was exported from the client's `generated.client.entityclass` and
`generated.client.stringtable` tables.

```
dotnet run -c Release --project src\Rasa.NavMesh -- --client "C:\Games\Tabula Rasa" --out navmesh
```

Options: `--map <name>` (repeatable) builds only those maps; `--threads N`; `--terrain-step 2`
(heightmap samples per terrain quad; 1 uses every metre and quadruples the terrain triangles);
`--cell 0.4`, `--radius 0.6`, `--climb 0.9`, `--slope 50` change the Recast parameters;
`--obj` also writes the input geometry as `<map>.obj` for a mesh viewer. `data/terrain_cuts.csv` and
`data/map_build_settings.csv` are read on every build (see below).

A 2 km zone takes two to three minutes on four cores and produces an 11 MB `.nav`. Rebuild when
`entity_meshes.csv`, the build parameters, or the tool's geometry handling change; the client
data never does.

To check a result without starting the server:

```
dotnet run -c Release --project src\Rasa.NavMesh -- --path navmesh\adv_foreas_concordia_wilderness.nav  894.9 307.9 347.1  297.4 142.3 -580.9
```

prints the route from Alia Das to the Divide pass (or "PARTIAL" with where it stopped).

## Running the server with them

`GameDataConfig.NavMeshPath` in `appsettings.json` (default `navmesh`) is the folder, relative
to the server's working directory. The server logs how many maps got one. In game, a GM can run
`.navmesh` to see the mesh ground under their feet and `.navmesh path x y z` to see the route
the AI would take from where they stand.

## What goes into the mesh, and one thing that deliberately does not

Terrain triangles steeper than 60 degrees are left out before Recast sees them. The client
treats the heightmap as a surface to stand on, not a solid: cave mouths are cut into cliffs, and
the cliff face continues straight through the tunnel mesh behind it. Rasterized, that face would
wall the tunnel off a few metres in. Nobody can stand on a 60 degree face, so leaving it out
loses nothing; the walkable ground on either side still ends at a ledge.

## Terrain inside buildings sunk into it

The heightmap also runs on through the buildings and tunnels placed into a hill. On Pravus Research the facility
plateau is a flat 40.27 m, and the entrance ramp (`arch_bane_industrial_tunnel_ramp_01_32m_v01` at 208, 8, -8) climbs
from the interior at 8 m to the tunnel jamb at 41.6 m through it: under the terrain the ramp's top coil had less than an
agent height of room, so no path joined the entrance to the interior, and over the lower coils the terrain was a
floor floating in the tunnel. The client walks players through, so the terrain is not solid there.

`src/Rasa.NavMesh/data/terrain_cuts.csv` lists, per map, the mesh families whose inside the terrain must not cross
(`map,mesh_prefix,reason`). For those meshes only, `Rasa.ClientData.EnclosureIndex` leaves out every terrain triangle
whose centroid has one of their floors straight under it and one of their ceilings straight over it; the build log
says how many (273 on Pravus). The list is per map on purpose: over every placed mesh the same test also fires under
rock overhangs, trees and pond surfaces, where the terrain is real ground, and on Pravus it cut the AFS camp off from
the Frontlines. A map not in the list builds exactly as before. Pravus and Timora Mines are listed. Torcastra Prison
also has terrain inside tunnels but its probes are all joined; the Bane Conscript Facility carries the same ramp and the
Wardenbot Factory shows terrain inside buildings, unprobed (`GAP-NAVMESH-TERRAIN-CUTS`,
`docs/evidence/pravus-research-instance-20260927.json`). Crater
Lake's islands (the Corman HQ's upper floors, the greenhouse) are not this defect: its floors are above the terrain,
and cutting the terrain inside its `arch_` meshes leaves both probes where they were.

A prefix may name one placement, `mesh_prefix@x:z` (the entity within 1 m of x, z). Timora Mines needs it: the hillside
crosses the Fuel Egress chunnel entrance at 242, 202, -181, but the same entrance model at -144, 186, 280 has its awning
over open ground, and cutting there opens a hole in the apron that split Kearney's hall from the mine. Scoped to the one
placement the cut takes 22 triangles and changes no other island (`docs/evidence/divide-operations-instances-20260927.json`).

## A finer grid for one map

`src/Rasa.NavMesh/data/map_build_settings.csv` (`map,cell_size,cell_height,reason`) builds a listed map on a finer voxel
grid; the agent (radius, height, climb, slope) stays the same, so the geometry is only sampled more closely. Minos Caverns
is listed at 0.2 x 0.1 m: the cave has no terrain and is laid from cavern tiles whose collision floors overlap at the
joints, leaving walkable strips about 2 m wide that the 0.6 m agent radius erodes away at 0.4 m cells. At the default
grid the cave fell into islands the client walks straight through. The build log says when a map's grid comes from the
file. Other tile-built interiors were not re-probed at the finer grid (`GAP-NAVMESH-MAP-GRID`).

## Polygons under the terrain

Every polygon carries flag 0x01 (walkable). A polygon most of whose vertices are more than 2 m
below the terrain heightmap - the floor of a cave, tunnel or cellar the surface runs over - also
carries 0x02 (`NavMeshFlags.Underground`). The build log says how many; Wilderness has about
4,200 of 60,000. The server reads the flag under a player's feet to decide whether a region
volume marked underground-only or surface-only applies to them (see `regions.md`). Maps with no
terrain archive never get the flag. A `.nav` built before the flag existed still loads and paths
exactly as before; every player on it just counts as being on the surface, so rebuild to get the
cavern regions.

## Open: boot camp caldera trench (2026-09-28)

`GAP-BOOTCAMP-TRENCH-NAVMESH`. In `adv_bootcamp.nav`, paths from the caldera trench toward the AFS base end at
about (183, 108, 83). The mesh picks up again at about (177, 105, 92). The DIT boot camp pilot found this while
walking two recruits through the camp, and crosses the gap with a logged straight hop. The cause (a terrain cut,
the cell size, or an original drop) is not yet diagnosed. It needs a probe and either a rebuild or an off-mesh
link. Until 2026-09-28 the per-character boot camp copies had no navmesh at all (`MapChannelManager.PerCharacterInstance`
did not copy it), so this gap had never been reached in play.

## Open: the Divide north of Delta Outpost (2026-09-28)

`GAP-DIVIDE-DELTA-THORIA-NAVMESH`. DIT's population bots stall on a partial path at about (459, 85) (x, z), north of
Delta Outpost toward Thoria Das. The DIT squad-chat lane reported it. It is not yet probed.

## Format notes

`.glm` archives: chunks (zlib or raw), a name table, then a `CHNKBLXX` directory whose offset is
the file's last dword; entries are offset, compressed size, size, name offset, version,
timestamp. `.geo` meshes: a chunk tree (`GBOD` > `PSKE` > `PBON` > `BDAT` for bones and their
collision volumes; `GSKN` > `GPCE` > `INDX`/`VERT` for the render mesh). Terrain: 128 x 128
samples per tile, 12 bytes each, big-endian u16 height scaled to the map's max height. The
readers are in `src/Rasa.ClientData`.
