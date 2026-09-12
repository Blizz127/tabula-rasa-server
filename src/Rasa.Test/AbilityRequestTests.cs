using System;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Packets;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Protocol;
using Rasa.Structures;

namespace Rasa.Test
{
    [TestClass]
    public class AbilityRequestTests
    {
        [TestMethod]
        public void LightningRequiresLearnedRankAndPowerLogos()
        {
            var player = Player(1, 49, 194, 3);
            Assert.IsFalse(AbilityRequirements.CanUseSkillAbility(player, ActionId.AaRecruitLightning, 3));
            player.Logos.Add(23);
            Assert.IsTrue(AbilityRequirements.CanUseSkillAbility(player, ActionId.AaRecruitLightning, 1));
            Assert.IsTrue(AbilityRequirements.CanUseSkillAbility(player, ActionId.AaRecruitLightning, 3));
            Assert.IsFalse(AbilityRequirements.CanUseSkillAbility(player, ActionId.AaRecruitLightning, 4));
            player.Skills.Clear();
            Assert.IsFalse(AbilityRequirements.CanUseSkillAbility(player, ActionId.AaRecruitLightning, 1));
        }

        [DataTestMethod]
        [DataRow(-1)]
        [DataRow(0)]
        [DataRow(6)]
        [DataRow(int.MaxValue)]
        public void InvalidPumpNeverQueuesAnAbility(int rank)
        {
            var client = Client(Player(1, 165, 401, 5));
            Manager().RequestPerformAbility(client, new RequestPerformAbilityPacket
            {
                ActionId = ActionId.AaRecruitSprint, ActionArgId = rank
            });
            Assert.AreEqual(0, client.Player.MapChannel.PerformRecovery.Count);
            var failed = PopFailure(client);
            Assert.AreEqual(ActionId.AaRecruitSprint, failed.ActionId);
            Assert.AreEqual(rank, failed.ActionArgId);
        }

        [TestMethod]
        public void TacticalEvasionRequiresEveryLogosRegardlessOfCollectionOrder()
        {
            var player = Player(5, 54, 10000005, 5);
            player.Logos.AddRange(new uint[] { 4, 1, 7 });
            Assert.IsTrue(AbilityRequirements.CanUseSkillAbility(player, ActionId.AaRangerTacticalEvasion, 5));
            foreach (var id in new uint[] { 1, 7, 4 })
            {
                player.Logos.Remove(id);
                Assert.IsFalse(AbilityRequirements.CanUseSkillAbility(player, ActionId.AaRangerTacticalEvasion, 1));
                player.Logos.Add(id);
            }
            player.Class = 4;
            Assert.IsFalse(AbilityRequirements.CanUseSkillAbility(player, ActionId.AaRangerTacticalEvasion, 1));
        }

        [TestMethod]
        public void GrantedSignatureUsesOneRankAndDoesNotAllowCorruptSavedRanks()
        {
            var player = Player(12, 20, 137, 1);
            player.Logos.AddRange(new uint[] { 48, 6, 131, 125 });
            Assert.IsTrue(AbilityRequirements.CanUseSkillAbility(player, (ActionId)137, 1));
            Assert.IsFalse(AbilityRequirements.CanUseSkillAbility(player, (ActionId)137, 2));
            player.Skills[(SkillId)20].SkillLevel = 5;
            Assert.IsFalse(AbilityRequirements.CanUseSkillAbility(player, (ActionId)137, 1));
        }

        [TestMethod]
        public void CrouchingAllowsSprintButDeadPlayersCannotStartIt()
        {
            var player = Player(1, 165, 401, 1);
            player.IsCrouching = true;
            Assert.IsTrue(AbilityRequirements.CanUseSkillAbility(player, ActionId.AaRecruitSprint, 1));
            player.State = CharacterState.Dead;
            Assert.IsFalse(AbilityRequirements.CanUseSkillAbility(player, ActionId.AaRecruitSprint, 1));
        }

        [TestMethod]
        public void NonSkillActionsCannotEnterThroughAbilityRequest()
        {
            var client = Client(Player(1, 165, 401, 5));
            var manager = Manager();
            foreach (var action in new[] { ActionId.UseObject, ActionId.WeaponReload, (ActionId)int.MaxValue })
            {
                manager.RequestPerformAbility(client, new RequestPerformAbilityPacket { ActionId = action, ActionArgId = 1 });
                Assert.AreEqual(action, PopFailure(client).ActionId);
            }
            Assert.AreEqual(0, client.Player.MapChannel.PerformRecovery.Count);
        }

        [TestMethod]
        public void AuthorizedSkillRequestPreservesOpaqueTargetFieldsForRecovery()
        {
            // Synthetic transport fields test preservation, not target eligibility.
            var client = Client(Player(5, 54, 10000005, 3));
            client.Player.Logos.AddRange(new uint[] { 1, 7, 4 });
            var request = new RequestPerformAbilityPacket
            {
                ActionId = ActionId.AaRangerTacticalEvasion, ActionArgId = 2,
                TargetLocation = (100.25, -22.5, 300.75), ItemId = 0x100000002UL, ClientYaw = 1.25
            };
            Manager().RequestPerformAbility(client, request);
            var queued = client.Player.MapChannel.PerformRecovery;
            Assert.AreEqual(1, queued.Count);
            Assert.AreSame(client.Player, queued[0].Actor);
            Assert.AreEqual(2U, queued[0].ActionArgId);
            Assert.AreEqual(request.TargetLocation, queued[0].TargetLocation);
            Assert.AreEqual(request.ItemId, queued[0].ItemId);
            Assert.AreEqual(request.ClientYaw, queued[0].ClientYaw);
        }

        [TestMethod]
        public void FailedActionCarriesOriginalPairAndOptionalMessage()
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            new UserActionFailedPacket(ActionId.AaRecruitLightning, -1).Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual(194, reader.ReadInt());
            Assert.AreEqual(-1, reader.ReadInt());
            reader.ReadNoneStruct();
            Assert.AreEqual(stream.Length, stream.Position);
        }

        private static Manifestation Player(uint classId, int skillId, int abilityId, int rank)
        {
            var player = new Manifestation { Class = classId, State = CharacterState.Normal, MapChannel = new MapChannel() };
            player.Skills[(SkillId)skillId] = new SkillsData((SkillId)skillId, abilityId, rank);
            return player;
        }

        private static Client Client(Manifestation player) => new Client(null, new ClientPacketHandler())
        {
            State = ClientState.Ingame, Player = player
        };

        private static ManifestationManager Manager() => (ManifestationManager)Activator.CreateInstance(
            typeof(ManifestationManager), BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { null }, null);

        private static UserActionFailedPacket PopFailure(Client client)
        {
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(client);
            var protocol = (ProtocolPacket)queue.PopOutgoing();
            var call = (CallMethodMessage)protocol.Message;
            Assert.AreEqual(client.Player.EntityId, call.EntityId);
            Assert.AreEqual(GameOpcode.UserActionFailed, call.MethodId);
            return (UserActionFailedPacket)call.Packet;
        }
    }
}
