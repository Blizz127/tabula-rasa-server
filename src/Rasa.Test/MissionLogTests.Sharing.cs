using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Game.Handlers;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets;
    using Rasa.Packets.Protocol;
    using Rasa.Packets.Mission.Server;
    using Rasa.Structures;

    /// <summary>
    /// Sharing a mission with the party: missionlog.pyo shows a shareable mission in the target's log and has
    /// accept/decline requests that name the sharer (AssignSharedMission / DeclineSharedMission). Both are implemented
    /// now (MissionManager.ShareMission and friends), so a shareable definition is no longer kept out of the game.
    /// </summary>
    public partial class MissionLogTests
    {
        private const uint PartyId = 77;
        private const uint FriendCharacterId = 102;

        private Client _friend;

        /// <summary>A second character in the same map channel, sharing a party with the first.</summary>
        private Client Friend()
        {
            _friend = new Client(_factory, new ClientPacketHandler()) { State = ClientState.Ingame };
            _map.ClientList.Add(_friend);
            _friend.Player = new Manifestation
            {
                Id = FriendCharacterId, Level = 1, Experience = 0, MapContextId = MapId, MapChannel = _map,
                Position = new Vector3(100f, 10f, 100f), Cells = new uint[,] { { 7 } }
            };
            _friend.Player.Credits[CurencyType.Credits] = 0;
            _friend.Player.PartyId = PartyId;
            _client.Player.PartyId = PartyId;

            PartyManager.Instance.Parties[PartyId] = new Party(PartyId, CharacterId, new List<PartyMember>
            {
                new PartyMember(CharacterId, "Recruit", 0, 1, false),
                new PartyMember(FriendCharacterId, "Friend", 0, 1, false)
            });

            return _friend;
        }

        private void Share(uint missionId = MissionId) => _missions.ShareMission(_client, missionId);

        /// <summary>The packets queued for another client (the harness's Drain only reads the first one's).</summary>
        private static List<PythonPacket> DrainOf(Client client)
        {
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(client);
            var packets = new List<PythonPacket>();
            while (queue.PopOutgoing() is ProtocolPacket packet)
                packets.Add(((CallMethodMessage)packet.Message).Packet);
            return packets;
        }

        [TestMethod]
        public void SharingPutsTheMissionInThePartyMembersLogWithAPendingShare()
        {
            var definition = Definition();
            definition.MissionConstantData.Shareable = true;
            _missions.LoadedMissions[MissionId] = definition;

            var friend = Friend();
            Accept();
            Drain();

            Share();

            // The share is in the friend's log, waiting on their answer, and the sharer is recorded on it.
            Assert.IsTrue(friend.Player.Missions.ContainsKey(MissionId));
            Assert.AreEqual(MissionState.Active, friend.Player.Missions[MissionId].State);
            Assert.AreEqual(CharacterId, friend.Player.PendingSharedMissions[MissionId]);
            // The gain packet goes to the member who was given the share, not to the sharer.
            Assert.AreEqual(1, DrainOf(friend).OfType<MissionGainedPacket>().Count());
        }

        [TestMethod]
        public void AcceptingAShareClearsIt()
        {
            var definition = Definition();
            definition.MissionConstantData.Shareable = true;
            _missions.LoadedMissions[MissionId] = definition;
            var friend = Friend();
            Accept();
            Share();
            Drain();

            _missions.AssignSharedMission(friend, CharacterId, MissionId);

            Assert.IsTrue(friend.Player.Missions.ContainsKey(MissionId));
            Assert.IsFalse(friend.Player.PendingSharedMissions.ContainsKey(MissionId));
        }

        [TestMethod]
        public void DecliningAShareTakesTheMissionBackOut()
        {
            var definition = Definition();
            definition.MissionConstantData.Shareable = true;
            _missions.LoadedMissions[MissionId] = definition;
            var friend = Friend();
            Accept();
            Share();
            Drain();

            _missions.DeclineSharedMission(friend, CharacterId, MissionId);

            Assert.IsFalse(friend.Player.Missions.ContainsKey(MissionId));
            Assert.IsFalse(friend.Player.PendingSharedMissions.ContainsKey(MissionId));
        }

        [TestMethod]
        public void OnlyThePlayerWhoSharedCanHaveTheirShareAnswered()
        {
            var definition = Definition();
            definition.MissionConstantData.Shareable = true;
            _missions.LoadedMissions[MissionId] = definition;
            var friend = Friend();
            Accept();
            Share();

            // A decline naming somebody else is not a request the client could make for this share.
            _missions.DeclineSharedMission(friend, CharacterId + 1, MissionId);

            Assert.IsTrue(friend.Player.Missions.ContainsKey(MissionId));
            Assert.AreEqual(CharacterId, friend.Player.PendingSharedMissions[MissionId]);
        }

        [TestMethod]
        public void AMissionThatIsNotShareableCannotBeShared()
        {
            Definition();
            var friend = Friend();
            Accept();

            Share();

            Assert.IsFalse(friend.Player.Missions.ContainsKey(MissionId));
            Assert.AreEqual(0, friend.Player.PendingSharedMissions.Count);
        }

        [TestMethod]
        public void SharingNeedsAPartyAndAnActiveMission()
        {
            var definition = Definition();
            definition.MissionConstantData.Shareable = true;
            _missions.LoadedMissions[MissionId] = definition;
            var friend = Friend();

            // Not accepted yet: there is nothing to share.
            Share();
            Assert.IsFalse(friend.Player.Missions.ContainsKey(MissionId));

            // Accepted, but with no party there is nobody to share it with.
            Accept();
            _client.Player.PartyId = 0;
            Share();
            Assert.IsFalse(friend.Player.Missions.ContainsKey(MissionId));
        }
    }
}
