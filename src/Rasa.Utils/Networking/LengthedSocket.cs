using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Rasa.Networking
{
    using Extensions;
    using Memory;
    using Packets;

    public enum SizeType : byte
    {
        None  = 0,
        Char  = 1,
        Word  = 2,
        Dword = 4
    }

    public class LengthedSocket
    {
        public delegate void AcceptHandler(LengthedSocket acceptedSocket);
        public delegate void AsyncHandler(SocketAsyncEventArgs args);
        public delegate void ReceiveHandler(BufferData data);
        public delegate void DisconnectHandler();
        public delegate void EncryptDelegate(BufferData data, ref int length);
        public delegate bool DecryptDelegate(BufferData data);

        public SizeType SizeHeaderLength { get; }
        public bool CountSize { get; }
        public int LengthSize => (int) SizeHeaderLength;
        public Socket Socket { get; }
        public bool Connected => Socket.Connected;
        public IPAddress RemoteAddress => ((IPEndPoint)Socket.RemoteEndPoint).Address;

        public bool AutoReceive { get; set; } = true;

        public AsyncHandler OnConnect;
        public DisconnectHandler OnDisconnect;
        public AcceptHandler OnAccept;
        public AsyncHandler OnSend;
        public ReceiveHandler OnReceive;
        public AsyncHandler OnError;
        public EncryptDelegate OnEncrypt;
        public DecryptDelegate OnDecrypt;

        public LengthedSocket(SizeType sizeHeaderLen, bool countSize = true)
           : this(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), sizeHeaderLen, countSize)
        {
        }

        public LengthedSocket(Socket s, SizeType sizeHeaderLen, bool countSize)
        {
            Socket = s;
            SizeHeaderLength = sizeHeaderLen;
            CountSize = countSize;
        }

        #region Sending

        /// <summary>
        /// One send may be in flight on a socket at a time; the rest wait here in the order they
        /// were written.
        ///
        /// Two things went wrong without this. A send that only transfers part of its buffer is
        /// resumed by issuing the remainder afterwards, so anything sent in between landed in the
        /// middle of it - and the other side is reading a length-prefixed stream, so a packet
        /// split around another packet's bytes is not a garbled message, it is a frame boundary
        /// in the wrong place and everything after it is misread. Partial sends happen when the
        /// socket's buffer is full, which is to say under load. Separately, overlapping sends have
        /// no ordering guarantee between them at all.
        ///
        /// The queue holds args that are already serialised and encrypted: the cipher is a stream
        /// and its state has to advance in the same order the bytes go out, so Send does that work
        /// under the same lock that fixes the order.
        /// </summary>
        private readonly object _sendLock = new object();
        private readonly Queue<SocketAsyncEventArgs> _sendQueue = new Queue<SocketAsyncEventArgs>();
        private bool _sending;
        private bool _sendClosed;

        /// <summary>
        /// How far a client is allowed to fall behind before it is dropped. Each queued send holds
        /// a pooled SocketAsyncEventArgs and its buffer, and the pool is shared by every
        /// connection, so one client that has stopped reading must not be able to starve the rest.
        /// The same reasoning as the inbound flood cap, in the other direction.
        /// </summary>
        private const int MaxQueuedSends = 512;

        #endregion

        #region SocketAsyncEventArgs
        private static Stack<SocketAsyncEventArgs> _socketAsyncEventArgsPool;

        private static readonly object ArgsInitLock = new object();

        public static void InitializeEventArgsPool(int eventArgsPoolCount)
        {
            if (_socketAsyncEventArgsPool != null)
                return;

            lock (ArgsInitLock)
            {
                if (_socketAsyncEventArgsPool != null)
                    return;

                _socketAsyncEventArgsPool = new Stack<SocketAsyncEventArgs>(eventArgsPoolCount);

                for (var i = 0; i < eventArgsPoolCount; ++i)
                    _socketAsyncEventArgsPool.Push(new SocketAsyncEventArgs());
            }
        }

        private SocketAsyncEventArgs SetupEventArgs(SocketAsyncOperation operation)
        {
            SocketAsyncEventArgs args;

            lock (_socketAsyncEventArgsPool)
                args = _socketAsyncEventArgsPool.Count > 0 ? _socketAsyncEventArgsPool.Pop() : null;

            if (args == null)
                throw new OutOfMemoryException("All of the SocketAsyncEventArgs are being used!");

            switch (operation)
            {
                case SocketAsyncOperation.Receive:
                case SocketAsyncOperation.Send:
                    var data = BufferManager.RequestBuffer();

                    args.SetBuffer(BufferManager.Buffer, data.BaseOffset, data.MaxLength);
                    args.UserToken = data;
                    args.AcceptSocket = Socket;
                    break;

                case SocketAsyncOperation.Connect:
                    args.AcceptSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    break;
            }

            args.Completed += OperationCompleted;

            return args;
        }

        private void TeardownEventArgs(SocketAsyncEventArgs args)
        {
            // Free by what the args is actually holding rather than by LastOperation. An args
            // that was set up for a send and then discarded - the socket closed underneath it,
            // or the queue overflowed - never started an operation, so its LastOperation is
            // still whatever the previous user of that pool entry did, and its buffer would
            // have been handed back to the pool while a BufferData still pointed at it.
            var buffer = args.GetUserToken<BufferData>();

            if (buffer != null)
            {
                BufferManager.FreeBuffer(buffer);

                args.SetBuffer(null, 0, 0);
                args.UserToken = null;
            }

            if (args.LastOperation == SocketAsyncOperation.Connect)
                args.RemoteEndPoint = null;

            args.AcceptSocket = null;
            args.Completed -= OperationCompleted;

            lock (_socketAsyncEventArgsPool)
                _socketAsyncEventArgsPool.Push(args);
        }

        private void OperationCompleted(object o, SocketAsyncEventArgs args)
        {
            // Set when this args finished a send outright, so the next queued one starts only
            // after this one's buffer has gone back to the pool.
            var sendFinished = false;

            if (args.SocketError != SocketError.Success || (args.LastOperation == SocketAsyncOperation.Receive && args.BytesTransferred == 0))
            {
                // A failed send leaves the socket marked busy for ever and nothing would go out
                // again; the connection is finished either way, so let go of what was waiting.
                if (args.LastOperation == SocketAsyncOperation.Send)
                    DiscardQueuedSends();

                OnError?.Invoke(args);
            }
            else
            {
                var data = args.GetUserToken<BufferData>();

                switch (args.LastOperation)
                {
                    case SocketAsyncOperation.Send:
                        data.ByteCount += args.BytesTransferred;

                        // We've transferred less bytes than we should have
                        if (data.Length > data.ByteCount)
                        {
                            args.SetBuffer(data.BaseOffset + data.ByteCount, data.Length - data.ByteCount);

                            // TODO: test if multiple send operations will collide (if the packet was only sent partially, and another packet is getting sent between the two parts)
                            // TODO: create a queue for sending async, if it collides?
                            SendAsync(args);
                            return;
                        }

                        sendFinished = true;
                        OnSend?.Invoke(args);
                        break;

                    case SocketAsyncOperation.Receive:
                        // This value may change in the middle of processing, causing odd behavior
                        var receiveAfter = AutoReceive;
                        if (ProcessInputBuffer(data, args))
                            return;

                        if (receiveAfter)
                            ReceiveAsync();

                        break;

                    case SocketAsyncOperation.Connect:
                        OnConnect?.Invoke(args);
                        break;

                    case SocketAsyncOperation.Accept:
                        OnAccept?.Invoke(new LengthedSocket(args.AcceptSocket, SizeHeaderLength, CountSize));
                        break;
                }
            }

            TeardownEventArgs(args);

            if (sendFinished)
                SendNext();
        }

        private int ReadSize(BufferData data)
        {
            var headerSize = !CountSize ? LengthSize : 0;

            switch (SizeHeaderLength)
            {
                case SizeType.Char:
                    return headerSize + data[0];

                case SizeType.Word:
                    return headerSize + BitConverter.ToInt16(data.Buffer, data.BaseOffset);

                case SizeType.Dword:
                    return headerSize + BitConverter.ToInt32(data.Buffer, data.BaseOffset);

                default:
                    throw new NotImplementedException($"Only 1, 2 and 4 byte headers are supported! {SizeHeaderLength} is not!");
            }
        }

        private bool ProcessInputBuffer(BufferData data, SocketAsyncEventArgs args)
        {
            data.ByteCount += args.BytesTransferred;

            while (true)
            {
                var length = -1;

                if (data.ByteCount >= LengthSize)
                    length = ReadSize(data);

                if (length != -1)
                {
                    data.Length = length;

                    if (data.Length > data.MaxLength)
                        throw new OutOfMemoryException($"Packet is bigger than the max packet size! Packet size: {length} | Max buffer size: {data.MaxLength}");
                }

                if (length == -1 || data.ByteCount < length)
                {
                    args.SetBuffer(data.BaseOffset + data.ByteCount, data.Length - data.ByteCount);

                    ReceiveAsync(args);
                    return true;
                }

                data.Offset = LengthSize;
                data.Length = length;

                OnDecrypt?.Invoke(data);
                OnReceive?.Invoke(data);

                if (data.ByteCount == length)
                    break;

                data.ByteCount -= length;
                data.BaseOffset += length;
                data.Offset = 0;
                data.Length = data.MaxLength;
            }

            return false;
        }

        private void CopyToOtherBuffer(BufferData source, BufferData desti)
        {
            
        }
        #endregion

        public void Bind(EndPoint ep)
        {
            Socket.Bind(ep);
        }

        public void Listen(int backlog)
        {
            Socket.Listen(backlog);
        }

        public void AcceptAsync()
        {
            var args = SetupEventArgs(SocketAsyncOperation.Accept);

            if (!Socket.AcceptAsync(args))
                OperationCompleted(Socket, args);
        }

        public void ConnectAsync(EndPoint remote)
        {
            var args = SetupEventArgs(SocketAsyncOperation.Connect);

            args.RemoteEndPoint = remote;

            if (!Socket.ConnectAsync(args))
                OperationCompleted(Socket, args);
        }

        public void ReceiveAsync()
        {
            ReceiveAsync(SetupEventArgs(SocketAsyncOperation.Receive));
        }

        private void ReceiveAsync(SocketAsyncEventArgs args)
        {
            if (!Socket.ReceiveAsync(args))
                OperationCompleted(Socket, args);
        }

        public void Send(IBasePacket packet)
        {
            if (_sendClosed)
                return;

            // The args and its buffer are taken from the pool *before* the send lock, never
            // while holding it. TeardownEventArgs takes the pool lock too, and it runs on the
            // completion path which then takes the send lock to start the next send - so a Send
            // that held the send lock while reaching for the pool would be waiting for a lock
            // held by a thread waiting for this one.
            var args = SetupEventArgs(SocketAsyncOperation.Send);

            var start = false;
            var discard = false;
            var overflowed = false;

            // Writing and encrypting happen under the send lock, not just the handing-off. The
            // encryption is a stream cipher, so its state has to advance in the same order the
            // bytes reach the socket; building two packets at once on two threads would advance
            // it twice and send both with the wrong keystream.
            lock (_sendLock)
            {
                if (_sendClosed)
                {
                    discard = true;
                }
                else
                {
                    var data = args.GetUserToken<BufferData>();

                    int length;

                    // Keep space for the length header
                    data.Offset = LengthSize;

                    // Write the packet data to the buffer
                    using (var sw = data.CreateWriter())
                    {
                        packet.Write(sw);

                        length = (int) sw.BaseStream.Position;
                    }

                    OnEncrypt?.Invoke(data, ref length);

                    // Reset the offset to send everything (including the size header)
                    data.Offset = 0;
                    data.Length = length + LengthSize;

                    var sizeLen = CountSize ? length + LengthSize : length;

                    // Copy the size header into the buffer
                    for (var i = 0; i < LengthSize; ++i)
                        data[i] = (byte) ((sizeLen >> (i * 8)) & 0xFF);

                    args.SetBuffer(data.BaseOffset, data.Length);

                    if (!_sending)
                    {
                        _sending = true;
                        start = true;
                    }
                    else if (_sendQueue.Count < MaxQueuedSends)
                    {
                        _sendQueue.Enqueue(args);
                    }
                    else
                    {
                        // Too far behind to catch up. Drop the packet and the connection with it -
                        // silently discarding one packet out of a stream the other side is framing
                        // would desync it just as surely as interleaving would.
                        _sendClosed = true;
                        overflowed = true;
                    }
                }
            }

            if (start)
            {
                SendAsync(args);
                return;
            }

            if (!discard && !overflowed)
                return;

            TeardownEventArgs(args);

            if (!overflowed)
                return;

            Logger.WriteLog(LogType.Network,
                $"Send queue full ({MaxQueuedSends}) for {SafeRemoteAddress()}; disconnecting.");

            // Hand the queued buffers back here rather than leaving it to whoever handles the
            // overflow. Nothing else will ever go out on this socket, and the pool entries the
            // queue is sitting on belong to every other connection.
            DiscardQueuedSends();

            QueueOverflow?.Invoke();
        }

        /// <summary>Raised when a connection falls too far behind to keep queueing for it.</summary>
        public DisconnectHandler QueueOverflow;

        private string SafeRemoteAddress()
        {
            try
            {
                return RemoteAddress.ToString();
            }
            catch (System.Exception)
            {
                return "a closed socket";
            }
        }

        /// <summary>
        /// Sends whatever is next in the queue, or marks the socket idle.
        ///
        /// A send that completes synchronously runs OperationCompleted on this thread, which
        /// comes back here - so this recurses once per back-to-back synchronous completion,
        /// bounded by MaxQueuedSends. That only happens while the socket has room, which is the
        /// case where the queue is not deep.
        /// </summary>
        private void SendNext()
        {
            SocketAsyncEventArgs next;

            lock (_sendLock)
            {
                if (_sendQueue.Count == 0 || _sendClosed)
                {
                    _sending = false;
                    return;
                }

                next = _sendQueue.Dequeue();
            }

            SendAsync(next);
        }

        /// <summary>
        /// Throws away anything still waiting to go out and hands its buffers back. Called when
        /// the socket errors or closes: those packets have nowhere to go, and the pool entries
        /// they are holding belong to every other connection.
        /// </summary>
        private void DiscardQueuedSends()
        {
            List<SocketAsyncEventArgs> abandoned;

            lock (_sendLock)
            {
                _sendClosed = true;
                _sending = false;

                if (_sendQueue.Count == 0)
                    return;

                abandoned = new List<SocketAsyncEventArgs>(_sendQueue);
                _sendQueue.Clear();
            }

            foreach (var args in abandoned)
                TeardownEventArgs(args);
        }

        private void SendAsync(SocketAsyncEventArgs args)
        {
            if (!Socket.SendAsync(args))
                OperationCompleted(Socket, args);
        }

        public void Close()
        {
            DiscardQueuedSends();

            try
            {
                OnDisconnect?.Invoke();

                Socket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
