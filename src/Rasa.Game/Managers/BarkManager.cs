using System;
using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// Sends a creature's spoken line, and holds the two rules the client's own code imposes on it.
    ///
    /// A bark is a one-argument push: <c>Bark = 411</c> (client methodid.pyo:84) reaches
    /// <c>Creature.Recv_Bark(barkID)</c> (client/augmentations/creature.pyo, first=108), which looks the id up in
    /// its own table, plays the clip and puts a white overhead bubble up for the duration that table names. No
    /// text and no audio id travel, and the server chooses neither - see <see cref="BarkTable"/>.
    ///
    /// Two of the client's numbers are therefore the server's job:
    ///
    /// * <b>Earshot.</b> Recv_Bark reads the clip's own <c>audio3DParameter</c> (line 116) and then overwrites it
    ///   with the constant <c>(2.0, 20.0)</c> (line 117), so every bark, whatever its clip, is inaudible past
    ///   20 m; the overhead manager culls bubbles at the same distance
    ///   (<c>ui/overheadwindow.pyo</c> kOverheadDisplayMaxDistance = 20, line 207). Recv_Bark itself tests no
    ///   distance, so a bark sent to a player further away is a packet that renders nothing. Nothing is sent
    ///   outside <see cref="AudibleRange"/>.
    /// * <b>Overlap.</b> The client has no rate limit: a second bark inside the first one's duration replaces the
    ///   bubble and plays a second clip over the first. A creature is therefore held silent until its own bubble
    ///   has expired.
    ///
    /// Both follow from the client. This class invents no cadence: a bark is sent only when something asks for
    /// one (a content rule, or the GM command).
    /// </summary>
    public class BarkManager
    {
        private static BarkManager _instance;
        private static readonly object InstanceLock = new object();

        /// <summary>
        /// The client's hard-coded bark range in metres: <c>minDist, maxDist = (2.0, 20.0)</c>
        /// (creature.pyo Recv_Bark line 117), which is also kOverheadDisplayMaxDistance.
        /// </summary>
        public const float AudibleRange = 20.0f;

        /// <summary>A beat after the bubble goes, so two scripted lines never run together.</summary>
        private const long SpeakingTailMs = 500;

        public static BarkManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new BarkManager();
                    }
                }

                return _instance;
            }
        }

        private BarkManager()
        {
        }

        /// <summary>True when the listener is close enough to hear the speaker and see the bubble.</summary>
        public static bool InEarshot(Vector3 speaker, Vector3 listener) => Vector3.Distance(speaker, listener) <= AudibleRange;

        /// <summary>
        /// Holds the speaker silent for the length of the bubble it is about to put up. False when it is still
        /// talking, in which case nothing is sent: a dropped line is better than two voices from one mouth.
        /// </summary>
        public bool TryTakeTheFloor(Creature speaker, int durationMs, long now)
        {
            if (speaker == null || now < speaker.BarkSpeakingUntil)
                return false;

            speaker.BarkSpeakingUntil = now + durationMs + SpeakingTailMs;
            return true;
        }

        /// <summary>
        /// Says a line, to everyone within earshot of the speaker.
        /// </summary>
        /// <param name="speaker">The creature the line comes out of; the client draws the bubble over its entity.</param>
        /// <param name="barkId">A row of the client's own bark table; an id it does not know is not sent.</param>
        /// <returns>True when at least one player was sent the line.</returns>
        public bool Say(Creature speaker, uint barkId)
        {
            if (speaker == null || !BarkTable.TryGetDuration(barkId, out var durationMs))
                return false;

            var audience = ClientsInEarshot(speaker);

            // Nobody to hear it: the packet would render nothing, and the speaker keeps its line for later.
            if (audience.Count == 0)
                return false;

            if (!TryTakeTheFloor(speaker, durationMs, Environment.TickCount64))
                return false;

            var packet = new BarkPackage(barkId);

            foreach (var client in audience)
                client.CallMethod(speaker.EntityId, packet);

            return true;
        }

        /// <summary>
        /// The players who can hear the speaker. The cell matrix is 5x5 cells of 25.6 m (CellManager), about
        /// 64 m across - three times what the client can hear - so it is the candidate set, and the 20 m test
        /// is what decides.
        /// </summary>
        private static List<Client> ClientsInEarshot(Creature speaker)
        {
            var heard = new List<Client>();

            // The channel the creature was added to; a creature that is not on one has no audience at all.
            var mapChannel = speaker.MapChannel;

            if (mapChannel?.MapCellInfo == null)
                return heard;

            var cellPosX = (uint)(speaker.Position.X / CellManager.CellSize + CellManager.CellBias);
            var cellPosZ = (uint)(speaker.Position.Z / CellManager.CellSize + CellManager.CellBias);

            foreach (var cellSeed in CellManager.Instance.CreateCellMatrix(mapChannel, cellPosX, cellPosZ))
            {
                if (!mapChannel.MapCellInfo.Cells.TryGetValue(cellSeed, out var cell))
                    continue;

                foreach (var client in cell.ClientList)
                    if (client?.Player != null && !heard.Contains(client) && InEarshot(speaker.Position, client.Player.Position))
                        heard.Add(client);
            }

            return heard;
        }
    }
}
