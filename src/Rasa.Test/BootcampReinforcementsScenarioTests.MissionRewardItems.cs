using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Packets.Inventory.Server;
using Rasa.Packets.Mission.Server;
using Rasa.Structures;
using Rasa.Structures.Char;

namespace Rasa.Test
{
    /// <summary>
    /// MissionRewardItems (2026-09-26) against the migrated world: the seven missions load their reward items as one
    /// choice, in the wiki's order, through the real ItemManager, and a turn-in at the seeded receiver delivers the
    /// chosen item with its recorded stack in the same completion as the credits.
    /// </summary>
    public partial class BootcampReinforcementsScenarioTests
    {
        // Each template's world-seed class and class level requirement (itemclass.pyo itemTemplateItemClass / reqData).
        private static readonly Dictionary<uint, (EntityClasses Class, int Level)> RewardTemplateClasses = new()
        {
            [120418] = ((EntityClasses)13924, 34), [120419] = ((EntityClasses)16776, 33), [120420] = ((EntityClasses)18718, 34),
            [120421] = ((EntityClasses)19930, 35), [120101] = ((EntityClasses)14067, 39), [120102] = ((EntityClasses)18073, 35),
            [120103] = ((EntityClasses)16931, 38), [120104] = ((EntityClasses)18861, 39), [120382] = ((EntityClasses)27054, 33),
            [120383] = ((EntityClasses)27074, 31), [120384] = ((EntityClasses)27115, 30), [120385] = ((EntityClasses)27157, 33),
            [120804] = ((EntityClasses)27094, 25), [120805] = ((EntityClasses)27156, 28), [120806] = ((EntityClasses)27087, 26),
            [45059] = ((EntityClasses)22542, 30), [118897] = ((EntityClasses)28488, 30), [111037] = ((EntityClasses)26287, 26),
            [111027] = ((EntityClasses)26277, 26), [118906] = ((EntityClasses)28497, 30), [111017] = ((EntityClasses)26267, 26),
            [45062] = ((EntityClasses)22545, 35), [118898] = ((EntityClasses)28489, 35), [111048] = ((EntityClasses)26298, 31),
            [45446] = ((EntityClasses)22963, 31)
        };

        private static bool IsWeaponReward(uint template) => template is >= 120382 and <= 120385 or >= 120804 and <= 120806;
        private static bool IsEquipmentReward(uint template) => template >= 120000;

        [TestMethod]
        public void TheSevenRewardItemMissionsOfferTheirRecordedItemsAsOneChoice()
        {
            foreach (var (missionId, items, credits, experience) in ContentSchemaMigrationTests.ApprovedMissionRewardItems)
            {
                var definition = _missions.LoadedMissions[missionId];
                CollectionAssert.AreEqual(Array.Empty<string>(), definition.RewardGaps, $"mission {missionId}");
                CollectionAssert.AreEqual(Array.Empty<string>(), definition.DefinitionGaps(), $"mission {missionId}");
                Assert.AreEqual(((long)credits, 0L, (long)experience), (definition.RewardCredits, definition.RewardPrestige, definition.RewardExperience), $"mission {missionId}");
                Assert.AreEqual(0, definition.OfferedFixedItems.Count, $"mission {missionId} has no fixed item");

                var offered = definition.MissionConstantData.RewardInfo.SelectableReward;
                CollectionAssert.AreEqual(items.Select(item => (item.Template, item.Quantity)).ToArray(),
                    offered.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray(), $"mission {missionId} offer");
                foreach (var item in offered)
                {
                    var (classId, level) = RewardTemplateClasses[item.ItemTemplateId];
                    // Quality from the regenerated item_template: the UNC equipment 3, the consumables 2.
                    Assert.AreEqual((classId, IsEquipmentReward(item.ItemTemplateId) ? 3 : 2), (item.Class, item.QualityId), $"template {item.ItemTemplateId}");
                    Assert.AreEqual(0, item.ModuleIds.Count, $"template {item.ItemTemplateId}: no manufacturer module is invented");

                    var template = _rewardItems.Items.GetItemTemplateById(item.ItemTemplateId);
                    Assert.AreEqual(IsEquipmentReward(item.ItemTemplateId) ? InventoryCategory.Equipment : InventoryCategory.Consumable, template.InventoryCategory, $"template {item.ItemTemplateId}");
                    Assert.AreEqual(level, template.ItemInfo.Requirements[RequirementsType.ReqXpLevel], $"template {item.ItemTemplateId}");
                    Assert.AreEqual(IsWeaponReward(item.ItemTemplateId), template.WeaponInfo != null, $"template {item.ItemTemplateId} weapon row");

                    // A tooltip request for the offered reward writes without a missing row.
                    using var stream = new System.IO.MemoryStream();
                    using var writer = new PythonWriter(new System.IO.BinaryWriter(stream));
                    new Rasa.Packets.MapChannel.Server.ItemTemplateTooltipInfoPacket(template, EntityClassManager.Instance.GetClassInfo(template.Class)).Write(writer);
                    Assert.IsTrue(stream.Length > 0, $"template {item.ItemTemplateId} tooltip");
                }
            }
        }

        [DataTestMethod]
        [DataRow(1541u, 3, 120421u, 1u)]   // Stealth Legs v4, the entry that separates 1541's run from 120109-120112
        [DataRow(1673u, 0, 120101u, 1u)]
        [DataRow(1040u, 2, 120384u, 1u)]
        [DataRow(983u, 1, 120805u, 1u)]
        [DataRow(970u, 2, 111037u, 4u)]
        [DataRow(1068u, 1, 118906u, 2u)]
        [DataRow(1863u, 3, 45446u, 4u)]
        public void TurningInARewardItemMissionDeliversTheChosenItemWithTheCompletion(uint missionId, int selection, uint expectedTemplate, uint expectedStack)
        {
            var definition = _missions.LoadedMissions[missionId];
            var credits = ContentSchemaMigrationTests.ApprovedMissionRewardItems.Single(entry => entry.Mission == missionId).Credits;

            // A Soldier of the missions' band (the rewards require 25-39) with a full attribute set, so the experience
            // reward can level the character as it would in play. The mission is in the log with every required
            // objective done; how it got there is covered elsewhere.
            _client.Player.Class = 2;
            _client.Player.Level = 30;
            foreach (Attributes attribute in Enum.GetValues(typeof(Attributes)))
                _client.Player.Attributes[attribute] = new ActorAttributes(attribute, 100, 100, 100, 0, 0);
            using (var context = CharContext(_charConnection))
            {
                context.CharacterMissionEntries.Add(new CharacterMissionEntry(CharacterId, missionId, (uint)MissionState.Active));
                context.SaveChanges();
            }
            var mission = new PlayerMission { MissionId = missionId, State = MissionState.Active };
            foreach (var objective in definition.Objectives.Values.Where(objective => objective.IsRequired == true))
                mission.Objectives[objective.ObjectiveId] = MissionObjectiveState.Completed;
            _client.Player.Missions[missionId] = mission;

            // The seeded receiver, where its content placement stands.
            var placement = _content.Content.Catalog.Placements.Values.FirstOrDefault(candidate => candidate.CreatureId == definition.MissionReciver);
            Assert.IsNotNull(placement, $"mission {missionId}: receiver {definition.MissionReciver} has no seeded placement");
            var zone = new MapChannel { MapInfo = new MapInfo(placement.MapContextId, "zone", 1, 0), ClientList = new List<Client>() };
            _instance.ClientList.Remove(_client);
            zone.ClientList.Add(_client);
            _client.Player.MapContextId = placement.MapContextId;
            _client.Player.MapChannel = zone;
            var receiver = NpcAt(definition.MissionReciver, placement.NpcPackageId, new Vector3((float)placement.PosX, (float)placement.PosY, (float)placement.PosZ), zone);
            _client.Player.Position = receiver.Position + new Vector3(1f, 0f, 0f);

            // A choice is required and must name one of the offered items.
            Drain();
            _missions.CompleteNpcMission(_client, receiver.EntityId, missionId, null);
            _missions.CompleteNpcMission(_client, receiver.EntityId, missionId, definition.OfferedSelectableRewards.Count);
            Assert.AreEqual(MissionState.Active, _client.Player.Missions[missionId].State);
            Assert.IsFalse(Drain().OfType<MissionCompletedPacket>().Any());

            var before = _client.Player.Credits[CurencyType.Credits];
            _missions.CompleteNpcMission(_client, receiver.EntityId, missionId, selection);
            Assert.AreEqual(MissionState.Completed, _client.Player.Missions[missionId].State, $"mission {missionId} was not turned in");
            Assert.AreEqual(before + credits, _client.Player.Credits[CurencyType.Credits]);

            var packets = Drain();
            var added = packets.OfType<InventoryAddItemPacket>().Single();
            var firstSlot = IsEquipmentReward(expectedTemplate) ? (uint)InventoryOffset.Player : (uint)InventoryOffset.CategoryConsumable;
            Assert.AreEqual(firstSlot, added.SlotId, "the item goes to the first free slot of its category");
            Assert.AreEqual(1, packets.OfType<MissionCompletedPacket>().Count());
            Assert.IsTrue(packets.FindIndex(packet => packet is InventoryAddItemPacket) < packets.FindIndex(packet => packet is MissionCompletedPacket));
            var item = EntityManager.Instance.GetItem(_client.Player.Inventory.PersonalInventory[(int)firstSlot]);
            Assert.AreEqual((expectedTemplate, expectedStack), (item.ItemTemplateId, item.StackSize));

            using (var context = CharContext(_charConnection))
            {
                Assert.AreEqual((uint)MissionState.Completed, context.CharacterMissionEntries.Single(m => m.CharacterId == CharacterId && m.MissionId == missionId).MissionState);
                var row = context.CharacterInventoryEntries.Single(entry => entry.CharacterId == CharacterId);
                var saved = context.ItemEntries.Single(entry => entry.ItemId == row.ItemId);
                Assert.AreEqual((firstSlot, expectedTemplate, expectedStack), (row.SlotId, saved.ItemTemplateId, saved.StackSize));
            }

            // A replayed turn-in pays nothing more.
            _missions.CompleteNpcMission(_client, receiver.EntityId, missionId, selection);
            Assert.IsFalse(Drain().OfType<InventoryAddItemPacket>().Any());

            zone.ClientList.Remove(_client);
            _instance.ClientList.Add(_client);
        }
    }
}
