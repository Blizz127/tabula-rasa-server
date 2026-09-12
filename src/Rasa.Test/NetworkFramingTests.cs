using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Memory;
using Rasa.Networking;

namespace Rasa.Test
{
    [TestClass]
    [DoNotParallelize]
    public class NetworkFramingTests
    {
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            BufferManager.Initialize(65536, 4, 4);
            LengthedSocket.InitializeEventArgsPool(16);
        }

        [DataTestMethod]
        [DataRow(65537u)]
        [DataRow(uint.MaxValue)]
        [DataRow(uint.MaxValue - 2)]
        public void OversizedOrOverflowingHeaderIsInvalidData(uint declaredLength)
        {
            using var transport = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var socket = new LengthedSocket(transport, SizeType.Dword, false);
            var frame = BufferManager.RequestBuffer();
            try
            {
                Array.Copy(BitConverter.GetBytes(declaredLength), 0, frame.Buffer, frame.BaseOffset, 4);
                frame.ByteCount = 4;
                Assert.ThrowsException<InvalidDataException>(() => Process(socket, frame));
            }
            finally { BufferManager.FreeBuffer(frame); }
        }

        [DataTestMethod]
        [DataRow(0)]
        [DataRow(1)]
        public void CountedSizeCannotBeShorterThanItsHeader(int declaredLength)
        {
            using var transport = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var socket = new LengthedSocket(transport, SizeType.Word, true);
            var frame = BufferManager.RequestBuffer();
            try
            {
                frame[0] = (byte)declaredLength;
                frame[1] = 0;
                frame.ByteCount = 2;
                socket.OnReceive = data => throw new InvalidOperationException("Invalid frame reached the handler");
                Assert.ThrowsException<InvalidDataException>(() => Process(socket, frame));
            }
            finally { BufferManager.FreeBuffer(frame); }
        }

        [TestMethod]
        public void UnsignedWordLengthAboveInt16MaximumIsPreserved()
        {
            using var transport = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var socket = new LengthedSocket(transport, SizeType.Word, true);
            var frame = BufferManager.RequestBuffer();
            try
            {
                frame[0] = 0;
                frame[1] = 128;
                frame.ByteCount = 32768;
                var calls = 0;
                socket.OnReceive = data => { calls++; Assert.AreEqual(32766, data.RemainingLength); };
                Assert.IsFalse(Process(socket, frame));
                Assert.AreEqual(1, calls);
            }
            finally { BufferManager.FreeBuffer(frame); }
        }

        [TestMethod]
        public void FailedDecryptionDoesNotReachPacketHandler()
        {
            using var transport = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var socket = new LengthedSocket(transport, SizeType.Dword, false);
            var frame = BufferManager.RequestBuffer();
            try
            {
                frame[0] = 4;
                frame.ByteCount = 8;
                var calls = 0;
                socket.OnDecrypt = data => false;
                socket.OnReceive = data => calls++;
                Assert.ThrowsException<InvalidDataException>(() => Process(socket, frame));
                Assert.AreEqual(0, calls);
            }
            finally { BufferManager.FreeBuffer(frame); }
        }

        private static bool Process(LengthedSocket socket, BufferData data)
        {
            using var args = new SocketAsyncEventArgs();
            args.SetBuffer(data.Buffer, data.BaseOffset, data.MaxLength);
            try
            {
                return (bool)typeof(LengthedSocket).GetMethod("ProcessInputBuffer", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(socket, new object[] { data, args });
            }
            catch (TargetInvocationException exception)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }

        [TestMethod]
        public void PartialHeaderOnlyPreparesContinuationForItsCurrentOwner()
        {
            using var transport = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var socket = new LengthedSocket(transport, SizeType.Dword, false);
            var frame = BufferManager.RequestBuffer();
            try
            {
                frame.ByteCount = 2;
                // No connection exists: parsing must leave receive submission to its caller,
                // which transfers ownership before a synchronous completion can run.
                Assert.IsTrue(Process(socket, frame));
            }
            finally { BufferManager.FreeBuffer(frame); }
        }

        [TestMethod]
        public void InvalidHeaderClosesOnlyItsSocketAndReturnsReceiveResources()
        {
            var available = AvailableEventArgs();
            using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);
            for (var attempt = 0; attempt < available + 2; attempt++)
            {
                using var peer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                peer.Connect(listener.LocalEndPoint);
                using var accepted = listener.Accept();
                var socket = new LengthedSocket(accepted, SizeType.Dword, false);
                using var failed = new ManualResetEventSlim();
                socket.OnError = args => failed.Set();
                socket.ReceiveAsync();
                peer.Send(BitConverter.GetBytes(65537u));
                Assert.IsTrue(failed.Wait(TimeSpan.FromSeconds(5)), "Invalid frame must close this connection");
                Assert.IsTrue(SpinWait.SpinUntil(() => AvailableEventArgs() == available, TimeSpan.FromSeconds(5)));
                Assert.IsTrue(accepted.SafeHandle.IsClosed);
                Assert.AreEqual(IPAddress.Loopback, socket.RemoteAddress);
            }
        }

        [TestMethod]
        public void CoalescedFrameFollowedByFragmentedMaximumFrameKeepsItsWholeBuffer()
        {
            var available = AvailableEventArgs();
            using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);
            using var peer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            peer.Connect(listener.LocalEndPoint);
            using var accepted = listener.Accept();
            var socket = new LengthedSocket(accepted, SizeType.Dword, false) { AutoReceive = false };
            using var received = new ManualResetEventSlim();
            var payloadLengths = new List<int>();
            socket.OnReceive = data =>
            {
                payloadLengths.Add(data.RemainingLength);
                if (payloadLengths.Count == 2)
                    received.Set();
            };
            var frames = new byte[BufferManager.BlockSize + 8];
            Array.Copy(BitConverter.GetBytes(4u), frames, 4);
            Array.Copy(BitConverter.GetBytes((uint)BufferManager.BlockSize - 4), 0, frames, 8, 4);
            // Queue the first frame together with the next header before starting receive.
            peer.Send(frames, 0, 12, SocketFlags.None);
            socket.ReceiveAsync();
            for (var sent = 12; sent < frames.Length;)
                sent += peer.Send(frames, sent, frames.Length - sent, SocketFlags.None);
            Assert.IsTrue(received.Wait(TimeSpan.FromSeconds(5)));
            CollectionAssert.AreEqual(new[] { 4, BufferManager.BlockSize - 4 }, payloadLengths);
            Assert.IsTrue(SpinWait.SpinUntil(() => AvailableEventArgs() == available, TimeSpan.FromSeconds(5)));
            socket.Close();
            Assert.IsTrue(accepted.SafeHandle.IsClosed);
            Assert.AreEqual(IPAddress.Loopback, socket.RemoteAddress);
        }

        private static int AvailableEventArgs()
        {
            var pool = (Stack<SocketAsyncEventArgs>)typeof(LengthedSocket)
                .GetField("_socketAsyncEventArgsPool", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            lock (pool)
                return pool.Count;
        }
    }
}
