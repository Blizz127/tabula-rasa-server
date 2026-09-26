using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Packets.Mission.Server;
using Rasa.Structures;
using Rasa.Structures.Char;

namespace Rasa.Test
{
    public partial class BootcampOpeningTests
    {
        private const uint LurkingInTheShadows = 427;

        [TestMethod]
        public void SeededFulgorKillAdvancesLurkingInTheShadowsToCaufield()
        {
            var definition = _missions.LoadedMissions[LurkingInTheShadows];
            var binding = definition.Bindings.Single(row => row.ObjectiveId == 6);
            Assert.AreEqual((byte)ObjectiveBindingKind.Kill, binding.Kind);
            Assert.AreEqual(76u, binding.CreatureId);
            Assert.AreEqual(0u, binding.PlacementId);

            using (var unit = _factory.CreateChar())
            {
                unit.CharacterMissions.Add(new CharacterMissionEntry(CharacterId, LurkingInTheShadows, (uint)MissionState.Active), new[]
                {
                    new CharacterMissionObjectiveEntry(CharacterId, LurkingInTheShadows, 1, (uint)MissionObjectiveState.Completed),
                    new CharacterMissionObjectiveEntry(CharacterId, LurkingInTheShadows, 6, (uint)MissionObjectiveState.Incomplete)
                });
                unit.Complete();
            }

            _client = new Client(_factory, new ClientPacketHandler()) { State = ClientState.Ingame };
            _client.Player = new Manifestation { Id = CharacterId, Level = 5, MapContextId = 1220 };
            using (var unit = _factory.CreateChar())
                _client.Player.Missions = _missions.LoadPlayerMissions(unit.CharacterMissions, AccountId, 1, CharacterId);

            var boss = new Creature { DbId = 76, State = CharacterState.Dead };
            _missions.OnCreatureKilled(_client, boss);

            using (var unit = _factory.CreateChar())
            {
                var saved = unit.CharacterMissions.GetObjectives(CharacterId).Where(row => row.MissionId == LurkingInTheShadows).ToArray();
                Assert.AreEqual((uint)MissionObjectiveState.Completed, saved.Single(row => row.ObjectiveId == 6).Status);
                Assert.AreEqual((uint)MissionObjectiveState.Incomplete, saved.Single(row => row.ObjectiveId == 7).Status);
            }
            Assert.AreEqual(MissionObjectiveState.Completed, _client.Player.Missions[LurkingInTheShadows].Objectives[6]);
            Assert.AreEqual(MissionObjectiveState.Incomplete, _client.Player.Missions[LurkingInTheShadows].Objectives[7]);
            Assert.AreEqual(MissionState.Active, _client.Player.Missions[LurkingInTheShadows].State,
                "Fulgor's death must reveal the return to Caufield, not pay the mission early.");
            Assert.AreEqual(0u, _client.Player.Experience);
            var packets = Drain(_client);
            Assert.AreEqual(1, packets.OfType<ObjectiveCompletedPacket>().Count());
            Assert.AreEqual(1, packets.OfType<ObjectiveRevealedPacket>().Count());

            _missions.OnCreatureKilled(_client, boss);
            Assert.IsFalse(Drain(_client).Any(packet => packet is ObjectiveCompletedPacket || packet is ObjectiveRevealedPacket),
                "A repeated kill notification must not advance this mission twice.");
        }
    }
}
