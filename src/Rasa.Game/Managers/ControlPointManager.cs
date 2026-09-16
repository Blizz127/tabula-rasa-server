using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;

    /// <summary>
    /// The PvP control points of the two final-live battlegrounds, as far as the original client evidences them.
    ///
    /// What is original: the points themselves (ControlPointData, the client's own 17-row table with its fields named
    /// from the code that reads it), the status struct and its state ids (shared/controlpointdefs.py,
    /// shared/controlpointstatus.py), and the two calls the client handles - ControlPointStatus(statusList) on the
    /// control-point manager entity and SetOwnerId(ownerId) on an OwnableControlPoint entity. The client asks for the
    /// list with RequestControlPointStatus (a CallActorMethod with no arguments).
    ///
    /// Where the client shows the points is worth being exact about (docs/pvp-control-point-client-evidence.md,
    /// section 3): the status list this manager answers with is read only by the challenge-board window, which is dead
    /// code in the final client, so the pair is kept faithful rather than load-bearing. The battleground tracker takes
    /// its points from ScoreBoardGameScore's cpData (cpId -> teamId), the world map and radar from CONTROL_POINT map
    /// markers whose state is (ownerTypeId, ownerId), and the point's own entity from SetOwnerId. Those belong to the
    /// battleground lifecycle, which has no server side yet.
    ///
    /// What is not: where each point stands on its map (the client maps carry no control-point entities; they were
    /// server-placed), how a point is captured and how long a war lasts, and the team/scoreboard lifecycle around a
    /// battleground. Those are recorded as gaps in docs/evidence and are not guessed here. So a channel for a
    /// battleground map answers the status request with every point of that map, unheld (ownerId None) and New, and
    /// <see cref="SetOwner"/> exists for the moment a placement and a capture rule are evidenced. Every other channel
    /// answers with an empty list, which is what the client shows for a map without control points.
    /// </summary>
    public class ControlPointManager
    {
        private static ControlPointManager _instance;
        private static readonly object InstanceLock = new object();

        /// <summary>Status per channel; a private instance keeps its own set.</summary>
        private readonly Dictionary<MapChannel, Dictionary<uint, ControlPointStatus>> _statusByChannel = new Dictionary<MapChannel, Dictionary<uint, ControlPointStatus>>();

        public static ControlPointManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new ControlPointManager();
                    }
                }

                return _instance;
            }
        }

        private ControlPointManager()
        {
        }

        /// <summary>The control points a channel answers for, from the client's table by the channel's map name.</summary>
        public IReadOnlyList<ControlPointStatus> StatusFor(MapChannel mapChannel)
        {
            if (mapChannel == null)
                return Array.Empty<ControlPointStatus>();

            lock (_statusByChannel)
            {
                if (!_statusByChannel.TryGetValue(mapChannel, out var statuses))
                {
                    statuses = ControlPointData.ForMap(mapChannel.MapInfo?.MapName)
                        .ToDictionary(row => row.Id, row => new ControlPointStatus(row.Id, null, ControlPointState.New, 0u));
                    _statusByChannel[mapChannel] = statuses;
                }

                return statuses.Values.OrderBy(status => status.ControlPointId).ToArray();
            }
        }

        /// <summary>RequestControlPointStatus (817): answered with the channel's list on the client's control-point manager.</summary>
        public void RequestControlPointStatus(Client client)
        {
            var mapChannel = client?.Player?.MapChannel;
            if (mapChannel == null)
                return;

            client.CallMethod(SysEntity.ClientControlPointManagerId, new ControlPointStatusPacket(StatusFor(mapChannel)));
        }

        /// <summary>The list, to every player on the channel; what a change of state is followed by.</summary>
        public void SendStatus(MapChannel mapChannel)
        {
            if (mapChannel?.ClientList == null)
                return;

            var packet = new ControlPointStatusPacket(StatusFor(mapChannel));
            foreach (var client in mapChannel.ClientList.ToArray())
                client?.CallMethod(SysEntity.ClientControlPointManagerId, packet);
        }

        /// <summary>
        /// Records a new owner and state for one of the channel's points, tells every player, and - when the point has a
        /// world entity - calls SetOwnerId on it so the client swaps the point's state package. The owner id is in the
        /// point's ownership type's id space (a team id for the live rows). Returns false for a point the map has not.
        /// </summary>
        public bool SetOwner(MapChannel mapChannel, uint controlPointId, long? ownerId, ControlPointState state, uint endTime, DynamicObject entity = null)
        {
            if (mapChannel == null)
                return false;

            StatusFor(mapChannel);
            lock (_statusByChannel)
            {
                if (!_statusByChannel[mapChannel].TryGetValue(controlPointId, out var status))
                    return false;

                status.OwnerId = ownerId;
                status.StateId = state;
                status.EndTime = endTime;
            }

            if (entity != null)
            {
                var wireOwner = ownerId.HasValue ? (int)ownerId.Value : ControlPointOwner.Neutral;
                CellManager.Instance.CellCallMethod(entity, new SetOwnerIdPacket(wireOwner));
            }

            SendStatus(mapChannel);
            return true;
        }

        /// <summary>Forgets a channel's state (a private instance that closed).</summary>
        public void Forget(MapChannel mapChannel)
        {
            if (mapChannel == null)
                return;

            lock (_statusByChannel)
                _statusByChannel.Remove(mapChannel);
        }
    }
}
