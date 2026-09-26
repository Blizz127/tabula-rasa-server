using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Test.Reconstruction;

namespace Rasa.Test
{
    /// <summary>
    /// The 2026-09-26 footage ledger must cite every 2026-09-26-supplied observation (final
    /// night, launch-era Eloh Vale, pre-D11 Foreas) and must not promote a launch-era or
    /// pre-D11 row into a final-live rule.
    /// </summary>
    [TestClass]
    public class FootageLedgerSupplied20260926Tests
    {
        private const string Heading = "## 2026-09-26 — supplied footage ledger: final night, Eloh Vale and pre-D11 Foreas";

        private static readonly string[] FanoutFiles =
        {
            "fxAtDpxypSw.json",
            "j_4B22Y8z28.json",
            "ZevYcdI1y8I.json",
            "ikIRxsE9HhU.json",
        };

        [TestMethod]
        public void DatedLedgerCitesEverySupplied20260926ObservationWithoutClaimingFinalLiveRules()
        {
            var section = LedgerSection();
            StringAssert.Contains(section, "Missing evidence is not 100% retail accuracy.");
            foreach (var line in section.Split('\n'))
            {
                if (line.IndexOf("100% retail accuracy", StringComparison.OrdinalIgnoreCase) >= 0)
                    StringAssert.Contains(line, "not 100% retail accuracy");
            }

            var evidence = EvidenceLocator.EvidenceDirectory();
            var inventoryPath = Path.Combine(evidence, "gameplay-footage-supplied-20260926.json");
            var inventoryIds = InventoryVideoIds(inventoryPath);
            Assert.AreEqual(7, inventoryIds.Count, "inventory should list all seven 2026-09-26 supplied videos");

            var cites = new List<(string Cite, bool Boundary)>();
            var fanoutIds = new HashSet<string>();
            foreach (var name in FanoutFiles)
            {
                var (rows, ids) = FanoutCites(Path.Combine(evidence, "footage-fanout-20260926", name));
                cites.AddRange(rows);
                fanoutIds.UnionWith(ids);
            }

            Assert.IsTrue(cites.Count > 0, "the 2026-09-26 supplied footage files have no observations");
            foreach (var videoId in inventoryIds)
                Assert.IsTrue(fanoutIds.Contains(videoId), "inventory video " + videoId + " has no fan-out observations");

            foreach (var (cite, boundary) in cites.Distinct())
            {
                var line = section.Split('\n').FirstOrDefault(row => row.Contains(cite, StringComparison.Ordinal));
                Assert.IsNotNull(line, "ledger is missing " + cite);
                if (boundary)
                {
                    StringAssert.Contains(line, "not a final-live rule");
                    Assert.IsFalse(line.Contains("is a final-live rule", StringComparison.Ordinal), cite);
                }
                else
                {
                    Assert.IsFalse(line.Contains("not a final-live rule", StringComparison.Ordinal),
                        cite + " is not a boundary row and must not carry the boundary disclaimer");
                }
            }
        }

        [TestMethod]
        public void LaunchEraAndPreD11AreTheOnlyBoundarySourceClasses()
        {
            var evidence = EvidenceLocator.EvidenceDirectory();
            foreach (var name in FanoutFiles)
            {
                using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(evidence, "footage-fanout-20260926", name)));
                foreach (var video in document.RootElement.GetProperty("videos").EnumerateArray())
                {
                    var sourceClass = video.GetProperty("sourceClass").GetString();
                    var boundary = IsBoundarySourceClass(sourceClass);
                    var expected = sourceClass.StartsWith("launch-era", StringComparison.Ordinal)
                        || sourceClass.StartsWith("pre-d11", StringComparison.Ordinal);
                    Assert.AreEqual(expected, boundary, sourceClass);
                }
            }
        }

        private static string LedgerSection()
        {
            var path = RetailAccuracy();
            var text = File.ReadAllText(path);
            var start = text.IndexOf(Heading, StringComparison.Ordinal);
            Assert.IsTrue(start >= 0, "docs/retail-accuracy.md has no 2026-09-26 supplied footage ledger heading");
            var next = text.IndexOf("\n## ", start + Heading.Length, StringComparison.Ordinal);
            return next < 0 ? text.Substring(start) : text.Substring(start, next - start);
        }

        private static string RetailAccuracy()
        {
            for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            {
                var candidate = Path.Combine(directory.FullName, "docs", "retail-accuracy.md");
                if (File.Exists(candidate))
                    return candidate;
            }
            Assert.Fail("docs/retail-accuracy.md was not found above " + AppContext.BaseDirectory);
            return null;
        }

        private static List<string> InventoryVideoIds(string path)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            return document.RootElement.GetProperty("videos").EnumerateArray()
                .Select(video => video.GetProperty("videoId").GetString())
                .ToList();
        }

        private static (IEnumerable<(string Cite, bool Boundary)> Rows, IEnumerable<string> VideoIds) FanoutCites(string path)
        {
            var rows = new List<(string Cite, bool Boundary)>();
            var ids = new List<string>();
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            foreach (var video in document.RootElement.GetProperty("videos").EnumerateArray())
            {
                var videoId = video.GetProperty("videoId").GetString();
                ids.Add(videoId);
                var sourceClass = video.TryGetProperty("sourceClass", out var source) ? source.GetString() : "";
                var boundary = IsBoundarySourceClass(sourceClass);
                foreach (var observation in video.GetProperty("observations").EnumerateArray())
                    rows.Add((Cite(videoId, observation.GetProperty("tSeconds")), boundary));
            }
            return (rows, ids);
        }

        private static bool IsBoundarySourceClass(string sourceClass)
        {
            if (string.IsNullOrEmpty(sourceClass))
                return false;
            return sourceClass.StartsWith("launch-era", StringComparison.Ordinal)
                || sourceClass.StartsWith("pre-d11", StringComparison.Ordinal);
        }

        private static string Cite(string videoId, JsonElement time)
        {
            var raw = time.ValueKind == JsonValueKind.Number && time.TryGetInt64(out var whole)
                ? whole.ToString(CultureInfo.InvariantCulture)
                : time.GetRawText();
            return "cite:" + videoId + "@" + raw;
        }
    }
}
