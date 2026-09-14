using System;
using System.Net;
using System.Net.Sockets;

namespace Rasa.Queue
{
    using Data;
    using Memory;
    using Networking;
    using Packets.Queue.Client;
    using Packets.Queue.Server;

    public class QueueClient
    {
        public QueueManager Manager { get; }
        public LengthedSocket Socket { get; }
        public QueueState State { get; private set; }
        public uint UserId { get; set; }
        public uint OneTimeKey { get; set; }
        public DateTime EnqueueTime { get; private set; }
        public DateTime DequeueTime { get; set; }

        /// <summary>When the handoff was sent; the slot it holds is given up if nobody arrives.</summary>
        public DateTime RedirectTime { get; private set; }

        public QueueClient(QueueManager manager, LengthedSocket socket)
        {
            Manager = manager;
            Socket = socket;
            Socket.OnReceive += OnReceive;
            Socket.OnError += OnError;
            Socket.OnDrop += OnDrop;

            Socket.ReceiveAsync();

            Socket.Send(new ServerKeyPacket
            {
                PublicKey = Manager.Config.PublicKey,
                Prime = Manager.Config.Prime,
                Generator = Manager.Config.Generator
            });

            State = QueueState.Authenticating;
        }

        private void OnReceive(BufferData data)
        {
            // Runs on a socket completion thread: an exception escaping here is unhandled and
            // terminates the process, so a malformed or unexpected queue packet must only ever
            // cost this one connection.
            try
            {
                HandleReceive(data);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Error handling queue packet from {Socket.RemoteAddress}, disconnecting: {e}");
                Close();
            }
        }

        private void HandleReceive(BufferData data)
        {
            switch (State)
            {
                case QueueState.Authenticating:
                    var keyPacket = new ClientKeyPacket();

                    keyPacket.Read(data.GetReader());

                    if (keyPacket.PublicKey != Manager.Config.PublicKey)
                    {
                        Close();
                        return;
                    }

                    Socket.Send(new ClientKeyOkPacket());

                    State = QueueState.Authenticated;

                    break;

                case QueueState.Authenticated:
                    if (data[data.Offset++] != 7)
                        throw new Exception("Invalid opcode???");

                    var loginPacket = new QueueLoginPacket();

                    loginPacket.Read(data.GetReader());

                    UserId = loginPacket.UserId;
                    OneTimeKey = loginPacket.OneTimeKey;
                    State = QueueState.InQueue;

                    Manager.Enqueue(this);
                    EnqueueTime = DateTime.Now;
                    break;

                default:
                    throw new Exception("Received packet in a invalid queue state!");
            }
        }

        private void OnError(SocketAsyncEventArgs args)
        {
            Close();
        }

        /// <summary>The socket gave up on this connection; the reason is already logged.</summary>
        private void OnDrop(string reason)
        {
            Close();
        }

        public void Close()
        {
            Socket.Close();

            State = QueueState.Disconnected;

            Manager.Disconnect(this);
        }

        /// <summary>
        /// The account this connection was handed off for has logged in at the world port. From
        /// here it is a world client and counted as one; whether the client closes this socket
        /// now or keeps it open until it exits, it no longer holds a slot of its own. Before this
        /// a client that kept the queue socket open counted twice, and the server read as full
        /// at half its cap.
        /// </summary>
        internal void MarkArrived()
        {
            if (State == QueueState.Redirecting)
                State = QueueState.Arrived;
        }

        public void Redirect(IPAddress ip, int port)
        {
            State = QueueState.Redirecting;
            RedirectTime = DateTime.Now;

            Socket.Send(new HandoffToGamePacket
            {
                OneTimeKey = OneTimeKey,
                ServerIp = ip,
                ServerPort = port,
                UserId = UserId
            });
        }

        public void SendPositionUpdate(int position, int estimatedTime)
        {
            Socket.Send(new QueuePositionPacket
            {
                Position = position,
                EstimatedTime = estimatedTime
            });
        }
    }
}
