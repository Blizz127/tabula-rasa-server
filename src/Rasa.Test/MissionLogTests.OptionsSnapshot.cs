using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Packets.MapChannel.Client;
using Rasa.Structures;

namespace Rasa.Test
{
    public partial class MissionLogTests
    {
        private void SeedCharacterOptionSnapshot()
        {
            _client.Player.CharacterOptions.Clear();
            _client.Player.CharacterOptions.Add(new CharacterOptions(CharacterOption.MissionTrack0, "1,992"));
            _client.Player.CharacterOptions.Add(new CharacterOptions(CharacterOption.MaximizeInventory, "1"));
            _client.Player.CharacterOptions.Add(new CharacterOptions(CharacterOption.Tutorial0, "37"));
            using var unit = _factory.CreateChar();
            foreach (var option in _client.Player.CharacterOptions)
                unit.CharacterOptions.AddOrUpdate(CharacterId, (uint)option.OptionId, option.Value);
            unit.CharacterOptions.AddOrUpdate(CharacterId + 1, (uint)CharacterOption.MissionTrack0, "9,999");
            unit.Complete();
        }

        private static SaveCharacterOptionsPacket DecodeOptionSnapshot(params CharacterOptions[] options)
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            writer.WriteTuple(1);
            writer.WriteList(options.Length);
            foreach (var option in options) writer.WriteStruct(option);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            var packet = new SaveCharacterOptionsPacket();
            packet.Read(reader);
            Assert.AreEqual(stream.Length, stream.Position);
            return packet;
        }

        [TestMethod]
        public void EmptyOriginalOptionSnapshotClearsDefaultsAndKeepsOtherCharacters()
        {
            SeedCharacterOptionSnapshot();
            new ManifestationManager(_factory, new WeaponAttackManager(_factory, () => 0)).SaveCharacterOptions(_client, DecodeOptionSnapshot());
            Assert.AreEqual(0, _client.Player.CharacterOptions.Count);
            using var unit = _factory.CreateChar();
            Assert.AreEqual(0, unit.CharacterOptions.Get(CharacterId).Count);
            Assert.AreEqual("9,999", unit.CharacterOptions.Get(CharacterId + 1).Single().Value);
        }

        [TestMethod]
        public void NonemptyOriginalSnapshotRemovesOmittedTrackingAndOtherDefaultOptions()
        {
            SeedCharacterOptionSnapshot();
            new ManifestationManager(_factory, new WeaponAttackManager(_factory, () => 0)).SaveCharacterOptions(_client,
                DecodeOptionSnapshot(new CharacterOptions(CharacterOption.Tutorial0, "37")));
            Assert.AreEqual(CharacterOption.Tutorial0, _client.Player.CharacterOptions.Single().OptionId);
            Assert.AreEqual(0, MissionTrackingRules.Tracked(_client.Player.CharacterOptions).Count);
            using var unit = _factory.CreateChar();
            Assert.AreEqual((uint)CharacterOption.Tutorial0, unit.CharacterOptions.Get(CharacterId).Single().OptionId);
            Assert.AreEqual("9,999", unit.CharacterOptions.Get(CharacterId + 1).Single().Value);
        }

        [TestMethod]
        public void OptionSnapshotRetainsRawGroupedTextAndPublishesAnIndependentCache()
        {
            SeedCharacterOptionSnapshot();
            var packet = DecodeOptionSnapshot(new CharacterOptions(CharacterOption.MissionTrack0, "1,992"),
                new CharacterOptions(CharacterOption.Tutorial0, " 37 "));
            new ManifestationManager(_factory, new WeaponAttackManager(_factory, () => 0)).SaveCharacterOptions(_client, packet);
            CollectionAssert.AreEquivalent(new[] { "1,992", " 37 " }, _client.Player.CharacterOptions.Select(o => o.Value).ToArray());
            packet.OptionsList[0].Value = "changed after save";
            Assert.AreEqual("1,992", _client.Player.CharacterOptions.Single(o => o.OptionId == CharacterOption.MissionTrack0).Value);
            using var unit = _factory.CreateChar();
            CollectionAssert.AreEquivalent(new[] { "1,992", " 37 " }, unit.CharacterOptions.Get(CharacterId).Select(o => o.Value).ToArray());
        }

        [TestMethod]
        public void OptionSnapshotSaveFailureRollsBackRowsAndDoesNotPublishCache()
        {
            SeedCharacterOptionSnapshot();
            var originalCache = _client.Player.CharacterOptions;
            using (var context = WeaponReloadPersistenceTests.Context(_connection))
                context.Database.ExecuteSqlRaw("CREATE TRIGGER option_snapshot_failure BEFORE INSERT ON character_option " +
                    "WHEN NEW.option_id = 56 BEGIN SELECT RAISE(ABORT, 'injected option write failure'); END;");
            var packet = DecodeOptionSnapshot(new CharacterOptions(CharacterOption.MaximizeInventory, "changed"),
                new CharacterOptions(CharacterOption.MissionTrack1, "1,993"));
            Assert.ThrowsException<DbUpdateException>(() => new ManifestationManager(_factory, new WeaponAttackManager(_factory, () => 0)).SaveCharacterOptions(_client, packet));
            Assert.AreSame(originalCache, _client.Player.CharacterOptions);
            CollectionAssert.AreEquivalent(new[] { "1,992", "1", "37" }, originalCache.Select(o => o.Value).ToArray());
            using var unit = _factory.CreateChar();
            CollectionAssert.AreEquivalent(new[] { "1,992", "1", "37" }, unit.CharacterOptions.Get(CharacterId).Select(o => o.Value).ToArray());
            Assert.AreEqual("9,999", unit.CharacterOptions.Get(CharacterId + 1).Single().Value);
        }
    }
}
