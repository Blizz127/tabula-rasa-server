using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Structures;

namespace Rasa.Test
{
    /// <summary>
    /// Regression: the world data's mission tables name NPCs that do not stand on an NPC-augmented entity class.
    /// Creature 132 "AFS Quartermaster Caufield" is spawned by spawnpool 210 in Alia Das on the plain Redshirt class
    /// 29423 (client entityclass augmentation list [1]) and receives the seeded class-gear missions 2010/2011. The NPC
    /// load used to dereference a null <see cref="Creature.Npc"/> there and abort server startup, and bound
    /// <c>npc_package</c> rows only when the class carried the NPC augmentation, which silently dropped Caufield's
    /// dialogue package 133.
    /// </summary>
    [TestClass]
    public class CreatureNpcBindingTests
    {
        private static Creature Redshirt() => new Creature
        {
            DbId = 132, EntityClass = (EntityClasses)29423, Level = 10, MapContextId = 1220
        };

        [TestMethod]
        public void AMissionNamingACreatureWithoutAnNpcClassCreatesItsNpcRecord()
        {
            var caufield = Redshirt();
            Assert.IsNull(caufield.Npc, "the Redshirt class 29423 carries no NPC augmentation");

            CreatureManager.AddNpcMissionId(caufield, 2010);
            CreatureManager.AddNpcMissionId(caufield, 2011);

            Assert.IsNotNull(caufield.Npc);
            CollectionAssert.AreEqual(new uint[] { 2010, 2011 }, caufield.Npc.NpcMissionIds);
            Assert.AreEqual(0u, caufield.Npc.NpcPackageId, "the mission ids alone do not invent a dialogue package");
        }

        [TestMethod]
        public void AnNpcPackageRowBindsToACreatureWhoseClassHasNoNpcAugmentation()
        {
            var caufield = Redshirt();

            CreatureManager.BindNpcPackage(caufield, 133);

            Assert.AreEqual(133u, caufield.Npc.NpcPackageId);
        }

        [TestMethod]
        public void AnExistingNpcRecordKeepsItsVendorAndPackageWhenBothBindingsRun()
        {
            // Rogers (creature 100, class 3846) already carries an NPC record from the class augmentation.
            var rogers = new Creature { DbId = 100, EntityClass = (EntityClasses)3846, Level = 20, MapContextId = 1220 };
            rogers.Npc = new Npc { NpcPackageId = 116, Vendor = new Vendor(7) };

            CreatureManager.AddNpcMissionId(rogers, 1995);
            CreatureManager.BindNpcPackage(rogers, 116);

            Assert.AreEqual(116u, rogers.Npc.NpcPackageId);
            Assert.AreEqual(7u, rogers.Npc.Vendor.VendorPackageId);
            CollectionAssert.AreEqual(new uint[] { 1995 }, rogers.Npc.NpcMissionIds);
        }
    }
}
