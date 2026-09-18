using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game.Handlers;
using Rasa.Memory;
using Rasa.Packets;
using Rasa.Packets.MapChannel.Client;

namespace Rasa.Test
{
    /// <summary>
    /// RequestToolAction (524) had no handler: the terminator check then closed the connection.
    /// Ported from InfiniteRasa PR #95 / EllimistArcade 4885b05.
    /// </summary>
    [TestClass]
    public class ToolActionPacketTests
    {
        [TestMethod]
        public void RequestToolActionIsRegisteredSoAToolClickIsNotADisconnect()
        {
            var router = new PacketRouter<ClientPacketHandler, GameOpcode>();
            Assert.AreEqual(typeof(RequestToolActionPacket), router.GetPacketType(GameOpcode.RequestToolAction));
        }

        [TestMethod]
        public void RequestToolActionReadsAnEntityTarget()
        {
            var packet = new RequestToolActionPacket();
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            writer.WriteTuple(3);
            writer.WriteInt((int)ActionId.ToolHealingDisc);
            writer.WriteInt(1);
            writer.WriteLong(99);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            packet.Read(reader);
            Assert.AreEqual(ActionId.ToolHealingDisc, packet.ActionId);
            Assert.AreEqual(1u, packet.ActionArgId);
            Assert.IsTrue(packet.Target.HasEntity);
            Assert.AreEqual(99u, packet.Target.EntityId);
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void RequestToolActionReadsNoneAsNoTarget()
        {
            var packet = new RequestToolActionPacket();
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            writer.WriteTuple(3);
            writer.WriteInt((int)ActionId.ToolHarvest);
            writer.WriteInt((int)Harvest.SalvageArg);
            writer.WriteNoneStruct();
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            packet.Read(reader);
            Assert.AreEqual(ActionTargetKind.None, packet.Target.Kind);
            Assert.AreEqual(stream.Length, stream.Position);
        }
    }
}
