using System.Collections.Generic;

namespace Rasa.Game
{
    using Managers;

    // Socket callbacks only enqueue; world state and persistence belong to the server loop.
    public sealed class DisconnectedClientQueue
    {
        private readonly HashSet<Client> _clients = new();

        public void Enqueue(Client client)
        {
            lock (_clients)
                _clients.Add(client);
        }

        public void Process(List<Client> clients, MapChannelManager maps)
        {
            Client[] pending;
            lock (_clients)
            {
                pending = new Client[_clients.Count];
                _clients.CopyTo(pending);
            }

            foreach (var client in pending)
            {
                if (!maps.TryRemoveDisconnectedClient(client))
                    continue;

                clients.Remove(client);

                // Nothing else reaches this client now, and this is the loop its inbound stream
                // belongs to, so its pooled arrays can go back (InfiniteRasa 492954a): a partial
                // frame left by an Alt+F4 otherwise costs the shared pool a block per disconnect.
                try
                {
                    client.ReleaseInboundBuffers();
                }
                catch (System.Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"Failed to release inbound buffers for a disconnected client: {e}");
                }

                lock (_clients)
                    _clients.Remove(client);
            }
        }
    }
}
