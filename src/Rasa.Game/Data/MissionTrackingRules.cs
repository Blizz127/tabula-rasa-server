using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Data
{
    using Structures;

    /// <summary>
    /// Which missions the on-screen mission tracker shows. Recovered 1.16.5.0 client:
    ///
    ///  - client/ui/missiontracker.py:95 (_UpdateTrackedMissions, offset 61) draws exactly
    ///    gameui.GetMissionTracking(), which returns the client-local list g_MissionTrackingList
    ///    (client/gameui.py:1747). Nothing else gates what the tracker displays.
    ///  - client/missionlog.py:286 (Recv_MissionGained, offset 22) already calls
    ///    gameui.AddMissionTracking(missionId, bAnnounce = False), so the MissionGained the server
    ///    sends on accept tracks the mission for the rest of the session by itself.
    ///  - The only other writer is the log window's tracking checkbox
    ///    (client/ui/currentmissionswindow.py:317, OnTrackingBtnToggled, offsets 81 and 114:
    ///    gameui.AddMissionTracking / gameui.RemoveMissionTracking). It sends no packet, and
    ///    generated/client/methodid.py has no TrackMission / SetMissionTracked / MissionTracking
    ///    method at all: tracking is never a field on the wire.
    ///  - The list outlives the moment only as character options
    ///    Character.UserInterface.MissionTrack0..29 - client/gameui.py:1777 LoadMissionTrackingData
    ///    reads them through the options manager, :1803 SaveMissionTrackingData writes them back,
    ///    and kMaxMissionTrackCount is 30 (client/gameui.py:130). Those options are the pair the
    ///    server already stores and echoes (CharacterOptions 692 / SaveCharacterOptions 693).
    ///
    /// LoadMissionTrackingData empties g_MissionTrackingList before refilling it from the options,
    /// and the server re-sends CharacterOptions on every map change as well as at login
    /// (ManifestationManager.AssignPlayer), so an accept the server never wrote into the slots is
    /// back to untracked at the next zone. Defaulting the accept to tracked therefore means writing
    /// the slots; the server writes them only on accept, so a later SaveCharacterOptions from the
    /// client - the player clearing the checkbox - is stored as sent and the untrack sticks.
    /// </summary>
    public static class MissionTrackingRules
    {
        /// <summary>kMaxMissionTrackCount, client/gameui.py:130. The client reads and writes only
        /// these slots, although generated/client/defaultoption.py declares MissionTrack0..35.</summary>
        public const int MaxTrackedMissions = 30;

        /// <summary>The option id of each slot, in slot order. The ids are the ones paired with the
        /// names in generated/client/defaultoption.py (55..64, 85..99, 185, 187..190); the run is
        /// not contiguous, so the order is spelled out rather than counted.</summary>
        public static readonly IReadOnlyList<CharacterOption> TrackSlots = new[]
        {
            CharacterOption.MissionTrack0, CharacterOption.MissionTrack1, CharacterOption.MissionTrack2,
            CharacterOption.MissionTrack3, CharacterOption.MissionTrack4, CharacterOption.MissionTrack5,
            CharacterOption.MissionTrack6, CharacterOption.MissionTrack7, CharacterOption.MissionTrack8,
            CharacterOption.MissionTrack9, CharacterOption.MissionTrack10, CharacterOption.MissionTrack11,
            CharacterOption.MissionTrack12, CharacterOption.MissionTrack13, CharacterOption.MissionTrack14,
            CharacterOption.MissionTrack15, CharacterOption.MissionTrack16, CharacterOption.MissionTrack17,
            CharacterOption.MissionTrack18, CharacterOption.MissionTrack19, CharacterOption.MissionTrack20,
            CharacterOption.MissionTrack21, CharacterOption.MissionTrack22, CharacterOption.MissionTrack23,
            CharacterOption.MissionTrack24, CharacterOption.MissionTrack25, CharacterOption.MissionTrack26,
            CharacterOption.MissionTrack27, CharacterOption.MissionTrack28, CharacterOption.MissionTrack29
        };

        /// <summary>
        /// The mission ids the stored slots track, in slot order. LoadMissionTrackingData skips the
        /// value 0 (the default every slot carries in generated/client/defaultoption.py) and warns
        /// on a duplicate instead of adding it twice; this does the same silently.
        /// </summary>
        public static List<uint> Tracked(IEnumerable<CharacterOptions> options)
        {
            var values = new Dictionary<CharacterOption, string>();

            foreach (var option in options ?? Enumerable.Empty<CharacterOptions>())
                values[option.OptionId] = option.Value;

            var tracked = new List<uint>();

            foreach (var slot in TrackSlots)
            {
                if (!values.TryGetValue(slot, out var value))
                    continue;

                if (!uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var missionId) || missionId == 0)
                    continue;

                if (!tracked.Contains(missionId))
                    tracked.Add(missionId);
            }

            return tracked;
        }

        /// <summary>
        /// All thirty slots as they should stand once <paramref name="missionId"/> is accepted: the
        /// slots as they stand, minus any id the log no longer holds (client/gameui.py:1754
        /// FilterMissionTrackingData drops exactly those, against the avatar's current mission data,
        /// every time the client loads or saves the list), with the accepted mission appended the way
        /// AddMissionTracking appends it. Unused slots come back as "0" so a dropped mission does not
        /// leave a stale id behind.
        ///
        /// Anything past the thirtieth is discarded, which is what SaveMissionTrackingData does: it
        /// writes list[idx] for idx below the list length and 0 for the rest, keeping the oldest
        /// thirty. With MissionRules.MaxMissionCount also 30 the log cannot hold a thirty-first
        /// mission, so the truncation is unreachable in practice.
        /// </summary>
        public static List<CharacterOptions> Track(IEnumerable<CharacterOptions> options, Func<uint, bool> isInLog, uint missionId)
        {
            if (isInLog == null)
                throw new ArgumentNullException(nameof(isInLog));

            var tracked = Tracked(options).Where(isInLog).ToList();

            if (missionId != 0 && !tracked.Contains(missionId))
                tracked.Add(missionId);

            return TrackSlots
                .Select((slot, index) => new CharacterOptions(slot, index < tracked.Count ? tracked[index].ToString(CultureInfo.InvariantCulture) : "0"))
                .ToList();
        }
    }
}
