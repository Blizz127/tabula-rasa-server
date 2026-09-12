using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Memory;
using Rasa.Packets.Mission.Server;
using Rasa.Structures;

namespace Rasa.Test
{
    [TestClass]
    public class MissionSerializationTests
    {
        [DataTestMethod]
        [DataRow(0U)]
        [DataRow(30U)]
        public void MissionGainedCarriesFullCategoryTimestampAndDistinctMarkerAxes(uint remaining)
        {
            var mission = new MissionInfo { ChangeTime = 12345 };
            mission.MissionConstantData.CategoryId = 10000044;
            mission.ObjectivesList.Add(new MissionObjective
            {
                ObjectiveId = 5,
                TimeRemaining = remaining,
                CounterDict = new Dictionary<uint, MissionObjectiveGenericCounter>
                {
                    { 10, new MissionObjectiveGenericCounter { Count = 2, InitialCount = 0, TargetCount = 5 } }
                },
                ItemCounters = new Dictionary<uint, MissionObjectiveCounter>
                {
                    { 27120, new MissionObjectiveCounter { Count = 2, MaxCount = 3 } }
                },
                IsRequired = true,
                IndicatorList = new List<MissionIndicator>
                {
                    new MissionIndicator
                    {
                        Position = new Vector3(101.25f, -42.5f, 303.75f),
                        Radius = 8.5,
                        IndicatorId = 7,
                        Show3DEffect = true
                    }
                }
            });

            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            new MissionGainedPacket(429, mission).Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(429U, reader.ReadUInt());
            ReadMissionHeader(reader, 10000044, 12345);
            Assert.AreEqual(1, reader.ReadList());
            ReadObjectiveHeader(reader, 5, remaining);
            Assert.AreEqual(1, reader.ReadDictionary());
            Assert.AreEqual(10U, reader.ReadUInt());
            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual(2, reader.ReadInt());
            Assert.AreEqual(0, reader.ReadInt());
            Assert.AreEqual(5, reader.ReadInt());
            Assert.AreEqual(1, reader.ReadDictionary());
            Assert.AreEqual(27120U, reader.ReadUInt());
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(2U, reader.ReadUInt());
            Assert.AreEqual(3U, reader.ReadUInt());
            Assert.IsTrue(reader.ReadBool());
            Assert.AreEqual(1, reader.ReadList());
            Assert.AreEqual(4, reader.ReadTuple());
            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual(101.25, reader.ReadDouble());
            Assert.AreEqual(-42.5, reader.ReadDouble());
            Assert.AreEqual(303.75, reader.ReadDouble());
            Assert.AreEqual(8.5, reader.ReadDouble());
            Assert.AreEqual(7U, reader.ReadUInt());
            Assert.IsTrue(reader.ReadBool());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void MissionStatusSupportsObjectivesWithoutIndicatorsAndMultipleMissions()
        {
            var first = new MissionInfo();
            first.ObjectivesList.Add(new MissionObjective { ObjectiveId = 4 });
            var missions = new Dictionary<uint, MissionInfo> { { 429, first }, { 321, new MissionInfo() } };
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            new MissionStatusInfoPacket(missions).Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(1, reader.ReadTuple());
            Assert.AreEqual(2, reader.ReadDictionary());
            Assert.AreEqual(429U, reader.ReadUInt());
            ReadMissionHeader(reader, 0, 0);
            Assert.AreEqual(1, reader.ReadList());
            ReadObjectiveHeader(reader, 4);
            Assert.AreEqual(0, reader.ReadDictionary()); // generic counters are always a mapping.
            Assert.AreEqual(0, reader.ReadDictionary());
            Assert.IsFalse(reader.ReadBool());
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(321U, reader.ReadUInt());
            ReadMissionHeader(reader, 0, 0);
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        private static void ReadMissionHeader(PythonReader reader, uint category, int changeTime)
        {
            Assert.AreEqual(5, reader.ReadTuple());
            Assert.AreEqual(0, reader.ReadInt());
            Assert.IsFalse(reader.ReadBool());
            Assert.AreEqual(6, reader.ReadTuple());
            Assert.AreEqual(0U, reader.ReadUInt());
            Assert.AreEqual(0U, reader.ReadUInt());
            Assert.AreEqual(category, reader.ReadUInt());
            Assert.IsFalse(reader.ReadBool());
            Assert.IsFalse(reader.ReadBool());
            Assert.AreEqual(2, reader.ReadTuple()); // rewards
            Assert.AreEqual(2, reader.ReadTuple()); // fixed rewards
            Assert.AreEqual(0, reader.ReadList()); // currencies
            Assert.AreEqual(0, reader.ReadList()); // fixed items
            Assert.AreEqual(0, reader.ReadList()); // selectable items
            Assert.AreEqual(changeTime, reader.ReadInt());
        }

        private static void ReadObjectiveHeader(PythonReader reader, uint objective, uint? timeRemaining = null)
        {
            Assert.AreEqual(8, reader.ReadTuple());
            Assert.AreEqual(objective, reader.ReadUInt());
            Assert.AreEqual(0U, reader.ReadUInt());
            Assert.AreEqual(0U, reader.ReadUInt());
            if (timeRemaining.HasValue)
                Assert.AreEqual(timeRemaining.Value, reader.ReadUInt());
            else
                reader.ReadNoneStruct();
        }
    }
}
