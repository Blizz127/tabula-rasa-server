using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Extensions;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;

    public class MapTriggerManager
    {
        private static MapTriggerManager _instance;
        private static readonly object InstanceLock = new object();
        public static MapTriggerManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new MapTriggerManager();
                    }
                }

                return _instance;
            }
        }

        public MapTriggerManager()
        {
        }

        internal void MapTriggerInit()
        {
        }

        internal void PlayerEnterTriggerRange(Client client, MapTrigger mapTrigger)
        {
            if (client.Player.IsNear5m(mapTrigger))
            {
                if (mapTrigger.TriggeredBy.Contains(client))
                    return;

                mapTrigger.TriggeredBy.Add(client);

                var dropshipInfoList = DynamicObjectManager.Instance.CreateListOfDropships();

                client.CallMethod(SysEntity.ClientMethodId, new EnteredWaypointPacket(mapTrigger.MapContextId, mapTrigger.MapContextId, dropshipInfoList, WaypointType.Dropship, mapTrigger.TriggerId));
            }
        }
        internal void PlayerExitTriggerRange(Client client, MapTrigger mapTrigger)
        {
            if (!client.Player.IsNear5m(mapTrigger))
                if (mapTrigger.TriggeredBy.Contains(client))
                {
                    mapTrigger.TriggeredBy.Remove(client);
                    client.CallMethod(SysEntity.ClientMethodId, new ExitedWaypointPacket());
                }
        }

        internal void TriggersProximityWorker(MapChannel mapChannel)
        {
            foreach (var client in mapChannel.ClientList)
            {
                if (client.Player == null || client.Player.Disconected || client.State == ClientState.Loading ||
                    !mapChannel.MapCellInfo.Cells.TryGetValue(client.Player.Cells[2, 2], out var cell) ||
                    !cell.ClientList.Contains(client))
                    continue;

                foreach (var mapTrigger in cell.MapTriggers)
                {
                    // check for players that enter range
                    PlayerEnterTriggerRange(client, mapTrigger);

                    // check for players that leave range
                    PlayerExitTriggerRange(client, mapTrigger);
                }
            }
        }
    }
}
