using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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
        private int _closed;
        private IPAddress _remoteAddress;
        public bool Connected => Socket.Connected;
        public IPAddress RemoteAddress
        {
            get
            {
                if (_remoteAddress == null)
                {
                    try { _remoteAddress = (Socket.RemoteEndPoint as IPEndPoint)?.Address; }
                    catch (ObjectDisposedException) { }
                }
                return _remoteAddress;
            }
        }

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
            if (args.UserToken is BufferData data)
            {
                BufferManager.FreeBuffer(data);
                args.SetBuffer(null, 0, 0);
                args.UserToken = null;
            }

            args.RemoteEndPoint = null;

            args.AcceptSocket = null;
            args.Completed -= OperationCompleted;

            lock (_socketAsyncEventArgsPool)
                _socketAsyncEventArgsPool.Push(args);
        }

        private void OperationCompleted(object o, SocketAsyncEventArgs args)
        {
            var keepEventArgs = false;
            try
            {
                if (args.SocketError != SocketError.Success || (args.LastOperation == SocketAsyncOperation.Receive && args.BytesTransferred == 0))
                    OnError?.Invoke(args);
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
                                keepEventArgs = true;
                                SendAsync(args);
                                return;
                            }

                            OnSend?.Invoke(args);
                            break;

                        case SocketAsyncOperation.Receive:
                            // This value may change in the middle of processing, causing odd behavior
                            var receiveAfter = AutoReceive;
                            if (ProcessInputBuffer(data, args))
                            {
                                keepEventArgs = true;
                                ReceiveAsync(args);
                                return;
                            }

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
            }
            catch (Exception exception) when (exception is InvalidDataException || exception is EndOfStreamException)
            {
                args.SocketError = SocketError.InvalidArgument;
                try { OnError?.Invoke(args); }
                finally { Close(); }
            }
            finally
            {
                if (!keepEventArgs)
                    TeardownEventArgs(args);
            }
        }

        private int ReadSize(BufferData data)
        {
            ulong payloadLength = SizeHeaderLength switch
            {
                SizeType.Char => data[0],
                SizeType.Word => BitConverter.ToUInt16(data.Buffer, data.BaseOffset),
                SizeType.Dword => BitConverter.ToUInt32(data.Buffer, data.BaseOffset),
                _ => throw new NotSupportedException($"Unsupported size header: {SizeHeaderLength}")
            };
            var length = payloadLength + (CountSize ? 0UL : (ulong)LengthSize);
            if (length < (ulong)LengthSize || length > (ulong)BufferManager.BlockSize)
                throw new InvalidDataException("Frame length is outside the receive buffer bounds.");

            return (int)length;
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
                        throw new InvalidDataException("Frame length exceeds the available receive buffer.");
                }

                if (length == -1 || data.ByteCount < length)
                {
                    args.SetBuffer(data.BaseOffset + data.ByteCount, data.Length - data.ByteCount);

                    return true;
                }

                data.Offset = LengthSize;
                data.Length = length;

                if (OnDecrypt != null && !OnDecrypt(data))
                    throw new InvalidDataException("Frame decryption failed.");
                OnReceive?.Invoke(data);

                if (Volatile.Read(ref _closed) != 0)
                    return false;

                if (data.ByteCount == length)
                    break;

                data.ByteCount -= length;
                // Keep the next frame at the start of this buffer block, including when
                // its header/body is split across receives after a coalesced frame.
                Array.Copy(data.Buffer, data.BaseOffset + length, data.Buffer, data.RealBaseOffset, data.ByteCount);
                data.BaseOffset = data.RealBaseOffset;
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
            if (Volatile.Read(ref _closed) != 0)
                return;
            ReceiveAsync(SetupEventArgs(SocketAsyncOperation.Receive));
        }

        private void ReceiveAsync(SocketAsyncEventArgs args)
        {
            bool pending;
            try
            {
                pending = Socket.ReceiveAsync(args);
            }
            catch (Exception exception) when (exception is SocketException || exception is ObjectDisposedException)
            {
                FailSocketOperation(args);
                return;
            }
            if (!pending)
                OperationCompleted(Socket, args);
        }

        public void Send(IBasePacket packet)
        {
            var args = SetupEventArgs(SocketAsyncOperation.Send);
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

            SendAsync(args);
        }

        private void SendAsync(SocketAsyncEventArgs args)
        {
            bool pending;
            try
            {
                pending = Socket.SendAsync(args);
            }
            catch (Exception exception) when (exception is SocketException || exception is ObjectDisposedException)
            {
                FailSocketOperation(args);
                return;
            }
            if (!pending)
                OperationCompleted(Socket, args);
        }

        private void FailSocketOperation(SocketAsyncEventArgs args)
        {
            try
            {
                args.SocketError = SocketError.OperationAborted;
                OnError?.Invoke(args);
            }
            finally
            {
                Close();
                TeardownEventArgs(args);
            }
        }

        public void Close()
        {
            if (Interlocked.Exchange(ref _closed, 1) != 0)
                return;

            try
            {
                _ = RemoteAddress; // Disconnect observers can still identify a disposed socket.
                OnDisconnect?.Invoke();

                Socket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                Socket.Dispose();
            }
        }
    }
}
