using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Structures;

namespace Rasa.Test
{
    public partial class WeaponAttackLifecycleTests
    {
        [TestMethod]
        public void PracticeDummyIsTheWindupTargetForServerAutofire()
        {
            var dummy = new DynamicObject
            {
                MapChannel = _map, MapContextId = 1220,
                DynamicObjectType = DynamicObjectType.ContentUsable, HitPoints = 100
            };
            EntityManager.Instance.RegisterEntity(dummy.EntityId, EntityType.Object);
            EntityManager.Instance.RegisterDynamicObject(dummy);
            _map.DynamicObjects.Add(dummy);
            try
            {
                Assert.IsTrue(_attacks.TryStart(_client, new RequestWeaponAttackPacket
                {
                    ActionId = ActionId.WeaponAttack, ActionArgId = 203, TargetId = dummy.EntityId
                }, false));
                var windup = Drain().OfType<PerformWindupPacket>().Single();
                Assert.AreEqual(dummy.EntityId, windup.Arg,
                    "The original TargetedAction windup needs the object target to aim the visible attack.");
                Assert.AreSame(dummy, _client.Player.CurrentWeaponAttack.ContentTarget);
                Assert.AreEqual(5u, _weapon.CurrentAmmo, "Windup must not spend ammunition before resolution.");
            }
            finally
            {
                _client.Player.CurrentWeaponAttack = null;
                _map.DynamicObjects.Remove(dummy);
                EntityManager.Instance.UnregisterDynamicObject(dummy.EntityId);
                EntityManager.Instance.UnregisterEntity(dummy.EntityId);
            }
        }
    }
}
