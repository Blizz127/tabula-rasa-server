using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Reconstruction
{
    /// <summary>
    /// Finds the repository's docs/evidence directory and the test fixtures. A missing
    /// directory fails the calling test with an explicit message; nothing is skipped.
    /// </summary>
    public static class EvidenceLocator
    {
        public static string EvidenceDirectory()
        {
            var walked = WalkUp(Path.Combine("docs", "evidence"));
            if (walked != null)
                return walked;

            // Rasa.Test.csproj links ..\..\docs\evidence\**\*.json into the output under evidence\.
            var copied = Path.Combine(AppContext.BaseDirectory, "evidence");
            if (Directory.Exists(copied))
                return copied;

            Assert.Fail(
                $"docs/evidence was not found walking up from {AppContext.BaseDirectory}, and no copied evidence directory exists at {copied}. " +
                "Build from a checkout that contains docs/evidence, or mount it (for example -v <repo>/docs/evidence:/app/docs/evidence:ro).");
            return null;
        }

        public static string EvidenceFile(string name)
        {
            var path = Path.Combine(EvidenceDirectory(), name);
            if (!File.Exists(path))
                Assert.Fail($"Evidence file {path} does not exist.");
            return path;
        }

        public static string FixturesDirectory()
        {
            var copied = Path.Combine(AppContext.BaseDirectory, "Reconstruction", "Fixtures");
            if (Directory.Exists(copied))
                return copied;

            var walked = WalkUp(Path.Combine("src", "Rasa.Test", "Reconstruction", "Fixtures"));
            if (walked != null)
                return walked;

            Assert.Fail($"Reconstruction fixtures were not found at {copied} or in a src/Rasa.Test/Reconstruction/Fixtures directory above {AppContext.BaseDirectory}.");
            return null;
        }

        public static string FixtureFile(params string[] parts)
        {
            var path = Path.Combine(FixturesDirectory(), Path.Combine(parts));
            if (!File.Exists(path))
                Assert.Fail($"Fixture {path} does not exist.");
            return path;
        }

        private static string WalkUp(string relative)
        {
            for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            {
                var candidate = Path.Combine(directory.FullName, relative);
                if (Directory.Exists(candidate))
                    return candidate;
            }
            return null;
        }
    }
}
