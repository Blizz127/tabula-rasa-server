using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.World;

namespace Rasa.Test
{
    public partial class MissionLogTests
    {
        [DataTestMethod]
        [DataRow(6U)]
        [DataRow(10U)]
        public void CollectionTurnInConsumesItemsOnlyWhenRewardAndMissionCommit(uint stackSize)
        {
            const uint templateId = 900509;
            const uint itemClassId = 900609;
            var template = RegisterTemplate(templateId, (EntityClasses)itemClassId);
            var definition = _missions.LoadedMissions[MissionId];
            definition.Counters[5] = new() { new NpcMissionObjectiveCounterEntry
            {
                MissionId = MissionId, ObjectiveId = 5, CounterId = 0, InitialValue = 0, TargetValue = 6
            } };
            definition.ItemCounterClasses[(5, 0)] = itemClassId;
            _client.Player.Inventory.PersonalInventory.AddRange(new ulong[250]);
            typeof(Rasa.Game.Client).GetProperty("AccountEntry")?.SetValue(_client,
                new GameAccountEntry { Id = AccountId });

            Accept();
            CompleteObjective(_scout, 5);
            CompleteObjective(_giver, 4);
            Assert.IsTrue(_client.Player.Missions[MissionId].IsCompleteable(definition));

            var hearts = new Item
            {
                ItemTemplate = template, ItemTemplateId = templateId, OwnerId = CharacterId,
                OwnerSlotId = 200, StackSize = stackSize, CurrentHitPoints = 1, Crafter = ""
            };
            using (var context = WeaponReloadPersistenceTests.Context(_connection))
            {
                var row = new ItemEntry(hearts);
                context.ItemEntries.Add(row);
                context.SaveChanges();
                hearts.Id = row.ItemId;
                context.CharacterInventoryEntries.Add(new CharacterInventoryEntry(AccountId, CharacterId,
                    (uint)InventoryType.Personal, 200, hearts.Id));
                context.SaveChanges();
            }
            EntityManager.Instance.RegisterEntity(hearts.EntityId, EntityType.Item);
            EntityManager.Instance.RegisterItem(hearts.EntityId, hearts);
            _client.Player.Inventory.PersonalInventory[200] = hearts.EntityId;
            Drain();

            _factory.FailNextComplete = true;
            CompleteMission();
            Assert.AreEqual(MissionState.Active, _client.Player.Missions[MissionId].State);
            Assert.AreEqual(stackSize, hearts.StackSize);
            using (var context = WeaponReloadPersistenceTests.Context(_connection))
            {
                Assert.AreEqual(stackSize, context.ItemEntries.Single(item => item.ItemId == hearts.Id).StackSize);
                Assert.AreEqual(1, context.CharacterInventoryEntries.Count(entry => entry.ItemId == hearts.Id));
            }

            CompleteMission();
            Assert.AreEqual(MissionState.Completed, _client.Player.Missions[MissionId].State);
            Assert.AreEqual(stackSize - 6, hearts.StackSize);
            Assert.AreEqual(stackSize == 6 ? 0UL : hearts.EntityId, _client.Player.Inventory.PersonalInventory[200]);
            using (var context = WeaponReloadPersistenceTests.Context(_connection))
            {
                Assert.AreEqual(stackSize - 6, context.ItemEntries.Single(item => item.ItemId == hearts.Id).StackSize);
                Assert.AreEqual(stackSize == 6 ? 0 : 1, context.CharacterInventoryEntries.Count(entry => entry.ItemId == hearts.Id));
                Assert.AreEqual(290, context.CharacterEntries.Single(entry => entry.Id == CharacterId).Credit);
            }
            if (stackSize > 6)
            {
                EntityManager.Instance.UnregisterItem(hearts.EntityId);
                EntityManager.Instance.UnregisterEntity(hearts.EntityId);
            }
        }
    }
}
