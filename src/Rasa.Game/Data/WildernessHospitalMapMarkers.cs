using System;
using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Data
{
    public enum HospitalMapMarkerType
    {
        Hospital = 3,
        SafeZone = 19
    }

    public sealed class HospitalMapMarkerData
    {
        public ulong MarkerEntityId { get; }
        public HospitalMapMarkerType MarkerType { get; }
        public uint NameTextId { get; }
        public uint? TooltipTextId { get; }
        public Vector3 Position { get; }

        internal HospitalMapMarkerData(ulong markerEntityId, HospitalMapMarkerType markerType,
            uint nameTextId, uint? tooltipTextId, Vector3 position)
        {
            MarkerEntityId = markerEntityId;
            MarkerType = markerType;
            NameTextId = nameTextId;
            TooltipTextId = tooltipTextId;
            Position = position;
        }
    }

    // Original 1.16.5.0 uimapmarker.factionedmarkers[1378], joined through
    // gamecontext.lookup[1220]. These are hospital MAP MARKERS, not verified
    // respawn positions. Marker entity/text IDs are not graveyard or waypoint IDs.
    // Runtime ownership, acquisition and PvP safety are separate server state.
    // See docs/hospital-recovery-evidence.md for original hashes and limitations.
    public static class WildernessHospitalMapMarkers
    {
        public const uint GameContextId = 1220;
        public const uint MapTemplateId = 1378;

        public static IReadOnlyList<HospitalMapMarkerData> Entries { get; } = Array.AsReadOnly(new[]
        {
            new HospitalMapMarkerData(133079561961145UL, HospitalMapMarkerType.SafeZone, 229, 229,
                new Vector3(-125.88742065429688f, 220.74998474121094f, -468.61846923828125f)),
            new HospitalMapMarkerData(133079561961146UL, HospitalMapMarkerType.Hospital, 230, 230,
                new Vector3(787.0088500976562f, 294.3324279785156f, 366.5991516113281f)),
            new HospitalMapMarkerData(133079561961147UL, HospitalMapMarkerType.Hospital, 231, 231,
                new Vector3(-693.8690185546875f, 170.1329803466797f, -342.3608093261719f)),
            new HospitalMapMarkerData(133079561961149UL, HospitalMapMarkerType.Hospital, 232, 232,
                new Vector3(-654.5541381835938f, 283.6459655761719f, 882.8185424804688f)),
            new HospitalMapMarkerData(133079561961338UL, HospitalMapMarkerType.Hospital, 306, null,
                new Vector3(-330.0f, 172.7716827392578f, -532.0f)),
            new HospitalMapMarkerData(133079561962699UL, HospitalMapMarkerType.Hospital, 303, null,
                new Vector3(156.02780151367188f, 163.0747833251953f, -86.59078979492188f))
        });
    }
}
