using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game.Handlers;
using Rasa.Memory;
using Rasa.Packets;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Protocol;
using Rasa.Structures;

namespace Rasa.Test
{
    [TestClass]
    public class ModuleTooltipPacketTests
    {
        [TestMethod]
        public void TrainingDayDefinitionsUseOriginalEffectTypesAndFilmedAmountAndDuration()
        {
            foreach (var (moduleId, effectId) in new[] { (900221, 9), (900256, 115) })
            {
                var definition = MissionRewardModuleBindings.DefinitionFor(moduleId);
                Assert.IsNotNull(definition);
                Assert.AreEqual(moduleId, definition.ModuleId);
                Assert.IsNull(definition.ModuleLevel);
                Assert.AreEqual(1, definition.Effects.Count);
                var effect = definition.Effects[0];
                Assert.AreEqual((effectId, 0, -15d, 0d, 0d, (int?)null, (int?)15, (int?)null, (int?)null),
                    (effect.EffectId, effect.SetLevel, effect.FlatValue, effect.LinearValue, effect.ExpValue,
                        effect.Arg1, effect.Arg2, effect.Arg3, effect.Arg4));
            }

            Assert.IsNull(MissionRewardModuleBindings.DefinitionFor(900222));
        }

        [TestMethod]
        public void ResponseIsACollectionOfCompleteNineFieldEffects()
        {
            // Deliberately synthetic, distinct values expose shifted fields and old self-assignments.
            var effects = new List<ModuleInfo>
            {
                new ModuleInfo(701, 2, 3, 4, 5, 6, 7, 8, 9),
                new ModuleInfo(702, 0, -2.5, 0.25, 1.5, 0, null, -3, 14)
            };
            var module = new ItemModule(12345, 7, effects);
            var packet = new ModuleTooltipInfoPacket(module);
            effects.Clear(); // A queued response must retain its original rows.

            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            packet.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(GameOpcode.ModuleTooltipInfo, packet.Opcode);
            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual(12345, reader.ReadInt());
            Assert.AreEqual(7, reader.ReadInt(), "The supplied module level must survive construction.");
            Assert.AreEqual(2, reader.ReadList(), "The original consumer iterates effect rows before unpacking.");
            Assert.AreEqual(9, reader.ReadTuple());
            foreach (var expected in new[] { 701, 2, 3, 4, 5, 6, 7, 8, 9 })
                Assert.AreEqual(expected, reader.ReadInt());
            Assert.AreEqual(9, reader.ReadTuple());
            Assert.AreEqual(702, reader.ReadInt());
            Assert.AreEqual(0, reader.ReadInt());
            Assert.AreEqual(-2.5, reader.ReadDouble());
            Assert.AreEqual(0.25, reader.ReadDouble());
            Assert.AreEqual(1.5, reader.ReadDouble());
            Assert.AreEqual(0, reader.ReadInt());
            reader.ReadNoneStruct();
            Assert.AreEqual(-3, reader.ReadInt());
            Assert.AreEqual(14, reader.ReadInt(), "Argument four must preserve its supplied value.");
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void KnownEmptyDefinitionAndUnknownLevelRemainDistinctWireValues()
        {
            var packet = new ModuleTooltipInfoPacket(new ItemModule(17, null, Array.Empty<ModuleInfo>()));
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            packet.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual(17, reader.ReadInt());
            reader.ReadNoneStruct();
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void NullEffectDefinitionCannotBecomeAnEmptyCachedResponse()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
                new ItemModule(17, null, (IEnumerable<ModuleInfo>)null));
            Assert.ThrowsException<ArgumentException>(() =>
                new ItemModule(17, null, new ModuleInfo[] { null }));
        }

        [TestMethod]
        public void OriginalRequestRoutesAndReadsExactlyOneIntegerId()
        {
            var router = new PacketRouter<ClientPacketHandler, GameOpcode>();
            Assert.AreEqual(typeof(RequestTooltipForModuleIdPacket),
                router.GetPacketType(GameOpcode.RequestTooltipForModuleId));
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            writer.WriteTuple(1);
            writer.WriteInt(900312);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            var packet = new RequestTooltipForModuleIdPacket();
            packet.Read(reader);
            Assert.AreEqual(900312, packet.ModuleId);
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [DataTestMethod]
        [DataRow(0, false)]
        [DataRow(2, false)]
        [DataRow(1, true)]
        public void MalformedRequestShapeCannotReachTheModuleHandler(int tupleLength, bool nonInteger)
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            writer.WriteTuple(tupleLength);
            if (nonInteger)
                writer.WriteString("900312");
            else
                for (var index = 0; index < tupleLength; index++)
                    writer.WriteInt(900312);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.ThrowsException<InvalidClientMessageException>(() =>
                new RequestTooltipForModuleIdPacket().Read(reader));
        }
    }
}
