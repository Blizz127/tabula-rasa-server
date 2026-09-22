using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
using Rasa.Services.Preloader;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Test
{
    /// <summary>
    /// The state a container announces has to be one its own augmentation owns. The client builds
    /// a usable's state machine out of usabledata.usableaugmentationstate for the augmentation its
    /// entity class carries, and the two lockbox augmentations own one state each:
    /// [66 LOCKBOX] = [3] USE_LOCKBOX_STATE_0, [76 CLANLOCKBOX] = [10000010]
    /// USE_CLANLOCKBOX_STATE_0. Anything else is a KeyError in Usable._SetState, which is caught:
    /// _curStateId stays None and every later _Transition fails too, so the container never plays
    /// a state at all.
    ///
    /// Every row of the footlocker table used to be put into 181 USE_CPOINT_STATE_UNCLAIMED, which
    /// belongs to augmentation 57 CONTROLPOINT and to no lockbox. Two of the 35 rows are not
    /// personal footlockers either: class 10000063 UsableClanLockboxV01 is a clan's.
    ///
    /// Client tables: generated/client/usabledata.pyo (usableaugmentationstate, usableclassstate,
    /// usableaugmentationstatetransition) and generated/client/entityclass.pyo from
    /// Tabula Rasa 1.16.5.0 data/game.zip, decoded 2026-09-13.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class LockboxUsableStateTests
    {
        /// <summary>
        /// usabledata.usableaugmentationstate, for the two augmentations the footlocker table's
        /// classes carry. One state each, and that is the whole of each machine.
        /// </summary>
        private static readonly Dictionary<EntityClasses, int[]> ClientStatesForClass = new Dictionary<EntityClasses, int[]>
        {
            // entityclass.lookup[21030] = ('UsableLockBoxHumFootlockerV01', ..., [66])
            { EntityClasses.UsableLockBoxHumFootlockerV01, new[] { 3 } },
            // entityclass.lookup[10000063] = ('UsableClanLockboxV01', ..., [76])
            { EntityClasses.UsableClanLockboxV01, new[] { 10000010 } }
        };

        [TestMethod]
        public void EachLockboxClassAnnouncesTheOneStateItsAugmentationOwns()
        {
            Assert.AreEqual(3, (int)UseObjectState.LockboxState0);
            Assert.AreEqual(10000010, (int)UseObjectState.ClanlockboxState0);

            Assert.AreEqual(UseObjectState.LockboxState0,
                DynamicObjectManager.LockboxStateFor(EntityClasses.UsableLockBoxHumFootlockerV01));
            Assert.AreEqual(UseObjectState.ClanlockboxState0,
                DynamicObjectManager.LockboxStateFor(EntityClasses.UsableClanLockboxV01));

            // The state the server used to send. It is a control point's, and no lockbox can hold it.
            Assert.AreEqual(181, (int)UseObjectState.CpointStateUnclaimed);
            foreach (var states in ClientStatesForClass.Values)
                CollectionAssert.DoesNotContain(states, 181);
        }

        [TestMethod]
        public void TheClanLockboxIsBuiltAsOneAndTheFootlockerAsItself()
        {
            var footlocker = DynamicObjectManager.CreateFootlocker(Row(3, 21030, 1148, "Foreas Base Footlocker1"));
            Assert.AreEqual(DynamicObjectType.Lockbox, footlocker.DynamicObjectType);
            Assert.AreEqual(UseObjectState.LockboxState0, footlocker.StateId);

            var clanLockbox = DynamicObjectManager.CreateFootlocker(Row(34, 10000063, 1454, "Clan Lockbox Paludos"));
            Assert.AreEqual(DynamicObjectType.ClanLockbox, clanLockbox.DynamicObjectType);
            Assert.AreEqual(UseObjectState.ClanlockboxState0, clanLockbox.StateId);

            // The rest of the row is carried across unchanged either way.
            Assert.AreEqual(1454u, clanLockbox.MapContextId);
            Assert.AreEqual(EntityClasses.UsableClanLockboxV01, clanLockbox.EntityClassId);
            Assert.AreEqual("Clan Lockbox Paludos", clanLockbox.Comment);
        }

        [TestMethod]
        public void EveryRowOfTheFootlockerTableGetsAStateItsClassCanHold()
        {
            var rows = FootlockerRows();
            Assert.AreEqual(35, rows.Count);

            foreach (var row in rows)
            {
                var entityClassId = (EntityClasses)row.ClassId;
                Assert.IsTrue(ClientStatesForClass.ContainsKey(entityClassId),
                    $"footlocker {row.Id} ({row.Comment}) is class {row.ClassId}, whose augmentation this test has not read from the client");

                var announced = (int)DynamicObjectManager.CreateFootlocker(row).StateId;
                CollectionAssert.Contains(ClientStatesForClass[entityClassId], announced,
                    $"footlocker {row.Id} ({row.Comment}) would announce {announced}, which class {row.ClassId} cannot hold");
            }

            // The two clan lockboxes, and only those two, are driven as clan lockboxes.
            var clan = rows.Where(row => row.ClassId == 10000063).Select(row => row.Id).ToArray();
            CollectionAssert.AreEqual(new uint[] { 34, 35 }, clan);
            Assert.IsTrue(rows.Where(row => clan.Contains(row.Id))
                .All(row => DynamicObjectManager.CreateFootlocker(row).DynamicObjectType == DynamicObjectType.ClanLockbox));
            Assert.IsTrue(rows.Where(row => !clan.Contains(row.Id))
                .All(row => DynamicObjectManager.CreateFootlocker(row).DynamicObjectType == DynamicObjectType.Lockbox));
        }

        [TestMethod]
        public void UsingAClanLockboxIsAnsweredWithItsOwnState()
        {
            var map = new MapChannel { MapInfo = new MapInfo(1454, "test", 1, 1), ClientList = new List<Client>() };
            var client = new Client(null, new ClientPacketHandler()) { State = ClientState.Ingame };
            client.Player = new Manifestation { EntityId = 0x5eed, MapChannel = map, Position = new Vector3(0f, 0f, 0f) };

            var clanLockbox = DynamicObjectManager.CreateFootlocker(Row(34, 10000063, 1454, "Clan Lockbox Paludos"));
            clanLockbox.MapChannel = map;
            map.FootLockers.Add(34, clanLockbox);
            EntityManager.Instance.RegisterDynamicObject(clanLockbox);

            try
            {
                DynamicObjectManager.Instance.RequestUseObjectPacket(client,
                    new RequestUseObjectPacket { ActionId = ActionId.UseObject, ActionArgId = 1, EntityId = clanLockbox.EntityId });

                // Not the "unsupported object type" default: the use is answered, and the state it
                // carries is the clan lockbox's own.
                var use = Drain(client).OfType<UsePacket>().Single();
                Assert.AreEqual(UseObjectState.ClanlockboxState0, use.CurState);
                Assert.AreEqual(1, map.PerformRecovery.Count);
                CollectionAssert.Contains(clanLockbox.TriggeredByPlayers, client);
            }
            finally
            {
                EntityManager.Instance.UnregisterDynamicObject(clanLockbox.EntityId);
            }
        }

        private static FootlockerEntry Row(uint id, uint classId, uint mapContextId, string comment) =>
            new FootlockerEntry { Id = id, ClassId = classId, MapContextId = mapContextId, Comment = comment };

        /// <summary>The preloaded footlocker table, read off the preloader that seeds it.</summary>
        private static List<FootlockerEntry> FootlockerRows()
        {
            var getRows = typeof(FootlockerPreloader).GetMethod("GetRows", BindingFlags.Instance | BindingFlags.NonPublic);
            var rows = (IEnumerable<object[]>)getRows.Invoke(new FootlockerPreloader(), null);

            return rows.Select(row => new FootlockerEntry
            {
                Id = (uint)(int)row[0],
                ClassId = (uint)(int)row[1],
                MapContextId = (uint)(int)row[2],
                PosX = (double)row[3],
                PosY = (double)row[4],
                PosZ = (double)row[5],
                Rotation = (double)row[6],
                Comment = (string)row[7]
            }).ToList();
        }

        private static List<PythonPacket> Drain(Client client)
        {
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(client);
            var packets = new List<PythonPacket>();
            while (queue.PopOutgoing() is ProtocolPacket protocol)
                if (protocol.Message is CallMethodMessage call)
                    packets.Add(call.Packet);
            return packets;
        }
    }
}
