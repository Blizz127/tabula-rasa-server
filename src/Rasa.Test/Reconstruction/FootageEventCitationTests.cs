using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Reconstruction
{
    /// <summary>
    /// Observed citations resolve only to extracted footage events with status confirmed
    /// or corrected, within 0.07 s of the event time, and never to a refuted event.
    /// Fixtures are synthetic (Reconstruction/Fixtures/Citations).
    /// </summary>
    [TestClass]
    public class FootageEventCitationTests
    {
        private static ValidationReport ValidateManifest(string manifest, string footageFile = "footage-events.json")
        {
            var footageReport = ManifestValidator.ValidateFootageEvents(
                File.ReadAllText(EvidenceLocator.FixtureFile("Citations", footageFile)), out var footage);
            if (footageFile == "footage-events.json")
                Assert.IsTrue(footageReport.IsValid, footageReport.ToString());
            var positionsReport = ManifestValidator.ValidatePositions(
                File.ReadAllText(EvidenceLocator.FixtureFile("Citations", "positions.json")), out var positions);
            Assert.IsTrue(positionsReport.IsValid, positionsReport.ToString());

            return new ManifestValidator().ValidateManifest(
                File.ReadAllText(EvidenceLocator.FixtureFile("Citations", manifest + ".json")), footage, positions);
        }

        private static void AssertOnly(ValidationReport report, string rule, params string[] fragments)
        {
            Assert.IsFalse(report.IsValid, "the citation was accepted");
            Assert.IsTrue(report.Errors.All(e => e.Rule == rule), report.ToString());
            foreach (var fragment in fragments)
                Assert.IsTrue(report.Errors.Any(e => e.ToString().Contains(fragment)), $"no message contains \"{fragment}\":\n{report}");
        }

        [TestMethod]
        public void CorrectObservedCitationsResolve()
        {
            var report = ValidateManifest("observed-correct");
            Assert.IsTrue(report.IsValid, report.ToString());
        }

        [TestMethod]
        public void TimeDifferenceOfExactlySeventyMillisecondsIsAccepted()
        {
            var report = ValidateManifest("observed-time-boundary");
            Assert.IsTrue(report.IsValid, report.ToString());
        }

        [TestMethod]
        public void CitationToAMissingEventFails()
        {
            AssertOnly(ValidateManifest("observed-missing-event"), ManifestRules.ObservedEventMissing,
                "[observed.event-missing]", "event CT1-404 is not in the footage events file");
        }

        [TestMethod]
        public void CitationToADroppedRefutedEventFails()
        {
            AssertOnly(ValidateManifest("observed-refuted-event"), ManifestRules.ObservedEventExcluded,
                "[observed.event-excluded]", "event CT1-003 was refuted");
        }

        [TestMethod]
        public void CitationToARefutedEventLeftInTheEventsListFails()
        {
            var footageReport = ManifestValidator.ValidateFootageEvents(
                File.ReadAllText(EvidenceLocator.FixtureFile("Citations", "footage-events-refuted-status.json")), out _);
            Assert.IsTrue(footageReport.Errors.Any(e => e.Rule == ManifestRules.FootageStatus && e.Path.Contains("CT1-003")), footageReport.ToString());

            AssertOnly(ValidateManifest("observed-refuted-event", "footage-events-refuted-status.json"), ManifestRules.ObservedEventExcluded,
                "event CT1-003 was refuted");
        }

        [TestMethod]
        public void CitationWithAMismatchedTimeFails()
        {
            AssertOnly(ValidateManifest("observed-time-mismatch"), ManifestRules.ObservedTime,
                "[observed.time]", "t 100.08 differs from event CT1-001 t 100 by 0.08 s (limit 0.07 s)");
        }

        [TestMethod]
        public void UncorrectedTextWhereACorrectionExistsFails()
        {
            AssertOnly(ValidateManifest("observed-uncorrected-text"), ManifestRules.ObservedCorrectedText,
                "[observed.corrected-text]", "differs from the corrected text of event CT1-002");
        }

        [TestMethod]
        public void RealObservedCitationsResolveAgainstTheRealFootageEvents()
        {
            var footageReport = ManifestValidator.ValidateFootageEvents(
                File.ReadAllText(EvidenceLocator.EvidenceFile("bootcamp-d11-footage-events.json")), out var footage);
            Assert.IsTrue(footageReport.IsValid, footageReport.ToString());
            var positionsReport = ManifestValidator.ValidatePositions(
                File.ReadAllText(EvidenceLocator.EvidenceFile("bootcamp-d11-positions.json")), out var positions);
            Assert.IsTrue(positionsReport.IsValid, positionsReport.ToString());

            var report = new ManifestValidator().ValidateManifest(
                File.ReadAllText(EvidenceLocator.EvidenceFile("bootcamp-d11-reconstruction-manifest.json")), footage, positions);
            var citationErrors = report.Errors.Where(e => e.Rule.StartsWith("observed.") || e.Rule == ManifestRules.CitationEventT).ToList();
            Assert.AreEqual(0, citationErrors.Count, string.Join("\n", citationErrors));
            Assert.IsTrue(report.IsValid, report.ToString());
        }
    }
}
