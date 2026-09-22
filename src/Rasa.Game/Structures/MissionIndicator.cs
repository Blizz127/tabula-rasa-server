using System.Numerics;

namespace Rasa.Structures
{
    using Content;

    public class MissionIndicator
    {
        public Vector3 Position { get; set; }
        public double Radius { get; set; }

        /// <summary>
        /// The client's missionobjectiveindicator label for the marker's map tooltip, or null for no label.
        /// mapwindow.OnMissionMarkerHighlighted (line 797 of the 1.16.5.0 disassembly) reads
        /// BuildObjectiveIndicator(indicatorId) when this is not None and BuildObjectiveNameText(missionId,
        /// objectiveId) when it is, so null means the marker is captioned with the objective's own text.
        /// </summary>
        public uint? IndicatorId { get; set; }

        public bool Show3DEffect { get; set; }

        /// <summary>
        /// The map this marker belongs to, or 0 when it is not known. The client's indicator tuple carries no
        /// map and mapwindow._PlaceWidget uses whatever map is open, so an indicator is only sent to a player
        /// standing on its own map (PlayerMission.ToMissionInfo).
        /// </summary>
        public uint MapContextId { get; set; }

        /// <summary>Where the position was read from. Server-side diagnostic; nothing of it reaches the client.</summary>
        public MissionIndicatorSource Source { get; set; } = MissionIndicatorSource.Stored;
    }
}
