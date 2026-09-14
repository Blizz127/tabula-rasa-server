using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Structures.World;

namespace Rasa.Test.Reconstruction
{
    [TestClass]
    public class ManifestValidatorTests
    {
        private const string ManifestSuffix = "-reconstruction-manifest.json";

        private static readonly string[] RejectFolders = { "RejectManifest", "RejectFootageEvents", "RejectPositions" };

        private static readonly ManifestValidator Validator = new ManifestValidator();

        public static IEnumerable<object[]> Rules => ManifestRules.All.Select(rule => new object[] { rule });

        private static string Read(string path) => File.ReadAllText(path);

        private static (FootageEventSet Footage, PositionSet Positions) FixtureContext()
        {
            var footageReport = ManifestValidator.ValidateFootageEvents(Read(EvidenceLocator.FixtureFile("footage-events.json")), out var footage);
            Assert.IsTrue(footageReport.IsValid, "fixture footage events must be valid:\n" + footageReport);
            var positionsReport = ManifestValidator.ValidatePositions(Read(EvidenceLocator.FixtureFile("positions.json")), out var positions);
            Assert.IsTrue(positionsReport.IsValid, "fixture positions must be valid:\n" + positionsReport);
            return (footage, positions);
        }

        [TestMethod]
        public void ValidFixtureExercisesEveryTierAndPasses()
        {
            var report = Validator.ValidateBundle(
                Read(EvidenceLocator.FixtureFile("manifest-valid.json")),
                Read(EvidenceLocator.FixtureFile("footage-events.json")),
                Read(EvidenceLocator.FixtureFile("positions.json")));
            Assert.IsTrue(report.IsValid, report.ToString());

            using var document = JsonDocument.Parse(Read(EvidenceLocator.FixtureFile("manifest-valid.json")));
            var tiers = document.RootElement.GetProperty("rows").EnumerateArray()
                .SelectMany(row => row.GetProperty("fields").EnumerateObject())
                .Select(field => field.Value.GetProperty("tier").GetString())
                .Distinct()
                .ToList();
            CollectionAssert.AreEquivalent(ManifestVocabulary.Tiers, tiers);
        }

        [DataTestMethod]
        [DynamicData(nameof(Rules), DynamicDataSourceType.Property)]
        public void RejectFixtureFailsWithMessagesNamingItsRule(string rule)
        {
            var folder = RejectFolders.FirstOrDefault(f => File.Exists(Path.Combine(EvidenceLocator.FixturesDirectory(), f, rule + ".json")));
            Assert.IsNotNull(folder, $"rule {rule} has no rejecting fixture {rule}.json in {string.Join(", ", RejectFolders)}");
            var text = Read(EvidenceLocator.FixtureFile(folder, rule + ".json"));

            ValidationReport report;
            switch (folder)
            {
                case "RejectManifest":
                    var (footage, positions) = FixtureContext();
                    report = Validator.ValidateManifest(text, footage, positions);
                    break;
                case "RejectFootageEvents":
                    report = ManifestValidator.ValidateFootageEvents(text, out _);
                    break;
                default:
                    report = ManifestValidator.ValidatePositions(text, out _);
                    break;
            }

            Assert.IsFalse(report.IsValid, $"fixture {folder}/{rule}.json was accepted");
            foreach (var error in report.Errors)
            {
                Assert.AreEqual(rule, error.Rule, $"fixture {folder}/{rule}.json must fail only its own rule:\n{report}");
                StringAssert.Contains(error.ToString(), $"[{rule}]");
            }
        }

        [TestMethod]
        public void EveryRejectFixtureNamesAKnownRule()
        {
            var fixtures = RejectFolders
                .SelectMany(folder => Directory.GetFiles(Path.Combine(EvidenceLocator.FixturesDirectory(), folder), "*.json"))
                .Select(Path.GetFileNameWithoutExtension)
                .ToList();
            Assert.AreEqual(fixtures.Count, fixtures.Distinct().Count(), "a rule has more than one rejecting fixture");
            CollectionAssert.AreEquivalent(ManifestRules.All.ToList(), fixtures);
        }

        [TestMethod]
        public void TierRulesCoverTheSection17Table()
        {
            CollectionAssert.AreEquivalent(new[]
            {
                ManifestRules.OriginalLocator, ManifestRules.OriginalSourceKind,
                ManifestRules.ObservedEvent, ManifestRules.ObservedSourceKind, ManifestRules.ObservedEventMissing,
                ManifestRules.ObservedEventExcluded, ManifestRules.ObservedTime, ManifestRules.ObservedVideo,
                ManifestRules.ObservedCorrectedText,
                ManifestRules.MeasuredUncertainty, ManifestRules.MeasuredReference, ManifestRules.MeasuredSourceKind,
                ManifestRules.MeasuredPositionKey,
                ManifestRules.InferredReasoning,
                ManifestRules.AnalogueCounterpart, ManifestRules.AnalogueRequiredBecause, ManifestRules.AnalogueDecision,
                ManifestRules.AnalogueDecisionApproved, ManifestRules.AnaloguePreD11, ManifestRules.AnalogueOptionalColumn,
                ManifestRules.AnalogueUnregisteredColumn
            }, ManifestRules.TierRules.ToList());
            Assert.AreEqual(0.07, ManifestVocabulary.ObservedTimeTolerance);
        }

        [TestMethod]
        public void EmptyObjectReportsEveryRequiredTopLevelMember()
        {
            var report = Validator.ValidateManifest("{}", FootageEventSet.Empty, PositionSet.Empty);
            CollectionAssert.AreEquivalent(ManifestVocabulary.RequiredTopLevel,
                report.Errors.Where(e => e.Rule == ManifestRules.SchemaRequired).Select(e => e.Path).ToList());
            Assert.IsTrue(report.Errors.All(e => e.Rule == ManifestRules.SchemaRequired), report.ToString());
        }

        [TestMethod]
        public void RealManifestsAndCompanionFilesPass()
        {
            var directory = EvidenceLocator.EvidenceDirectory();
            var manifests = Directory.GetFiles(directory, "*" + ManifestSuffix).OrderBy(p => p, StringComparer.Ordinal).ToList();
            Assert.IsTrue(manifests.Any(p => Path.GetFileName(p) == "bootcamp-d11" + ManifestSuffix),
                $"docs/evidence at {directory} has no bootcamp-d11{ManifestSuffix}");

            foreach (var manifest in manifests)
            {
                var segment = Path.GetFileName(manifest).Substring(0, Path.GetFileName(manifest).Length - ManifestSuffix.Length);
                using (var document = JsonDocument.Parse(Read(manifest)))
                    Assert.AreEqual(segment, document.RootElement.GetProperty("segment").GetProperty("id").GetString(),
                        $"{manifest}: segment.id must match the file name");

                var report = Validator.ValidateBundle(
                    Read(manifest),
                    Read(EvidenceLocator.EvidenceFile(segment + "-footage-events.json")),
                    Read(EvidenceLocator.EvidenceFile(segment + "-positions.json")));
                Assert.IsTrue(report.IsValid, $"{manifest}:\n{report}");
            }
        }

        [TestMethod]
        public void BootcampManifestDeclaresTheS0EvidenceContract()
        {
            using var document = JsonDocument.Parse(Read(EvidenceLocator.EvidenceFile("bootcamp-d11" + ManifestSuffix)));
            var root = document.RootElement;

            var segment = root.GetProperty("segment");
            Assert.AreEqual("bootcamp-d11", segment.GetProperty("id").GetString());
            CollectionAssert.AreEqual(new long[] { 1985 }, segment.GetProperty("map_context_ids").EnumerateArray().Select(e => e.GetInt64()).ToArray());
            Assert.AreEqual(783, segment.GetProperty("map_version").GetInt32());
            CollectionAssert.AreEqual(new long[] { 1990, 1992, 1994, 1995, 2005 }, segment.GetProperty("missions").EnumerateArray().Select(e => e.GetInt64()).ToArray());
            Assert.AreEqual("1.16.5.0", segment.GetProperty("client_version").GetString());

            var sources = root.GetProperty("sources");
            var expectedVideos = new Dictionary<string, string>
            {
                ["7Lrst9SG3pk"] = "c3783d8c863f6807e8f55a8b47dd33d6f1117c3556906fda94c62a933edf6632",
                ["8VXeKzGUv0c"] = "43187d064abf0282f150513a83f5abd07023a3b9db5baab01a6431f1536f63e0",
                ["Ycxm8Pa1-v4"] = "0176e9184c5b8bc71533712ca7da0e08044381e10bb90a35948f80b24cc0b62d"
            };
            foreach (var (videoId, sha) in expectedVideos)
            {
                var source = sources.GetProperty("video:" + videoId);
                Assert.AreEqual("video", source.GetProperty("kind").GetString());
                Assert.AreEqual("d11_to_shutdown", source.GetProperty("era").GetString());
                Assert.AreEqual(videoId, source.GetProperty("video_id").GetString());
                Assert.AreEqual(sha, source.GetProperty("sha256").GetString());
                StringAssert.StartsWith(source.GetProperty("timebase").GetString(), "t is decoded video seconds");
            }

            var map = sources.GetProperty("client_map:adv_bootcamp");
            Assert.AreEqual("client_map", map.GetProperty("kind").GetString());
            Assert.AreEqual("final_live", map.GetProperty("era").GetString());
            Assert.AreEqual("2876982ba3473dcb57d5cc4a1b88d75710349a71948d85fafb499b2a8f5106f3", map.GetProperty("sha256").GetString());
            var tables = sources.GetProperty("client_table:game.zip");
            Assert.AreEqual("client_table", tables.GetProperty("kind").GetString());
            Assert.AreEqual("final_live", tables.GetProperty("era").GetString());
            Assert.AreEqual("e78b53640e75954b6b36e88eccfb780fcc79ec7b7ee51edbd213073e26406ca6", tables.GetProperty("sha256").GetString());

            // Reserved boot-camp storage keys, build plan section 1.3.
            var scope = root.GetProperty("scope").EnumerateArray().ToDictionary(
                e => e.GetProperty("table").GetString(),
                e => e.GetProperty("key_ranges").EnumerateObject()
                    .Select(r => $"{r.Name}:{r.Value.GetProperty("min").GetInt64()}-{r.Value.GetProperty("max").GetInt64()}")
                    .Single());
            CollectionAssert.AreEquivalent(new Dictionary<string, string>
            {
                ["creature"] = "id:198500-198599",
                ["content_area"] = "id:198600-198649",
                ["content_placement"] = "id:198650-198899",
                ["content_condition"] = "condition_id:198900-198999",
                ["content_rule"] = "id:1985000-1985999",
                ["content_rule_action"] = "rule_id:1985000-1985999",
                ["content_item_set"] = "item_set_id:19851-19859",
                ["content_location"] = "id:19851-19859",
                ["content_map_setting"] = "map_context_id:1985-1985"
            }.ToList(), scope.ToList());

            var gate = root.GetProperty("non_content_settings").EnumerateArray()
                .Single(s => s.GetProperty("key").GetString() == "Bootcamp.EntryMode");
            Assert.AreEqual("Disabled", gate.GetProperty("default").GetString());

            Assert.IsTrue(root.GetProperty("gaps").GetArrayLength() > 0, "the gap register must not be empty");
            foreach (var decision in root.GetProperty("owner_decisions").EnumerateArray().Select(d => d.GetProperty("id").GetString()))
                StringAssert.StartsWith(decision, "OD-");
        }

        [TestMethod]
        public void SchemaFileMatchesTheValidatorVocabulary()
        {
            using var document = JsonDocument.Parse(Read(EvidenceLocator.EvidenceFile("reconstruction-manifest.schema.json")));
            var root = document.RootElement;
            Assert.AreEqual("https://json-schema.org/draft/2020-12/schema", root.GetProperty("$schema").GetString());
            Assert.AreEqual(ManifestVocabulary.ManifestSchemaId, root.GetProperty("$id").GetString());
            CollectionAssert.AreEqual(ManifestVocabulary.RequiredTopLevel,
                root.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToArray());

            var properties = root.GetProperty("properties");
            Assert.AreEqual(ManifestVocabulary.ManifestSchemaId, properties.GetProperty("schema").GetProperty("const").GetString());
            Assert.AreEqual(ManifestVocabulary.PolicyAnchor, properties.GetProperty("policy").GetProperty("const").GetString());

            var defs = root.GetProperty("$defs");
            foreach (var name in new[] { "tier", "source", "citation", "uncertainty", "field", "row", "change" })
                Assert.IsTrue(defs.TryGetProperty(name, out _), $"$defs.{name} is missing");

            static string[] EnumOf(JsonElement element) => element.GetProperty("enum").EnumerateArray().Select(e => e.GetString()).ToArray();
            CollectionAssert.AreEqual(ManifestVocabulary.Tiers, EnumOf(defs.GetProperty("tier")));
            CollectionAssert.AreEqual(ManifestVocabulary.Slices, EnumOf(defs.GetProperty("slice")));
            CollectionAssert.AreEqual(ManifestVocabulary.SourceKinds, EnumOf(defs.GetProperty("source").GetProperty("properties").GetProperty("kind")));
            CollectionAssert.AreEqual(ManifestVocabulary.Eras, EnumOf(defs.GetProperty("source").GetProperty("properties").GetProperty("era")));
            CollectionAssert.AreEqual(ManifestVocabulary.StorageRoles, EnumOf(defs.GetProperty("row").GetProperty("properties")
                .GetProperty("storage_fields").GetProperty("additionalProperties").GetProperty("properties").GetProperty("role")));
            CollectionAssert.AreEqual(ManifestVocabulary.DecisionStatuses, EnumOf(defs.GetProperty("owner_decision").GetProperty("properties").GetProperty("status")));
            CollectionAssert.AreEqual(new[] { "value", "tier", "citations" },
                defs.GetProperty("field").GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToArray());
        }

        [TestMethod]
        public void RealFootageEventsAndPositionsFilesAreValid()
        {
            foreach (var file in Directory.GetFiles(EvidenceLocator.EvidenceDirectory(), "*-footage-events.json"))
            {
                var report = ManifestValidator.ValidateFootageEvents(Read(file), out _);
                Assert.IsTrue(report.IsValid, $"{file}:\n{report}");
            }
            foreach (var file in Directory.GetFiles(EvidenceLocator.EvidenceDirectory(), "*-positions.json"))
            {
                var report = ManifestValidator.ValidatePositions(Read(file), out _);
                Assert.IsTrue(report.IsValid, $"{file}:\n{report}");
            }
        }

        [TestMethod]
        public void ProvenanceRegistryCoversTheSection13Tables()
        {
            CollectionAssert.AreEquivalent(new[]
            {
                "map_info", "content_map_setting", "creature", "npc_mission_prerequisite", "npc_mission_objective_binding",
                "npc_mission_objective_counter", "npc_mission_objective_timer", "npc_mission_objective_indicator",
                "content_area", "content_placement", "content_condition", "content_rule", "content_rule_action",
                "content_item_set", "content_location"
            }, ProvenanceRegistry.Default.Tables.Select(t => t.Table).ToList());

            foreach (var table in ProvenanceRegistry.Default.Tables)
            {
                var columns = table.AllColumns.ToList();
                Assert.AreEqual(columns.Count, columns.Distinct().Count(), $"{table.Table}: a column has more than one role");
                Assert.IsTrue(table.KeyColumns.Count > 0, $"{table.Table}: no key columns");
            }

            Assert.AreEqual(ColumnRole.Optional, ProvenanceRegistry.Default.RoleOf("npc_mission_objective_indicator", "pos_x"));
            Assert.AreEqual(ColumnRole.Optional, ProvenanceRegistry.Default.RoleOf("content_placement", "respawn_ms"));
            Assert.AreEqual(ColumnRole.Required, ProvenanceRegistry.Default.RoleOf("npc_mission_objective_timer", "limit_seconds"));
            Assert.AreEqual(ColumnRole.Required, ProvenanceRegistry.Default.RoleOf("creature", "class_id"));
            Assert.AreEqual(ColumnRole.Unknown, ProvenanceRegistry.Default.RoleOf("npc_mission", "giver_id"));
        }

        [TestMethod]
        public void ProvenanceRegistryListsEveryColumnOfTheExistingEntities()
        {
            static IEnumerable<string> Columns(Type entity) => entity.GetProperties()
                .Select(p => p.GetCustomAttribute<ColumnAttribute>()?.Name)
                .Where(n => n != null);

            var entities = new[]
            {
                typeof(CreatureEntry), typeof(MapInfoEntry), typeof(ContentMapSettingEntry), typeof(NpcMissionPrerequisiteEntry),
                typeof(NpcMissionObjectiveBindingEntry), typeof(NpcMissionObjectiveCounterEntry), typeof(NpcMissionObjectiveTimerEntry),
                typeof(NpcMissionObjectiveIndicatorEntry), typeof(ContentAreaEntry), typeof(ContentPlacementEntry), typeof(ContentConditionEntry),
                typeof(ContentRuleEntry), typeof(ContentRuleActionEntry), typeof(ContentItemSetEntry), typeof(ContentLocationEntry)
            };

            foreach (var entity in entities)
            {
                var table = entity.GetCustomAttribute<TableAttribute>().Name;
                Assert.IsTrue(ProvenanceRegistry.Default.TryGet(table, out var provenance), table);
                var columns = Columns(entity).ToList();
                foreach (var column in columns)
                    Assert.AreNotEqual(ColumnRole.Unknown, provenance.RoleOf(column), $"{table}.{column} is not classified in ProvenanceRegistry");
                foreach (var column in provenance.AllColumns)
                    CollectionAssert.Contains(columns, column, $"{table}.{column} is registered but has no column");
            }

            Assert.AreEqual(entities.Length, ProvenanceRegistry.Default.Tables.Count, "a registered table has no entity");
        }
    }
}
