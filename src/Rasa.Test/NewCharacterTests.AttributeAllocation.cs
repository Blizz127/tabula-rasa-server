using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Structures;
using Rasa.Structures.Char;

namespace Rasa.Test
{
    public partial class NewCharacterTests
    {
        [DataTestMethod]
        [DataRow(1, 1, 1, 0)]
        [DataRow(1, 0, 0, 2)]
        public void AcceptedAttributeAllocationRefreshesOriginalWindowFromCommittedBudget(
            int body, int mind, int spirit, int remaining)
        {
            using (var context = Context())
            {
                context.CharacterEntries.Add(new CharacterEntry
                    { Id = 101, AccountId = 10, Slot = 1, Name = "Allocation", Level = 2 });
                context.SaveChanges();
            }
            _client.State = ClientState.Ingame;
            var player = _client.Player = new Manifestation { Id = 101, Level = 2, Race = Race.Human };
            player.Inventory.EquippedInventory.AddRange(new ulong[22]);
            foreach (Attributes attribute in Enum.GetValues(typeof(Attributes)))
                player.Attributes[attribute] = new ActorAttributes(attribute, 0, 0, 0, 0, 0);
            var manager = new ManifestationManager(_factory, new WeaponAttackManager(_factory, () => 0));
            manager.UpdateStatsValues(_client, true);
            var singleton = typeof(CharacterManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = singleton.GetValue(null);
            singleton.SetValue(null, _manager);
            try
            {
                manager.AllocateAttributePoints(_client,
                    new AllocateAttributePointsPacket { Body = body, Mind = mind, Spirit = spirit });
                var packets = Drain();
                Assert.AreEqual(2, packets.Count);
                Assert.IsInstanceOfType(packets[0], typeof(AvailableAllocationPointsPacket));
                Assert.IsInstanceOfType(packets[1], typeof(AttributeInfoPacket));

                // Reproduce the relevant original consumer contract: receiving the budget
                // updates avatar.attributePoints, but the open attributes window reloads its
                // displayed counter only when AttributeInfo announces a B/M/S change.
                var avatarPoints = 3;
                var displayedPoints = remaining; // The local preview before Accept.
                foreach (var packet in packets)
                {
                    using var stream = new MemoryStream();
                    using var writer = new PythonWriter(new BinaryWriter(stream));
                    packet.Write(writer);
                    stream.Position = 0;
                    using var reader = new PythonReader(new BinaryReader(stream));
                    if (packet is AvailableAllocationPointsPacket)
                    {
                        Assert.AreEqual(3, reader.ReadTuple());
                        avatarPoints = reader.ReadInt();
                        Assert.AreEqual(0, reader.ReadInt());
                        reader.ReadInt(); // Skills have their separate budget.
                    }
                    else
                    {
                        Assert.AreEqual(1, reader.ReadTuple());
                        var count = reader.ReadDictionary();
                        for (var index = 0; index < count; index++)
                        {
                            var attribute = (Attributes)reader.ReadInt();
                            Assert.AreEqual(5, reader.ReadTuple());
                            var normalMaximum = reader.ReadInt();
                            var currentMaximum = reader.ReadInt();
                            reader.ReadInt();
                            reader.ReadInt();
                            reader.ReadInt();
                            if (attribute == Attributes.Body || attribute == Attributes.Mind || attribute == Attributes.Spirit)
                            {
                                displayedPoints = avatarPoints;
                                var spent = attribute == Attributes.Body ? body : attribute == Attributes.Mind ? mind : spirit;
                                Assert.AreEqual(12 + spent, normalMaximum);
                                Assert.AreEqual(normalMaximum, currentMaximum);
                            }
                        }
                    }
                    Assert.AreEqual(stream.Length, stream.Position);
                }
                Assert.AreEqual(remaining, avatarPoints);
                Assert.AreEqual(remaining, displayedPoints, "Accept must not restore the old spendable-point display.");
                Assert.AreEqual(remaining, AttributePointAllocation.GetAvailablePoints(player));
                using (var context = Context())
                {
                    var stored = context.CharacterEntries.Single(entry => entry.Id == 101);
                    Assert.AreEqual(body, stored.Body);
                    Assert.AreEqual(mind, stored.Mind);
                    Assert.AreEqual(spirit, stored.Spirit);
                }

                if (remaining == 0)
                {
                    manager.AllocateAttributePoints(_client,
                        new AllocateAttributePointsPacket { Body = body, Mind = mind, Spirit = spirit });
                    var rejected = Drain();
                    Assert.AreEqual(1, rejected.Count);
                    Assert.AreEqual(0, ((AvailableAllocationPointsPacket)rejected.Single()).AvailableAttributePoints);
                    Assert.AreEqual(body, player.SpentBody);
                    Assert.AreEqual(mind, player.SpentMind);
                    Assert.AreEqual(spirit, player.SpentSpirit);
                    using var context = Context();
                    var stored = context.CharacterEntries.Single(entry => entry.Id == 101);
                    Assert.AreEqual(body, stored.Body);
                    Assert.AreEqual(mind, stored.Mind);
                    Assert.AreEqual(spirit, stored.Spirit);
                }
            }
            finally
            {
                singleton.SetValue(null, previous);
            }
        }
    }
}
