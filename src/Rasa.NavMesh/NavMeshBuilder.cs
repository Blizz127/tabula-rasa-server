using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotRecast.Core.Numerics;
using DotRecast.Detour;
using DotRecast.Recast;
using DotRecast.Recast.Geom;
using Rasa.ClientData;

namespace Rasa.NavMesh
{
    /// <summary>
    /// Turns a map's triangle soup into a tiled Detour navmesh, the way recastnavigation's tiled
    /// sample does it: Recast rasterizes each tile (heightfield, regions, contours, poly mesh, detail
    /// mesh), Detour packs the tile, all tiles go into one <see cref="DtNavMesh"/>.
    /// </summary>
    public static class NavMeshBuilder
    {
        // Area and flag values written into the polygons. The server's query filter includes everything.
        public const int AreaGround = 0;
        public const int AreaWalkableInput = 0x3f;
        public const int FlagWalk = 0x01;

        public static DtNavMesh Build(MapGeometry geometry, BuildSettings s, Action<string> log)
        {
            var geom = new RcSampleInputGeomProvider(geometry.Vertices.ToArray(), geometry.Triangles.ToArray());

            var cfg = new RcConfig(
                true, s.TileSize, s.TileSize,
                RcConfig.CalcBorder(s.AgentRadius, s.CellSize),
                RcPartition.WATERSHED,
                s.CellSize, s.CellHeight,
                s.AgentMaxSlope, s.AgentHeight, s.AgentRadius, s.AgentMaxClimb,
                s.RegionMinSize * s.RegionMinSize * s.CellSize * s.CellSize,
                s.RegionMergeSize * s.RegionMergeSize * s.CellSize * s.CellSize,
                s.EdgeMaxLength, s.EdgeMaxError,
                s.VertsPerPoly,
                s.DetailSampleDistance, s.DetailSampleMaxError,
                true, true, true,
                new RcAreaModification(AreaWalkableInput), true);

            var bmin = geom.GetMeshBoundsMin();
            var bmax = geom.GetMeshBoundsMax();
            RcRecast.CalcGridSize(bmin, bmax, s.CellSize, out var gridW, out var gridH);
            var tilesX = (gridW + s.TileSize - 1) / s.TileSize;
            var tilesZ = (gridH + s.TileSize - 1) / s.TileSize;
            log($"  grid {gridW} x {gridH} cells, {tilesX} x {tilesZ} tiles of {s.TileSize * s.CellSize:0.#} m");

            // Recast's detail-mesh code reports every dangling Delaunay face on the console; on a
            // 2 km map that is thousands of lines saying nothing actionable.
            var console = Console.Out;
            Console.SetOut(System.IO.TextWriter.Null);
            List<RcBuilderResult> results;

            try
            {
                results = new RcBuilder().BuildTiles(geom, cfg, false, true, Math.Max(1, s.Threads), Task.Factory);
            }
            finally
            {
                Console.SetOut(console);
            }

            var tileBits = Math.Min(DtUtils.Ilog2(DtUtils.NextPow2(tilesX * tilesZ)), 14);
            var navMeshParams = new DtNavMeshParams
            {
                orig = bmin,
                tileWidth = s.TileSize * s.CellSize,
                tileHeight = s.TileSize * s.CellSize,
                maxTiles = 1 << tileBits,
                maxPolys = 1 << (22 - tileBits)
            };

            var navMesh = new DtNavMesh();
            navMesh.Init(navMeshParams, s.VertsPerPoly);

            var added = 0;
            var polys = 0;

            foreach (var result in results)
            {
                var pmesh = result.Mesh;

                if (pmesh == null || pmesh.npolys == 0)
                    continue;

                for (var i = 0; i < pmesh.npolys; i++)
                {
                    if (pmesh.areas[i] == AreaWalkableInput)
                        pmesh.areas[i] = AreaGround;

                    pmesh.flags[i] = FlagWalk;
                }

                var option = new DtNavMeshCreateParams
                {
                    verts = pmesh.verts,
                    vertCount = pmesh.nverts,
                    polys = pmesh.polys,
                    polyAreas = pmesh.areas,
                    polyFlags = pmesh.flags,
                    polyCount = pmesh.npolys,
                    nvp = pmesh.nvp,
                    walkableHeight = s.AgentHeight,
                    walkableRadius = s.AgentRadius,
                    walkableClimb = s.AgentMaxClimb,
                    bmin = pmesh.bmin,
                    bmax = pmesh.bmax,
                    cs = s.CellSize,
                    ch = s.CellHeight,
                    buildBvTree = true,
                    tileX = result.TileX,
                    tileZ = result.TileZ,
                    offMeshConCount = 0,
                    offMeshConVerts = Array.Empty<float>(),
                    offMeshConRad = Array.Empty<float>(),
                    offMeshConDir = Array.Empty<int>(),
                    offMeshConAreas = Array.Empty<int>(),
                    offMeshConFlags = Array.Empty<int>(),
                    offMeshConUserID = Array.Empty<int>()
                };

                var dmesh = result.MeshDetail;

                if (dmesh != null)
                {
                    option.detailMeshes = dmesh.meshes;
                    option.detailVerts = dmesh.verts;
                    option.detailVertsCount = dmesh.nverts;
                    option.detailTris = dmesh.tris;
                    option.detailTriCount = dmesh.ntris;
                }

                var data = DtNavMeshBuilder.CreateNavMeshData(option);

                if (data == null)
                    continue;

                if (navMesh.AddTile(data, 0, 0, out _).Succeeded())
                {
                    added++;
                    polys += pmesh.npolys;
                }
            }

            log($"  {added} tiles with walkable surface, {polys} polygons");

            return navMesh;
        }
    }
}
