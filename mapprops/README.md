# Map props

The furniture the client's own map files place: the small objects a body can end up standing inside.

Each file is one map, named after its `map_info.map_name`, decoded from that map's `.map` file in the client
(format 2.43-2.45, read with the grammar of the client's own loader `client/gamemap.pyo`). Only props that are
furniture-sized are kept - a horizontal reach of 3 m or less and a height between 0.15 m and 2 m - because the
purpose of the data is `PropOverlapAuditTests`, which checks that no NPC stands inside a cot, a console, a
workstation or an armour mannequin. Buildings, tents, walls and terrain are deliberately left out: an NPC is
meant to stand inside those.

Columns are the prop's class id, its world position, its rotation about Y in radians (the maps rotate about Y
only), its object-local box in X and Z, and the top of that box. A body at (bx, bz) is inside the prop when the
point, rotated into the prop's frame, falls in [minX, maxX] x [minZ, maxZ] and the prop's top stands at least
0.15 m above the body's feet.

31,380 props over 72 maps. Static decode of client 1.16.5.0 data; nothing from the game is executed.
