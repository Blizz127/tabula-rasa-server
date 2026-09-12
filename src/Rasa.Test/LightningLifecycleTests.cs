using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Packets;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.MapChannel.Server.PerformRecovery;
using Rasa.Packets.Protocol;
using Rasa.Structures;

namespace Rasa.Test
{
    [TestClass]
    [DoNotParallelize]
    public class LightningLifecycleTests
    {
        private long _now;
        private ActorActionManager _actions;
        private Client _client;
        private Creature _target;
        private MapChannel _map;

        [TestInitialize]
        public void Initialize()
        {
            _now = 0;
            _actions = new ActorActionManager(() => _now);
            _map = new MapChannel { MapInfo = new MapInfo(1220, "Test", 1, 1), ClientList = new List<Client>() };
            var player = new Manifestation
            {
                Class = 1, Level = 1, State = CharacterState.Normal, MapContextId = 1220,
                MapChannel = _map, Cells = new uint[1, 1], MovementSpeed = 1
            };
            player.Attributes[Attributes.Power] = new ActorAttributes(Attributes.Power, 1000, 1000, 1000, 0, 1);
            player.Skills[(SkillId)49] = new SkillsData((SkillId)49, 194, 5);
            player.Logos.Add(23);
            _client = new Client(null, new ClientPacketHandler()) { Player = player, State = ClientState.Ingame };
            _map.ClientList.Add(_client);
            _map.MapCellInfo.Cells[0] = new MapCell { ClientList = new List<Client> { _client } };
            _target = MakeTarget();
            EntityManager.Instance.RegisterEntity(_target.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(_target);
        }

        [TestCleanup]
        public void Cleanup()
        {
            EntityManager.Instance.UnregisterEntity(_target.EntityId);
            EntityManager.Instance.UnregisterCreature(_target.EntityId);
        }

        [DataTestMethod]
        [DataRow(1, 25)]
        [DataRow(2, 50)]
        [DataRow(3, 75)]
        [DataRow(4, 100)]
        [DataRow(5, 150)]
        public void OriginalWindupThenOneRankCostAndRecovery(int rank, int cost)
        {
            Assert.IsTrue(Start(rank));
            Assert.AreEqual(1000, Power);
            Assert.AreEqual(1, Drain().OfType<PerformWindupPacket>().Count());
            Advance(499);
            Assert.AreEqual(100000, Health);
            Assert.AreEqual(1000, Power);
            Assert.AreEqual(0, Drain().Count);

            Advance(500);
            Assert.AreEqual(1000 - cost, Power);
            Assert.IsTrue(Health < 100000);
            var packets = Drain();
            Assert.AreEqual(1, packets.OfType<LightningRecovery>().Count());
            Assert.AreEqual(1900L, packets.OfType<ActionReuseTimesPacket>().Single().ReuseTimes.Single().RemainingMilliseconds);
            Assert.AreEqual(2400L, _client.Player.AbilityReuseDeadlines[ActionId.AaRecruitLightning]);
            var health = Health;
            Advance(501);
            Assert.AreEqual(1000 - cost, Power);
            Assert.AreEqual(health, Health);
            Assert.AreEqual(0, Drain().Count);
        }

        [TestMethod]
        public void RecoveryBlocksOtherActionsAndReuseIsSharedAcrossRanks()
        {
            Assert.IsTrue(Start());
            Assert.IsFalse(Start(5));
            Assert.IsFalse(ManifestationManager.Instance.PlayerTryFireWeapon(_client));
            Advance(500);
            Advance(1199);
            Assert.IsFalse(_actions.CanBeginAbility(_client.Player));
            Advance(1200);
            Assert.IsTrue(_actions.CanBeginAbility(_client.Player));
            Assert.IsFalse(Start(5));
            Advance(2399);
            Assert.IsFalse(Start(5));
            Advance(2400);
            Assert.IsTrue(Start(5));
        }

        [TestMethod]
        public void MatchingInterruptCancelsWindupAndUnresolvedRequestWithoutDamageOrCost()
        {
            Assert.IsTrue(Start());
            Drain();
            Advance(499);
            ActorManager.Instance.RequestActionInterrupt(_client,
                new RequestActionInterruptPacket { ActionId = ActionId.AaRecruitLightning, ActionArgId = 1 });
            Assert.IsNull(_client.Player.CurrentAbility);
            var packets = Drain();
            Assert.AreEqual(1, packets.OfType<ActionInterruptPacket>().Count());
            Assert.AreEqual(1, packets.OfType<UserActionFailedPacket>().Count());
            Assert.AreEqual(0L, packets.OfType<ActionReuseTimesPacket>().Single().ReuseTimes.Single().RemainingMilliseconds);
            Advance(500);
            Assert.AreEqual(100000, Health);
            Assert.AreEqual(1000, Power);
            Assert.IsFalse(_client.Player.AbilityReuseDeadlines.ContainsKey(ActionId.AaRecruitLightning));
            Assert.IsTrue(Start());
        }

        [TestMethod]
        public void InterruptedRecoveryKeepsTheReuseDeadlineAndDoesNotDiscardAnotherPendingRequest()
        {
            Assert.IsTrue(Start());
            Advance(500);
            Drain();
            Assert.IsTrue(_actions.InterruptAbility(_client, ActionId.AaRecruitLightning, 1));
            var packets = Drain();
            Assert.AreEqual(1, packets.OfType<ActionInterruptPacket>().Count());
            Assert.AreEqual(0, packets.OfType<UserActionFailedPacket>().Count());
            Assert.AreEqual(975, Power);
            Assert.IsFalse(Start(2));
            Advance(2400);
            Assert.IsTrue(Start(2));
        }

        [TestMethod]
        public void WrongRankAndAnotherActorCannotCancelTheCast()
        {
            Assert.IsTrue(Start());
            Assert.IsFalse(_actions.InterruptAbility(_client, ActionId.AaRecruitLightning, 2));
            var other = new Client(null, new ClientPacketHandler())
            {
                State = ClientState.Ingame, Player = new Manifestation { MapChannel = _map }
            };
            ActorManager.Instance.RequestActionInterrupt(other,
                new RequestActionInterruptPacket { ActionId = ActionId.AaRecruitLightning, ActionArgId = 1 });
            Advance(500);
            Assert.AreEqual(975, Power);
        }

        [TestMethod]
        public void MovingDoesNotInterruptLightning()
        {
            Assert.IsTrue(Start());
            _client.Player.Position = new Vector3(10, 0, 0);
            _client.Player.IsRunning = true;
            Advance(500);
            Assert.AreEqual(975, Power);
            Assert.IsTrue(Health < 100000);
        }

        [TestMethod]
        public void MissingFriendlyAndOtherMapTargetsAreRejectedBeforeWindup()
        {
            var request = Request();
            request.Target = null;
            Assert.IsFalse(_actions.TryStartLightning(_client, request));
            _target.Faction = Factions.AFS;
            Assert.IsFalse(Start());
            _target.Faction = Factions.Bane;
            _target.MapContextId = 1148;
            Assert.IsFalse(Start());
            Assert.AreEqual(0, Drain().Count);
            Assert.AreEqual(1000, Power);
        }

        [TestMethod]
        public void ReusedTargetIdentityDuringWindupDoesNotReceiveTheOldCast()
        {
            Assert.IsTrue(Start());
            var replacement = MakeTarget();
            replacement.EntityId = _target.EntityId;
            EntityManager.Instance.UnregisterCreature(_target.EntityId);
            EntityManager.Instance.RegisterCreature(replacement);
            Drain();
            Advance(500);
            Assert.AreEqual(100000, replacement.Attributes[Attributes.Health].Current);
            Assert.AreEqual(1000, Power);
            AssertFailureAndNoRecovery();
        }

        [DataTestMethod]
        [DataRow("power")]
        [DataRow("logos")]
        [DataRow("target_dead")]
        [DataRow("actor_dead")]
        public void RecoveryRechecksEligibilityWithoutChargingAFailedCast(string changed)
        {
            Assert.IsTrue(Start());
            Drain();
            if (changed == "power") _client.Player.Attributes[Attributes.Power].Current = 24;
            if (changed == "logos") _client.Player.Logos.Clear();
            if (changed == "target_dead") _target.State = CharacterState.Dead;
            if (changed == "actor_dead") _client.Player.State = CharacterState.Dead;
            Advance(500);
            Assert.AreEqual(changed == "power" ? 24 : 1000, Power);
            Assert.AreEqual(100000, Health);
            AssertFailureAndNoRecovery();
        }

        [TestMethod]
        public void ADelayedMapTickResolvesOnceAndRetainsTheElapsedDeadline()
        {
            Assert.IsTrue(Start());
            Drain();
            Advance(10000);
            Assert.AreEqual(975, Power);
            Assert.IsNull(_client.Player.CurrentAbility);
            var packets = Drain();
            Assert.AreEqual(1, packets.OfType<LightningRecovery>().Count());
            Assert.AreEqual(0L, packets.OfType<ActionReuseTimesPacket>().Single().ReuseTimes.Single().RemainingMilliseconds);
            Assert.IsTrue(Start(2));
        }

        [TestMethod]
        public void SkillRequestDispatchStartsATimelineInsteadOfAnImmediateDamageQueue()
        {
            ManifestationManager.Instance.RequestPerformAbility(_client, Request());
            Assert.IsNotNull(_client.Player.CurrentAbility);
            Assert.AreEqual(0, _map.PerformRecovery.Count);
            Assert.AreEqual(1000, Power);
            Assert.AreEqual(100000, Health);
        }

        [TestMethod]
        public void RejectedAdmissionCancelsClientPredictionAndItsPendingRequest()
        {
            _client.Player.Attributes[Attributes.Power].Current = 24;
            ManifestationManager.Instance.RequestPerformAbility(_client, Request());
            Assert.IsNull(_client.Player.CurrentAbility);
            AssertFailureAndNoRecovery();
        }

        [TestMethod]
        public void RejectionSynchronizesTheSurvivingServerCooldown()
        {
            _client.Player.AbilityReuseDeadlines[ActionId.AaRecruitLightning] = Environment.TickCount64 + 1000000;
            ManifestationManager.Instance.RequestPerformAbility(_client, Request());
            var packets = Drain();
            var remaining = packets.OfType<ActionReuseTimesPacket>().Single().ReuseTimes.Single().RemainingMilliseconds;
            Assert.IsTrue(remaining > 0 && remaining <= 1000000);
            Assert.AreEqual(1, packets.OfType<ActionFailedPacket>().Count());
            Assert.IsNull(_client.Player.CurrentAbility);
        }

        [TestMethod]
        public void DuplicateAcceptedPairDoesNotDiscardTheEarlierClientRequest()
        {
            Assert.IsTrue(Start());
            var accepted = _client.Player.CurrentAbility;
            Drain();
            ManifestationManager.Instance.RequestPerformAbility(_client, Request());
            Assert.AreSame(accepted, _client.Player.CurrentAbility);
            Assert.AreEqual(0, Drain().Count);
            Advance(500);
            Assert.AreEqual(975, Power);
            Assert.AreEqual(1, Drain().OfType<LightningRecovery>().Count());
        }

        [TestMethod]
        public void LegacyInterruptedActionNeverPerformsSuccessfulRecovery()
        {
            _client.Player.WeaponReady = false;
            _map.PerformRecovery.Add(new ActionData(_client.Player, ActionId.WeaponDraw, 1, 1000));
            ActorManager.Instance.RequestActionInterrupt(_client,
                new RequestActionInterruptPacket { ActionId = ActionId.WeaponDraw, ActionArgId = 1 });
            Advance(1);
            Assert.AreEqual(0, _map.PerformRecovery.Count);
            Assert.IsFalse(_client.Player.WeaponReady);
            var packets = Drain();
            Assert.AreEqual(1, packets.OfType<ActionInterruptPacket>().Count());
            Assert.AreEqual(1, packets.OfType<UserActionFailedPacket>().Count());
            Assert.AreEqual(0, packets.OfType<PerformRecoveryPacket>().Count());
        }

        [TestMethod]
        public void CancelledObjectUseReleasesOnlyThatObjectsTriggerWithoutChangingOwnership()
        {
            var selected = new DynamicObject { Faction = Factions.Bane };
            var unrelated = new DynamicObject { Faction = Factions.AFS };
            selected.TriggeredByPlayers.Add(_client);
            unrelated.TriggeredByPlayers.Add(_client);
            _map.DynamicObjects.Add(selected);
            _map.DynamicObjects.Add(unrelated);
            _map.PerformRecovery.Add(new ActionData(_client.Player, ActionId.UseObject, 7, 10000)
            {
                SourceId = selected.EntityId
            });
            ActorManager.Instance.RequestActionInterrupt(_client,
                new RequestActionInterruptPacket { ActionId = ActionId.UseObject, ActionArgId = 7 });
            Advance(1);
            Assert.AreEqual(0, selected.TriggeredByPlayers.Count);
            Assert.AreEqual(1, unrelated.TriggeredByPlayers.Count);
            Assert.AreEqual(Factions.Bane, selected.Faction);
            Assert.AreEqual(0, Drain().OfType<PerformRecoveryPacket>().Count());
        }

        private int Power => _client.Player.Attributes[Attributes.Power].Current;
        private int Health => _target.Attributes[Attributes.Health].Current;
        private bool Start(int rank = 1) => _actions.TryStartLightning(_client, Request(rank));
        private RequestPerformAbilityPacket Request(int rank = 1) => new RequestPerformAbilityPacket
        {
            ActionId = ActionId.AaRecruitLightning, ActionArgId = rank, Target = _target.EntityId
        };
        private void Advance(long now)
        {
            var delta = now - _now;
            _now = now;
            _actions.DoWork(_map, delta);
        }
        private static Creature MakeTarget()
        {
            var creature = new Creature { MapContextId = 1220, Level = 1, Faction = Factions.Bane, State = CharacterState.Normal, Cells = new uint[1, 1] };
            creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100000, 100000, 100000, 0, 0);
            return creature;
        }
        private void AssertFailureAndNoRecovery()
        {
            Assert.IsNull(_client.Player.CurrentAbility);
            var packets = Drain();
            Assert.AreEqual(1, packets.OfType<ActionFailedPacket>().Count());
            Assert.AreEqual(1, packets.OfType<UserActionFailedPacket>().Count());
            Assert.AreEqual(0, packets.OfType<LightningRecovery>().Count());
            Assert.AreEqual(0L, packets.OfType<ActionReuseTimesPacket>().Single().ReuseTimes.Single().RemainingMilliseconds);
        }
        private List<PythonPacket> Drain()
        {
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_client);
            var packets = new List<PythonPacket>();
            while (queue.PopOutgoing() is ProtocolPacket packet)
                packets.Add(((CallMethodMessage)packet.Message).Packet);
            return packets;
        }
    }
}
