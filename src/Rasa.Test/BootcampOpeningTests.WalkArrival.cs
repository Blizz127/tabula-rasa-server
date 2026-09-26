using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Navigation;
using Rasa.Packets;
using Rasa.Packets.Game.Server;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Protocol;
using Rasa.Structures;

namespace Rasa.Test
{
    public partial class BootcampOpeningTests
    {
        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void SeededMcAllisterStopsAtArrivalAndKeepsHisFacingForLateObservers(bool checkLateObserver)
        {
            var root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "navmesh", "adv_bootcamp.nav")))
                root = root.Parent;
            Assert.IsNotNull(root, "Mount the original-map-derived adv_bootcamp.nav under the test checkout; this regression must not skip its real route.");
            _instance = new MapChannel
            {
                MapInfo = new MapInfo(Camp, "adv_bootcamp", MapVersion, 4),
                OwnerCharacterId = CharacterId, InstanceId = 7, ClientList = new List<Client>(),
                NavMesh = new NavMeshQuery(NavMeshFile.Read(Path.Combine(root.FullName, "navmesh", "adv_bootcamp.nav")))
            };
            _mcallister = PlaceMcAllister(_instance);
            // This fixture creates the seeded actor directly rather than through the
            // appearance loader; introduction still requires a valid empty equipment map.
            _mcallister.AppearanceData = new Dictionary<EquipmentData, AppearanceData>();
            CreatureManager.ApplyPlacementBehavior(_mcallister, _content.Content.Catalog.Placements[McAllisterPlacement]);
            var destinationRow = _content.Content.Catalog.Locations[McAllisterDestination];
            var destination = new Vector3((float)destinationRow.PosX, (float)destinationRow.PosY, (float)destinationRow.PosZ);
            Assert.AreEqual(2.5f, _mcallister.WalkSpeed);
            Assert.AreEqual(0f, _mcallister.RunSpeed);
            Assert.IsNotNull(_instance.NavMesh.FindPath(_mcallister.Position, destination, out var complete));
            Assert.IsTrue(complete, "This regression must reproduce the complete route, not a partial-path fallback.");

            var classes = EntityClassManager.Instance.LoadedEntityClasses;
            var classId = _mcallister.EntityClass;
            classes.TryGetValue(classId, out var previousClass);
            classes[classId] = new EntityClass((uint)classId, "McAllister test class metadata", 0, 0, new List<AugmentationType>(), true);
            _client = new Client(_factory, new ClientPacketHandler()) { State = ClientState.Ingame };
            _client.Player = new Manifestation { MapChannel = _instance, MapContextId = Camp };
            var movements = new List<MoveObjectMessage>();
            try
            {
                // Use the real migrated creature, placement, destination and navmesh. Mission
                // acceptance scheduling is independently covered by the opening progression test.
                Assert.IsTrue(BehaviorManager.Instance.WalkTo(_mcallister, destination));
                var ticks = 0;
                while (_mcallister.Controller.CurrentAction == BehaviorManager.BehaviorActionFollowingPath && ticks++ < 2400)
                {
                    ObserveCurrentCell();
                    BehaviorManager.Instance.MapChannelThink(_instance, 250);
                    movements.AddRange(DrainWalkMovements(_client));
                }
                Assert.IsTrue(ticks < 2400, "Seeded walk never reached its terminal state.");
                Assert.IsTrue(movements.Any(message => message.Movement.Velocity > 0));
                var moving = movements.Last(message => message.Movement.Velocity > 0).Movement;
                var final = movements.Last().Movement;
                var stoppedPosition = _mcallister.Position;
                Console.WriteLine($"McAllister arrival after {ticks} ticks: position={stoppedPosition}, last velocity={final.Velocity}, last yaw={final.ViewDirection.X}, stored yaw={_mcallister.Rotation}, packets={movements.Count}");
                Assert.IsTrue(Vector2.Distance(new Vector2(stoppedPosition.X, stoppedPosition.Z), new Vector2(destination.X, destination.Z)) <= 0.8f);
                Assert.AreEqual(_instance.NavMesh.GroundHeight(stoppedPosition).Value, stoppedPosition.Y, 0.001f,
                    "This checks existing navmesh grounding, not fidelity of that height to rendered original geometry.");

                for (var tick = 0; tick < 240; tick++)
                {
                    ObserveCurrentCell();
                    BehaviorManager.Instance.MapChannelThink(_instance, 250);
                }
                Assert.AreEqual(stoppedPosition, _mcallister.Position, "Arrival must remain stationary for 60 seconds.");
                Assert.IsFalse(DrainWalkMovements(_client).Any(message => message.Movement.Velocity > 0));

                if (checkLateObserver)
                {
                    var late = new Client(_factory, new ClientPacketHandler()) { State = ClientState.Ingame };
                    late.Player = new Manifestation { MapChannel = _instance, MapContextId = Camp };
                    CreatureManager.Instance.CreateCreatureOnClient(late, _mcallister);
                    var introduction = Drain(late).OfType<CreatePhysicalEntityPacket>().Single(packet => packet.EntityId == _mcallister.EntityId);
                    var actor = introduction.EntityData.OfType<ActorInfoPacket>().Single();
                    Assert.AreEqual((double)moving.ViewDirection.X, actor.Yaw, 0.0001,
                        "A late observer must see the final walking facing, not the old spawn heading.");
                }
                else
                {
                    Assert.AreEqual(0f, final.Velocity, "Ending a route must cancel the previously advertised moving velocity.");
                    Assert.AreEqual(1, movements.Count(message => message.Movement.Velocity == 0),
                        "Intermediate navmesh corners must retain continuous movement.");
                    Assert.AreEqual(stoppedPosition, final.Position);
                    Assert.AreEqual(moving.ViewDirection.X, final.ViewDirection.X);
                }

                // A repeated command to the current point must remain finite and stationary.
                Assert.IsTrue(BehaviorManager.Instance.WalkTo(_mcallister, stoppedPosition));
                for (var tick = 0; tick < 4; tick++)
                    BehaviorManager.Instance.MapChannelThink(_instance, 250);
                var repeatedStop = DrainWalkMovements(_client).Last().Movement;
                Assert.AreEqual(0f, repeatedStop.Velocity);
                Assert.IsFalse(float.IsNaN(repeatedStop.ViewDirection.X));
                Assert.AreEqual(stoppedPosition, repeatedStop.Position);
            }
            finally
            {
                if (previousClass == null) classes.Remove(classId);
                else classes[classId] = previousClass;
            }
        }

        private void ObserveCurrentCell()
        {
            foreach (var cell in _instance.MapCellInfo.Cells.Values)
                cell.ClientList.Remove(_client);
            _instance.MapCellInfo.Cells[_mcallister.Cells[2, 2]].ClientList.Add(_client);
        }

        private static List<MoveObjectMessage> DrainWalkMovements(Client client)
        {
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(client);
            var messages = new List<MoveObjectMessage>();
            while (queue.PopOutgoing() is ProtocolPacket packet)
                if (packet.Message is MoveObjectMessage movement)
                    messages.Add(movement);
            return messages;
        }
    }
}
