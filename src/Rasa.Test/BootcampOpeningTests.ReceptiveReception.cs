using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Packets.Mission.Server;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.World;

namespace Rasa.Test
{
    public partial class BootcampOpeningTests
    {
        [TestMethod]
        public void ReceptiveReceptionShrineUseAdvancesOnlyTheNamedShrineAction()
        {
            const uint missionId = 1069;
            const uint enhanceLogosId = 10;
            Assert.AreEqual((byte)ObjectiveBindingKind.LogosRecovered,
                _missions.LoadedMissions[missionId].Bindings.Single(row => row.ObjectiveId == 1).Kind);

            using (var unit = _factory.CreateChar())
            {
                unit.CharacterMissions.Add(new CharacterMissionEntry(CharacterId, missionId, (uint)MissionState.Active),
                    new[] { new CharacterMissionObjectiveEntry(CharacterId, missionId, 1, (uint)MissionObjectiveState.Incomplete) });
                unit.Complete();
            }

            _instance = new MapChannel
            {
                MapInfo = new MapInfo(1220, "wilderness", 1556, 0),
                ClientList = new List<Client>()
            };
            _client = new Client(_factory, new ClientPacketHandler()) { State = ClientState.Ingame };
            _client.Player = new Manifestation { Id = CharacterId, Level = 5, MapContextId = 1220, MapChannel = _instance };
            _client.Player.Logos.Add(enhanceLogosId); // Existing Logos still permits the mission objective to advance.
            using (var unit = _factory.CreateChar())
                _client.Player.Missions = _missions.LoadPlayerMissions(unit.CharacterMissions, AccountId, 1, CharacterId);
            _instance.ClientList.Add(_client);

            var other = new Logos(new LogosEntry { Id = 23, ClassId = 7302, MapContextId = 1220, Name = "Power", PosX = 1, PosY = 2, PosZ = 3 });
            var enhance = new Logos(new LogosEntry { Id = enhanceLogosId, ClassId = 7364, MapContextId = 1220, Name = "Enhance", PosX = 832, PosY = 161.838, PosZ = 960 });
            _instance.DynamicObjects.Add(other);
            _instance.DynamicObjects.Add(enhance);
            CellManager.Instance.AddToWorld(_instance, other);
            CellManager.Instance.AddToWorld(_instance, enhance);
            other.TriggeredByPlayers.Add(_client);
            enhance.TriggeredByPlayers.Add(_client);

            DynamicObjectManager.Instance.LogosRecovery(_instance,
                new ActionData(_client.Player, ActionId.UseObject, 6, 0) { SourceId = enhance.EntityId });

            Assert.IsTrue(other.TriggeredByPlayers.Contains(_client), "The other shrine's pending use belongs to its own action.");
            Assert.IsFalse(enhance.TriggeredByPlayers.Contains(_client));
            Assert.AreEqual(MissionObjectiveState.Completed, _client.Player.Missions[missionId].Objectives[1]);
            Assert.AreEqual(MissionObjectiveState.Incomplete, _client.Player.Missions[missionId].Objectives[2]);
            using (var unit = _factory.CreateChar())
            {
                var saved = unit.CharacterMissions.GetObjectives(CharacterId).Where(row => row.MissionId == missionId).ToArray();
                Assert.AreEqual((uint)MissionObjectiveState.Completed, saved.Single(row => row.ObjectiveId == 1).Status);
                Assert.AreEqual((uint)MissionObjectiveState.Incomplete, saved.Single(row => row.ObjectiveId == 2).Status);
            }
            var packets = Drain(_client);
            Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count());
            Assert.AreEqual(1, packets.OfType<ObjectiveRevealedPacket>().Count());
        }
    }
}
