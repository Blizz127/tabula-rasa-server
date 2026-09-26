using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Migrations.WildernessData;
using Rasa.Structures;
using Rasa.Test.Reconstruction;

namespace Rasa.Test
{
    /// <summary>
    /// The class-trainer implementation against the rules re-derived from the 1.16.5.0 client in
    /// docs/evidence/class-trainer-evidence.json (the lost research/20260914-class-trainer specification's
    /// replacement): the class tree, the tier gates, the trainer range and dialogue, and the single trainer of each
    /// hub - placed only where the evidence names him, a named gap everywhere else.
    /// </summary>
    [TestClass]
    public class ClassTrainerEvidenceTests
    {
        private static JsonDocument Evidence() =>
            JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("class-trainer-evidence.json")));

        private static JsonElement Rule(JsonDocument document, string id) =>
            document.RootElement.GetProperty("rules").EnumerateArray().Single(rule => rule.GetProperty("id").GetString() == id);

        [TestMethod]
        public void TheClassTreeGatesRangeAndDialogueAreTheClientsOwn()
        {
            using var document = Evidence();

            foreach (var entry in Rule(document, "CT-TREE").GetProperty("value").GetProperty("parent").EnumerateObject())
                Assert.AreEqual(entry.Value.GetUInt32(), ClassAdvancement.ParentOf(uint.Parse(entry.Name)), $"parent of class {entry.Name}");
            Assert.AreEqual(0u, ClassAdvancement.ParentOf(1));

            foreach (var entry in Rule(document, "CT-GATES").GetProperty("value").EnumerateObject())
                Assert.AreEqual(entry.Value.GetInt32(), ClassAdvancement.GateLevel(uint.Parse(entry.Name)), $"gate of class {entry.Name}");
            for (var classId = 8u; classId <= 15; classId++)
                Assert.IsNull(ClassAdvancement.GateLevel(classId), $"class {classId} is a final tier");

            Assert.AreEqual(Rule(document, "CT-RANGE").GetProperty("value").GetSingle(), ClassAdvancement.TrainerRange);

            var dialogs = Rule(document, "CT-DIALOG").GetProperty("value");
            StringAssert.Contains(dialogs.GetProperty(ClassAdvancement.DialogCanTrain.ToString()).GetString(), "regardless of your specialty");
            StringAssert.Contains(dialogs.GetProperty(ClassAdvancement.DialogFinalTier.ToString()).GetString(), "You already know all I can teach you");
            StringAssert.Contains(dialogs.GetProperty(ClassAdvancement.DialogNotReady.ToString()).GetString(), "Come back when");
        }

        [TestMethod]
        public void EveryTrainerTheServerKnowsIsAHubTheEvidenceNames()
        {
            using var document = Evidence();
            var hubs = document.RootElement.GetProperty("hubs").EnumerateArray().ToList();
            var placed = hubs.Where(hub => hub.GetProperty("status").GetString() == "placed").Select(hub => hub.GetProperty("trainer")).ToList();

            var packages = placed.Where(t => t.GetProperty("package").ValueKind == JsonValueKind.Number).Select(t => t.GetProperty("package").GetUInt32()).ToList();
            var creaturesWithoutPackage = placed.Where(t => t.GetProperty("package").ValueKind == JsonValueKind.Null).Select(t => t.GetProperty("creature").GetUInt32()).ToList();
            CollectionAssert.AreEquivalent(packages, ClassAdvancement.TrainerNpcPackages.ToList());
            CollectionAssert.AreEquivalent(creaturesWithoutPackage, ClassAdvancement.TrainerCreatureIds.ToList());

            // Stratton stands on marker 987 "Class Trainer: Daghda's Urn", with the name id the client gives him.
            var stratton = placed.Single(t => t.GetProperty("creature").GetUInt32() == SingleClassTrainersRows.Stratton);
            Assert.AreEqual(SingleClassTrainersRows.StrattonName, stratton.GetProperty("name_id").GetUInt32());
            var marker = document.RootElement.GetProperty("trainer_markers").EnumerateArray()
                .Single(m => m.GetProperty("map_template").GetInt32() == 1378 && m.GetProperty("text_id").GetInt32() == 987);
            Assert.AreEqual(1220, marker.GetProperty("game_context").GetInt32());
            var xyz = marker.GetProperty("xyz").EnumerateArray().Select(v => v.GetDouble()).ToArray();
            Assert.AreEqual(xyz[0], SingleClassTrainersRows.StrattonX, 0.001);
            Assert.AreEqual(xyz[1], SingleClassTrainersRows.StrattonY, 0.001);
            Assert.AreEqual(xyz[2], SingleClassTrainersRows.StrattonZ, 0.001);

            // Every hub marker of the Concordia overworld is either a placed trainer or a named gap.
            var concordia = document.RootElement.GetProperty("trainer_markers").EnumerateArray()
                .Where(m => m.GetProperty("map_kind").GetString() == "overworld" && m.GetProperty("game_context").GetInt32() is 1220 or 1148 or 1244)
                .Select(m => m.GetProperty("text_id").GetInt32()).ToList();
            var byMarker = hubs.Where(hub => hub.TryGetProperty("marker", out _)).ToDictionary(hub => hub.GetProperty("marker").GetInt32());
            CollectionAssert.AreEquivalent(new[] { 513, 980, 981, 987, 1096 }, concordia);
            foreach (var text in concordia)
            {
                var hub = byMarker[text];
                if (hub.GetProperty("status").GetString() == "gap")
                    StringAssert.StartsWith(hub.GetProperty("gap").GetString(), "GAP-HUB-TRAINER-IDENTITY-", $"marker {text}");
                else
                    Assert.AreEqual("placed", hub.GetProperty("status").GetString(), $"marker {text}");
            }
        }

        [TestMethod]
        public void ThePerClassTrainersAreRetiredAndTheirGapsAreInTheManifest()
        {
            using var document = Evidence();
            var retired = document.RootElement.GetProperty("retired").GetProperty("spawnpools").GetString();
            StringAssert.StartsWith(retired, $"{SingleClassTrainersRows.PerClassFirst}-{SingleClassTrainersRows.PerClassLast}");

            // The per-class markers survive only on the unused wargame copy of the Wilderness.
            var perClassMarkers = document.RootElement.GetProperty("trainer_markers").EnumerateArray()
                .Where(m => m.GetProperty("text_id").GetInt32() is 977 or 978).ToList();
            Assert.AreEqual(2, perClassMarkers.Count);
            Assert.IsTrue(perClassMarkers.All(m => m.GetProperty("map_template").GetInt32() == 2269 && m.GetProperty("game_context").GetInt32() == 2265));

            using var manifest = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("bootcamp-d11-reconstruction-manifest.json")));
            var gaps = manifest.RootElement.GetProperty("gaps").EnumerateArray().Select(g => g.GetProperty("id").GetString()).ToHashSet();
            var cited = document.RootElement.GetProperty("hubs").EnumerateArray()
                .Where(hub => hub.TryGetProperty("gap", out _)).Select(hub => hub.GetProperty("gap").GetString()).ToList();
            cited.Add("GAP-DAGHDA-TRAINER-PRESENTATION");
            foreach (var gap in cited)
                Assert.IsTrue(gaps.Contains(gap), $"{gap} is not in the manifest");
        }
    }
}
