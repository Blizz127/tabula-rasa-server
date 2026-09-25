using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Test.Reconstruction;

namespace Rasa.Test
{
    /// <summary>
    /// The 2026-09-25 footage ledger must cite every supplied observation and must not
    /// promote a public-test, beta, or E3 2006 row into a final-live rule.
    /// </summary>
    [TestClass]
    public class FootageLedgerTests
    {
        private const string Heading = "## 2026-09-25 — supplied footage ledger";

        [TestMethod]
        public void DatedLedgerCitesEverySuppliedObservationWithoutClaimingFinalLiveRules()
        {
            var section = LedgerSection();
            StringAssert.Contains(section, "Missing evidence is not 100% retail accuracy.");
            foreach (var line in section.Split('\n'))
            {
                if (line.IndexOf("100% retail accuracy", StringComparison.OrdinalIgnoreCase) >= 0)
                    StringAssert.Contains(line, "not 100% retail accuracy");
            }

            var evidence = EvidenceLocator.EvidenceDirectory();
            var inventoryPath = Path.Combine(evidence, "gameplay-footage-playlists-20260924.json");
            var inventory = InventoryCites(inventoryPath).ToList();
            AssertExerciseClipBoundaryComesFromTheSharedBlob(inventoryPath, inventory);
            var cites = new List<(string Cite, bool Boundary)>();
            cites.AddRange(inventory.Select(row => (row.Cite, row.Boundary)));
            foreach (var name in new[] { "soundtrack.json", "e3.json", "events.json", "neuronet.json" })
                cites.AddRange(FanoutCites(Path.Combine(evidence, "footage-fanout-20260925", name)));

            Assert.IsTrue(cites.Count > 0, "the supplied footage files have no observations");
            foreach (var (cite, boundary) in cites.Distinct())
            {
                var line = section.Split('\n').FirstOrDefault(row => row.Contains(cite, StringComparison.Ordinal));
                Assert.IsNotNull(line, "ledger is missing " + cite);
                if (!boundary)
                    continue;
                StringAssert.Contains(line, "not a final-live rule");
                Assert.IsFalse(line.Contains("is a final-live rule", StringComparison.Ordinal), cite);
            }
        }

        private static string LedgerSection()
        {
            var path = RetailAccuracy();
            var text = File.ReadAllText(path);
            var start = text.IndexOf(Heading, StringComparison.Ordinal);
            Assert.IsTrue(start >= 0, "docs/retail-accuracy.md has no 2026-09-25 footage ledger heading");
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

        private static void AssertExerciseClipBoundaryComesFromTheSharedBlob(
            string path, IReadOnlyList<(string Cite, bool Boundary, string Blob)> inventory)
        {
            var row = inventory.Single(item => item.Cite == "cite:EiE2oodlP8A@20");
            Assert.AreEqual(IsBoundary(row.Blob), row.Boundary, "the InventoryCites boundary flag is IsBoundary of its shared blob");
            Assert.IsTrue(row.Boundary, "cite:EiE2oodlP8A@20 must be a boundary from InventoryCites");
            StringAssert.Contains(row.Blob, "Public Test Server");
            StringAssert.Contains(row.Blob, "PTS only");
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            VideoMeta(document.RootElement).TryGetValue("EiE2oodlP8A", out var title);
            Assert.IsFalse(IsBoundary(title ?? ""), "the video title alone must not hide the public-test claim");
        }

        private static IEnumerable<(string Cite, bool Boundary, string Blob)> InventoryCites(string path)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            var meta = VideoMeta(root);
            foreach (var observation in root.GetProperty("observations").EnumerateArray())
            {
                var videoId = observation.GetProperty("videoId").GetString();
                meta.TryGetValue(videoId, out var title);
                var unverified = observation.TryGetProperty("unverified", out var note) ? note.GetString() : "";
                var blob = ObservationBlob(title, ClaimText(root, videoId), unverified);
                yield return (Cite(videoId, observation.GetProperty("timeSeconds")), IsBoundary(blob), blob);
            }
        }

        private static string ObservationBlob(string title, string claims, string unverified)
            => (title ?? "") + " " + (claims ?? "") + " " + (unverified ?? "");

        private static string ClaimText(JsonElement root, string videoId)
        {
            if (!root.TryGetProperty("uploaderClaims", out var claims))
                return "";
            var text = "";
            foreach (var claim in claims.EnumerateArray())
            {
                if (claim.GetProperty("videoId").GetString() != videoId)
                    continue;
                text += " " + (claim.TryGetProperty("claim", out var body) ? body.GetString() : "");
                text += " " + (claim.TryGetProperty("limit", out var limit) ? limit.GetString() : "");
            }
            return text;
        }

        private static IEnumerable<(string Cite, bool Boundary)> FanoutCites(string path)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            foreach (var video in document.RootElement.GetProperty("videos").EnumerateArray())
            {
                var videoId = video.GetProperty("videoId").GetString();
                var blob = (video.TryGetProperty("sourceClass", out var source) ? source.GetString() : "")
                    + " " + (video.TryGetProperty("title", out var title) ? title.GetString() : "");
                var boundary = IsBoundary(blob);
                foreach (var observation in video.GetProperty("observations").EnumerateArray())
                    yield return (Cite(videoId, observation.GetProperty("tSeconds")), boundary);
            }
        }

        private static Dictionary<string, string> VideoMeta(JsonElement root)
        {
            var meta = new Dictionary<string, string>();
            void Take(JsonElement item)
            {
                if (!item.TryGetProperty("videoId", out var id))
                    return;
                var blob = (item.TryGetProperty("title", out var title) ? title.GetString() : "")
                    + " " + (item.TryGetProperty("description", out var description) ? description.GetString() : "")
                    + " " + (item.TryGetProperty("sourceClass", out var source) ? source.GetString() : "");
                meta[id.GetString()] = blob;
            }
            if (root.TryGetProperty("playlists", out var playlists))
                foreach (var playlist in playlists.EnumerateArray())
                    foreach (var item in playlist.GetProperty("items").EnumerateArray())
                        Take(item);
            if (root.TryGetProperty("standalone", out var standalone))
                foreach (var item in standalone.EnumerateArray())
                    Take(item);
            return meta;
        }

        internal static bool IsBoundary(string blob)
        {
            if (Regex.IsMatch(blob, @"\be3\b", RegexOptions.IgnoreCase) || blob.IndexOf("pre-release", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (Regex.IsMatch(blob, @"\bpts\b", RegexOptions.IgnoreCase) || blob.IndexOf("public test", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return Regex.IsMatch(blob, @"\bbeta\b", RegexOptions.IgnoreCase);
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
