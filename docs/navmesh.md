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
`--obj` also writes the input geometry as `<map>.obj` for a mesh viewer.

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

## Format notes

`.glm` archives: chunks (zlib or raw), a name table, then a `CHNKBLXX` directory whose offset is
the file's last dword; entries are offset, compressed size, size, name offset, version,
timestamp. `.geo` meshes: a chunk tree (`GBOD` > `PSKE` > `PBON` > `BDAT` for bones and their
collision volumes; `GSKN` > `GPCE` > `INDX`/`VERT` for the render mesh). Terrain: 128 x 128
samples per tile, 12 bytes each, big-endian u16 height scaled to the map's max height. The
readers are in `src/Rasa.ClientData`.
