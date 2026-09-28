using System.Net.Sockets;

namespace Rasa.Login
{
    using Cryptography;
    using Memory;
    using Networking;
    using Packets.Login.Client;
    using Packets.Login.Server;

    public class LoginClient
    {
        public LengthedSocket Socket { get; }
        public LoginManager Manager { get; }
        public ClientCryptData Data { get; } = new ClientCryptData();

        // ReSharper disable once InconsistentNaming
        public BigNum PrivateKey { get; } = new BigNum();
        public BigNum PublicKey { get; } = new BigNum();
        public BigNum K { get; } = new BigNum();

        public LoginClient(LoginManager manager, LengthedSocket socket)
        {
            Manager = manager;

            Socket = socket;
            Socket.AutoReceive = false;
            Socket.OnReceive += OnReceive;
            Socket.OnError += OnError;
            Socket.OnDrop += OnDrop;

            DHKeyExchange.GeneratePrivateAndPublicA(PrivateKey, PublicKey);

            Socket.Send(new ServerKeyPacket
            {
                PublicKey = PublicKey,
                Prime = DHKeyExchange.ConstantPrime,
                Generator = DHKeyExchange.ConstantGenerator
            });

            Socket.OnEncrypt += OnEncrypt;
            Socket.ReceiveAsync();
        }

        private void OnEncrypt(BufferData data, ref int length)
        {
            GameCryptManager.Encrypt(data.Buffer, data.BaseOffset + data.Offset, ref length, data.RemainingLength, Data);
        }

        private void OnReceive(BufferData data)
        {
            var packet = new ClientKeyPacket();
            packet.Read(data.GetReader());

            DHKeyExchange.GenerateServerK(PrivateKey, packet.B, K);

            var key = new byte[64];
            K.WriteToBigEndian(key, 0, key.Length);

            GameCryptManager.Initialize(Data, key);

            Socket.Send(new ClientKeyOkPacket());

            Cleanup();

            Manager.ExchangeDone(this);
        }

        private void OnError(SocketAsyncEventArgs args)
        {
            // The world connection is not logged until the key exchange completes, so without this
            // a connection that ends inside it leaves no trace at all. The launcher's reachability
            // probe (a bare TCP connect) ends here as well, which is what a probe looks like.
            Logger.WriteLog(LogType.Network, $"World key exchange with {Socket.RemoteAddress} ended before it completed: {LengthedSocket.DescribeError(args)}.");

            Manager.Disconnect(this);

            Close();
        }

        /// <summary>The socket gave up on this connection; the reason is already logged.</summary>
        private void OnDrop(string reason)
        {
            Manager.Disconnect(this);

            Close();
        }

        /// <summary>
        /// Hands the socket over: every handler this exchange put on it comes off, OnDrop included.
        /// OnDrop used to stay, so a drop after the handover reached this finished exchange first,
        /// which closed the socket underneath the world client and had its disconnect logged as a
        /// local close rather than as the drop it was.
        /// </summary>
        private void Cleanup()
        {
            Socket.AutoReceive = true;
            Socket.OnReceive = null;
            Socket.OnError = null;
            Socket.OnDrop = null;
            Socket.OnEncrypt = null;
        }

        public void Close()
        {
            Socket.Close();
        }
    }
}
