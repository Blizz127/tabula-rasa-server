using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Packets.Game.Server;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Mission.Server;
using Rasa.Structures;

namespace Rasa.Test
{
    public partial class NewCharacterTests
    {
        [DataTestMethod]
        [DataRow(ClientState.Ingame, CharacterState.Normal)]
        [DataRow(ClientState.Ingame, CharacterState.Dead)]
        [DataRow(ClientState.Teleporting, CharacterState.Normal)]
        [DataRow(ClientState.Teleporting, CharacterState.Dead)]
        public void WorldIntroductionAssignsControlBeforeOwnOrNearbyEquipmentEvents(ClientState sessionState, CharacterState actorState)
        {
            var map = new MapChannel { MapInfo = new MapInfo(1985, "admission fixture", 1, 1) };
            Manifestation MakePlayer(uint id) => new Manifestation
            {
                Id = id, Level = 1, EntityClass = Items[0].Class, MapChannel = map,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>(),
                Name = "Arrival", FamilyName = "Fixture", Rotation = 1.25f
            };
            _client.State = sessionState;
            _client.Player = MakePlayer(101);
            _client.Player.State = actorState;
            _client.Player.Inventory.EquippedInventory.AddRange(new ulong[22]);
            var nearby = new Client(_factory, new ClientPacketHandler()) { State = ClientState.Ingame, Player = MakePlayer(102) };
            nearby.Player.Inventory.EquippedInventory.AddRange(new ulong[22]);
            var cells = CellManager.Instance.CreateCellMatrix(map, (uint)CellManager.CellBias, (uint)CellManager.CellBias);
            map.MapCellInfo.Cells[cells[2, 2]].ClientList.Add(nearby);

            var singleton = typeof(ManifestationManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = singleton.GetValue(null);
            singleton.SetValue(null, new ManifestationManager(_factory, new WeaponAttackManager(_factory, () => 0)));
            try
            {
                CellManager.Instance.AddToWorld(_client);
                var packets = Drain();
                var owner = packets.OfType<CreatePhysicalEntityPacket>().Single(p => p.EntityId == _client.Player.EntityId);
                var peer = packets.OfType<CreatePhysicalEntityPacket>().Single(p => p.EntityId == nearby.Player.EntityId);
                var controller = packets.OfType<SetControlledActorIdPacket>().Single();
                var actor = owner.EntityData.OfType<ActorInfoPacket>().Single();
                var missions = owner.EntityData.OfType<MissionStatusInfoPacket>().Single();
                var location = owner.EntityData.OfType<WorldLocationDescriptorPacket>().Single();
                Assert.IsFalse(owner.EntityData.OfType<EquipmentInfoPacket>().Any());
                Assert.AreEqual(actorState == CharacterState.Dead, owner.EntityData.OfType<DeadOnArrivalPacket>().Any());
                Assert.AreEqual(1, peer.EntityData.OfType<EquipmentInfoPacket>().Count());
                Assert.IsTrue(packets.FindIndex(p => p is CharacterOptionsPacket) < packets.IndexOf(controller));
                Assert.IsTrue(packets.IndexOf(controller) < packets.IndexOf(owner), "Selecting an absent actor defers controller creation until its AddToWorld event.");
                Assert.IsTrue(owner.EntityData.IndexOf(actor) < owner.EntityData.IndexOf(location));
                Assert.IsTrue(owner.EntityData.IndexOf(missions) < owner.EntityData.IndexOf(location));
                Assert.AreSame(location, owner.EntityData.Last(), "Location triggers the original controller callback after yaw and missions are ready.");
                Assert.IsTrue(packets.IndexOf(owner) < packets.IndexOf(peer));
                Assert.IsTrue(packets.IndexOf(owner) < packets.FindIndex(p => p is WeaponDrawerSlotPacket));
                Assert.IsTrue(packets.IndexOf(owner) < packets.IndexOf(packets.OfType<EquipmentInfoPacket>().Single()));
            }
            finally
            {
                singleton.SetValue(null, previous);
            }
        }

        [DataTestMethod]
        [DataRow(6.02139, CharacterState.Normal)]
        [DataRow(Math.PI, CharacterState.Normal)]
        [DataRow(1.25, CharacterState.Dead)]
        public void AdmissionInitializesActorYawBeforeCreatingItsController(double yaw, CharacterState state)
        {
            _client.State = ClientState.Ingame;
            _client.Player = new Manifestation
            {
                Id = 101, Level = 1, Rotation = (float)yaw, State = state,
                EntityClass = Items[0].Class, AppearanceData = new Dictionary<EquipmentData, AppearanceData>(),
                MapChannel = new MapChannel { MapInfo = new MapInfo(1985, "admission fixture", 1, 1) }
            };
            _client.Player.Inventory.EquippedInventory.AddRange(new ulong[22]);
            _client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100,
                state == CharacterState.Dead ? 0 : 100, 0, 0);

            // AssignPlayer also sends social state. Keep that unrelated work on this
            // fixture's isolated database, restoring the singleton even on failure.
            var singleton = typeof(SocialManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = singleton.GetValue(null);
            singleton.SetValue(null, Activator.CreateInstance(typeof(SocialManager),
                BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { _factory }, null));
            try
            {
                var manager = new ManifestationManager(_factory, new WeaponAttackManager(_factory, () => 0));
                manager.InitializePlayerControl(_client);
                manager.AssignPlayer(_client);
                var packets = Drain();
                var owner = packets.OfType<CreatePhysicalEntityPacket>().Single();
                var actor = owner.EntityData.OfType<ActorInfoPacket>().Single();
                var controller = packets.OfType<SetControlledActorIdPacket>().Single();
                Assert.IsTrue(packets.IndexOf(controller) < packets.IndexOf(owner));
                Assert.IsTrue(owner.EntityData.IndexOf(actor) < owner.EntityData.FindIndex(p => p is WorldLocationDescriptorPacket),
                    "The original deferred controller initializes on AddToWorld, after ActorInfo has set yaw.");
                Assert.AreEqual((double)(float)yaw, actor.Yaw);
                CollectionAssert.AreEqual(new[] { state }, actor.StateIds.ToArray());
                Assert.AreEqual(_client.Player.EntityId, controller.EntityId);
                Assert.IsTrue(packets.IndexOf(owner) < packets.FindIndex(p => p is WeaponDrawerSlotPacket));
                Assert.IsTrue(packets.IndexOf(owner) < packets.FindIndex(p => p is EquipmentInfoPacket));
            }
            finally
            {
                singleton.SetValue(null, previous);
            }
        }
    }
}
