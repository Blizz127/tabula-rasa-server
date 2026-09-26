using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Packets.Game.Server;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Mission.Server;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Test
{
    public partial class NewCharacterTests
    {
        [DataTestMethod]
        [DataRow("1992")]
        [DataRow("0")]
        [DataRow("1,992")]
        public void ReconnectLoadsSavedMissionsBeforeDeferredControllerAndPreservesTrackingChoice(string savedTracking)
        {
            _client.State = ClientState.Ingame;
            _client.Player = new Manifestation
            {
                Id = 101, Level = 1, EntityClass = Items[0].Class,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>(),
                MapChannel = new MapChannel { MapInfo = new MapInfo(1985, "mission admission", 1, 1) }
            };
            _client.Player.Inventory.EquippedInventory.AddRange(new ulong[22]);
            _client.Player.Missions[1992] = new PlayerMission
            {
                MissionId = 1992, State = MissionState.Active,
                Objectives =
                {
                    [9] = MissionObjectiveState.Completed, [8] = MissionObjectiveState.Incomplete
                }
            };
            using (var unit = _factory.CreateChar())
            {
                unit.CharacterOptions.AddOrUpdate(101, (uint)CharacterOption.MissionTrack0, savedTracking);
                unit.Complete();
            }
            var missions = new MissionManager(_factory);
            var definition = new Mission(new NpcMissionEntry { Id = 1992 });
            definition.Objectives[9] = new MissionObjectiveDefinition { ObjectiveId = 9, Ordinal = 1, IsRequired = true };
            definition.Objectives[8] = new MissionObjectiveDefinition { ObjectiveId = 8, Ordinal = 2, IsRequired = true };
            missions.LoadedMissions[1992] = definition;
            var singleton = typeof(MissionManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = singleton.GetValue(null);
            singleton.SetValue(null, missions);
            try
            {
                new ManifestationManager(_factory, new WeaponAttackManager(_factory, () => 0)).InitializePlayerControl(_client);
                var packets = Drain();
                var options = packets.OfType<CharacterOptionsPacket>().Single();
                var control = packets.OfType<SetControlledActorIdPacket>().Single();
                var owner = packets.OfType<CreatePhysicalEntityPacket>().Single();
                Assert.IsTrue(packets.IndexOf(options) < packets.IndexOf(control));
                Assert.IsTrue(packets.IndexOf(control) < packets.IndexOf(owner));
                var initial = owner.EntityData.OfType<MissionStatusInfoPacket>().Single();
                Assert.IsTrue(owner.EntityData.IndexOf(initial) < owner.EntityData.FindIndex(p => p is WorldLocationDescriptorPacket));
                var mission = initial.MissionStatusDict[1992];
                Assert.AreEqual(MissionState.Active, mission.MissionState);
                Assert.AreEqual((uint)MissionObjectiveState.Completed, mission.ObjectivesList.Single(o => o.ObjectiveId == 9).ObjectiveStatus);
                Assert.AreEqual((uint)MissionObjectiveState.Incomplete, mission.ObjectivesList.Single(o => o.ObjectiveId == 8).ObjectiveStatus);
                Assert.AreEqual(savedTracking, options.OptionsList.Single(o => o.OptionId == CharacterOption.MissionTrack0).Value);
                Assert.AreEqual(1, options.OptionsList.Count, "Do not invent tracked slots or normalize native option text.");
                using var unit = _factory.CreateChar();
                Assert.AreEqual(savedTracking, unit.CharacterOptions.Get(101).Single().Value);
                Assert.AreEqual(0, packets.OfType<MissionGainedPacket>().Count(), "Reconnection must not replay mission acceptance.");
            }
            finally
            {
                singleton.SetValue(null, previous);
            }
        }
    }
}
