using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Packets.LootDispenser.Client;
using Rasa.Packets.MapChannel.Client;
using Rasa.Structures;

namespace Rasa.Test
{
    /// <summary>
    /// A client can name an entity that is already gone (a despawned corpse, a removed object);
    /// such requests are ignored instead of throwing out of the packet handler.
    /// </summary>
    [TestClass]
    public class RobustLookupTests
    {
        private const ulong MissingEntityId = 0xFFFF_FFFF_FFFF_FF01;

        private static void Invoke(object manager, string method, params object[] args)
            => manager.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, args);

        [TestMethod]
        public void UseRequestForAnUnknownObjectIsIgnored()
        {
            Assert.IsFalse(EntityManager.Instance.DynamicObjects.ContainsKey(MissingEntityId));
            var client = new Client(null, new ClientPacketHandler());

            var oldLogger = Logger.Config;
            if (oldLogger == null) Logger.UpdateConfig(new Logger.LoggerConfig { LogToFile = false });
            try
            {
                Invoke(DynamicObjectManager.Instance, "RequestUseObjectPacket", client, new RequestUseObjectPacket { EntityId = MissingEntityId });
            }
            finally
            {
                if (oldLogger == null) typeof(Logger).GetProperty(nameof(Logger.Config)).SetValue(null, null);
            }
        }

        [TestMethod]
        public void LootAllRequestForAnUnknownCorpseIsIgnored()
        {
            var client = new Client(null, new ClientPacketHandler()) { Player = new Manifestation { MapChannel = new MapChannel() } };

            Invoke(LootDispenserManager.Instance, "RequestLootAllFromCorpse", client, new RequestLootAllFromCorpsePacket { EntityId = MissingEntityId, AutoLootOnly = false });
        }

        [TestMethod]
        public void ContextLookupHasANonThrowingForm()
        {
            var maps = new MapChannelManager(null);
            var map = new MapChannel();
            maps.MapChannelArray.Add(1220, map);

            Assert.IsTrue(maps.TryFindByContextId(1220, out var found));
            Assert.AreSame(map, found);
            Assert.IsFalse(maps.TryFindByContextId(1985, out var missing));
            Assert.IsNull(missing);
            Assert.ThrowsException<KeyNotFoundException>(() => maps.FindByContextId(1985));
        }
    }
}
