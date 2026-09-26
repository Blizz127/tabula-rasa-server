using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Communicator.Server;

    /// <summary>
    /// Free text from staff to every player in the world, or to everyone on one map, through the
    /// client's own admin-message path (AdminMessagePacket): the client prints its "ADMIN MESSAGE: "
    /// header in yellow and the text after it.
    ///
    /// Final-night footage shows two uses, and both are free text: "ALERT: PLATEAU IS LOST!" and the
    /// shutdown countdown (ShutdownCountdown). Nothing here sends anything by itself. The operator types
    /// it (console `announce`, chat `.announce`), and no zone-loss rule is attached: only the Plateau
    /// wording was ever seen, not what triggered it (GAP-SHUTDOWN-ZONE-LOSS-RULE).
    /// Provenance: docs/evidence/shutdown-broadcast.json.
    /// </summary>
    public class AdminBroadcastManager
    {
        private static AdminBroadcastManager _instance;
        private static readonly object InstanceLock = new object();

        public static AdminBroadcastManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new AdminBroadcastManager();
                    }
                }

                return _instance;
            }
        }

        private AdminBroadcastManager()
        {
        }

        /// <summary>
        /// Who is in the world: every client standing in a live map channel, shared or instanced. A
        /// client at character selection or on a loading screen has no chat window to print into.
        /// Replaceable so a test can hand over its own clients.
        /// </summary>
        public Func<IEnumerable<Client>> WorldClients { get; set; } = () =>
            MapChannelManager.Instance.Channels().SelectMany(channel => channel.ClientList.ToList());

        /// <summary>Sends the text to every client in the world. Returns how many were sent it.</summary>
        public int Broadcast(string text)
        {
            return Send(WorldClients(), text);
        }

        /// <summary>Sends the text to every client on the map context, every instance of it.</summary>
        public int BroadcastToMap(uint mapContextId, string text)
        {
            return Send(MapChannelManager.Instance.Channels()
                .Where(channel => channel.MapInfo?.MapContextId == mapContextId)
                .SelectMany(channel => channel.ClientList.ToList()), text);
        }

        public static void Send(Client client, string text)
        {
            client.CallMethod(SysEntity.CommunicatorId, new AdminMessagePacket(text));
        }

        private static int Send(IEnumerable<Client> clients, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            var sent = 0;

            foreach (var client in clients.Where(c => c != null && c.State != ClientState.Disconnected).Distinct())
            {
                Send(client, text);
                sent++;
            }

            return sent;
        }
    }
}
