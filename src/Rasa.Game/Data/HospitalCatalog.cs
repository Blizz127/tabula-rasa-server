using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Data
{
    public sealed class HospitalData
    {
        /// <summary>graveyardlanguage key: the id PlayerDead advertises and ReviveMe returns.</summary>
        public int GraveyardId { get; }

        /// <summary>waypointlanguage key: the id GraveyardGained announces and the character persists.</summary>
        public uint WaypointId { get; }

        public uint MapContextId { get; }

        /// <summary>Original uimapmarker.factionedmarkers entity id of the HOSPITAL/SAFE_ZONE marker.</summary>
        public ulong MarkerEntityId { get; }

        /// <summary>The marker's original coordinates, used as the respawn point (see docs/evidence/hospital-catalog.json).</summary>
        public Vector3 Position { get; }

        /// <summary>GraveyardInfo isSafe: selects the client's icon only; it does not gate the destination.</summary>
        public bool IsSafe { get; }

        /// <summary>Offered only while its control point is AFS-held (official control-point guide).</summary>
        public bool IsControlPoint { get; }

        /// <summary>Offered without a GraveyardGained discovery (no gain line in the boot-camp footage).</summary>
        public bool KnownWithoutDiscovery { get; }

        internal HospitalData(int graveyardId, uint waypointId, uint mapContextId, ulong markerEntityId,
            Vector3 position, bool isSafe, bool isControlPoint, bool knownWithoutDiscovery)
        {
            GraveyardId = graveyardId;
            WaypointId = waypointId;
            MapContextId = mapContextId;
            MarkerEntityId = markerEntityId;
            Position = position;
            IsSafe = isSafe;
            IsControlPoint = isControlPoint;
            KnownWithoutDiscovery = knownWithoutDiscovery;
        }
    }

    /// <summary>
    /// Hospitals of the starting areas, joined across the original client's graveyardlanguage,
    /// waypointlanguage and uimapmarker tables by their identical names. Every field's tier and
    /// citation is recorded in docs/evidence/hospital-catalog.json, which HospitalCatalogTests
    /// compares against these rows.
    /// </summary>
    public static class HospitalCatalog
    {
        /// <summary>
        /// Horizontal distance from a hospital within which it is gained. Inferred lower bound:
        /// "You just gained Alia Das Hospital." was already in chat in the first frame at Alia Das,
        /// measured 98.9 m from the hospital marker (footage B2-019). The original trigger is
        /// unrecovered.
        /// </summary>
        public const float DiscoveryRadius = 100f;

        public static IReadOnlyList<HospitalData> Entries { get; } = Array.AsReadOnly(new[]
        {
            // Boot camp (context 1985, map template 1985).
            new HospitalData(20000001, 500, 1985, 134419591469927UL,
                new Vector3(357.9005432128906f, 120.32544708251953f, 156.51882934570312f), false, false, true),

            // Wilderness (context 1220, map template 1378).
            new HospitalData(3, 103, 1220, 133079561961146UL,
                new Vector3(787.0088500976562f, 294.3324279785156f, 366.5991516113281f), false, false, false),
            new HospitalData(5, 106, 1220, 133079561961147UL,
                new Vector3(-693.8690185546875f, 170.1329803466797f, -342.3608093261719f), false, false, false),
            new HospitalData(6, 105, 1220, 133079561961145UL,
                new Vector3(-125.88742065429688f, 220.74998474121094f, -468.61846923828125f), true, false, false),
            new HospitalData(20, 108, 1220, 133079561961149UL,
                new Vector3(-654.5541381835938f, 283.6459655761719f, 882.8185424804688f), false, false, false),
            new HospitalData(122, 218, 1220, 133079561961338UL,
                new Vector3(-330.0f, 172.7716827392578f, -532.0f), false, true, false),
            new HospitalData(136, 216, 1220, 133079561962699UL,
                new Vector3(156.02780151367188f, 163.0747833251953f, -86.59078979492188f), false, true, false)
        });

        public static IEnumerable<HospitalData> ForMap(uint mapContextId)
            => Entries.Where(hospital => hospital.MapContextId == mapContextId);
    }
}
